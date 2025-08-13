using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Authentication.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<LoginResultDto>>;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<LoginResultDto>>
{
    private readonly IReferenceTokenRepository _referenceTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IReferenceTokenRepository referenceTokenRepository,
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        ITokenService tokenService,
        IAuditLogRepository auditLogRepository,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _referenceTokenRepository = referenceTokenRepository;
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _tokenService = tokenService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<LoginResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate refresh token
            var refreshToken = await _referenceTokenRepository.GetValidTokenAsync(request.RefreshToken);
            if (refreshToken == null || refreshToken.TokenType != TokenTypes.RefreshToken)
            {
                _logger.LogWarning("Invalid refresh token provided: {Token}", request.RefreshToken);
                return Result<LoginResultDto>.Failure("Invalid refresh token");
            }

            if (!refreshToken.IsValid())
            {
                _logger.LogWarning("Expired or revoked refresh token: {Token}", request.RefreshToken);
                return Result<LoginResultDto>.Failure("Refresh token is expired or revoked");
            }

            // Get user
            var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("User not found for refresh token: {UserId}", refreshToken.UserId);
                return Result<LoginResultDto>.Failure("User not found");
            }

            // Check if user is active
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Inactive user attempted token refresh: {UserId}", user.Id);
                return Result<LoginResultDto>.Failure("User account is not active");
            }

            // Get session if available
            UserSession? session = null;
            if (!string.IsNullOrEmpty(refreshToken.SessionId))
            {
                session = await _sessionRepository.GetByIdAsync(Guid.Parse(refreshToken.SessionId), cancellationToken);
                if (session == null || !session.IsActive)
                {
                    _logger.LogWarning("Session not found or expired for refresh token: {SessionId}", refreshToken.SessionId);
                    return Result<LoginResultDto>.Failure("Session expired");
                }
            }

            // Revoke old refresh token
            refreshToken.Revoke("system", "Token refreshed");
            await _referenceTokenRepository.UpdateAsync(refreshToken, cancellationToken);

            // Generate new tokens
            var newAccessToken = await _tokenService.GenerateReferenceAccessTokenAsync(user, new[] { "openid", "profile", "email" });
            var newRefreshToken = await _tokenService.GenerateReferenceRefreshTokenAsync(user.Id, refreshToken.SessionId);

            // Update session if exists
            if (session != null)
            {
                session.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));
                await _sessionRepository.UpdateAsync(session, cancellationToken);
            }

            // Update user last login
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateTokenEvent(
                "REFRESH",
                null, // clientId
                user.Id,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true,
                null, // errorMessage
                refreshToken.SessionId
            );
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            // Get token expiration
            var accessTokenEntity = await _referenceTokenRepository.GetValidTokenAsync(newAccessToken);
            var expiresAt = accessTokenEntity?.ExpiresAt ?? DateTime.UtcNow.AddHours(1);

            return Result<LoginResultDto>.Success(new LoginResultDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                RequiresMfa = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for refresh token: {RefreshToken}", request.RefreshToken);
            return Result<LoginResultDto>.Failure("An error occurred while refreshing the token");
        }
    }
}
