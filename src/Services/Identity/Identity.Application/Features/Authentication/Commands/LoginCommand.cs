using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Authentication.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? MfaCode = null,
    string? DeviceInfo = null,
    string? IpAddress = null,
    string? UserAgent = null,
    bool RememberMe = false) : ICommand<Result<LoginResultDto>>;

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly UserDomainService _userDomainService;
    private readonly ITokenService _tokenService;
    private readonly IMfaService _mfaService;
    private readonly ISessionManagementService _sessionManagementService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IAuditLogRepository auditLogRepository,
        UserDomainService userDomainService,
        ITokenService tokenService,
        IMfaService mfaService,
        ISessionManagementService sessionManagementService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
        _userDomainService = userDomainService;
        _tokenService = tokenService;
        _mfaService = mfaService;
        _sessionManagementService = sessionManagementService;
        _logger = logger;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = new Email(request.Email);
            
            // Authenticate user
            var user = await _userDomainService.AuthenticateUserAsync(email, request.Password, cancellationToken);

            // Check if MFA is required
            if (user.MfaEnabled && string.IsNullOrEmpty(request.MfaCode))
            {
                // Create audit log for MFA challenge
                var mfaAuditLog = AuditLog.CreateLoginAttempt(
                    user.Id,
                    user.Email.Value,
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    false,
                    "MFA required");

                await _auditLogRepository.AddAsync(mfaAuditLog, cancellationToken);

                // Get available MFA methods
                var availableMethods = user.MfaDevices
                    .Where(d => d.IsActive && d.CanBeUsed())
                    .Select(d => d.Type.ToString())
                    .ToArray();

                return Result<LoginResultDto>.Success(new LoginResultDto
                {
                    RequiresMfa = true,
                    AvailableMfaMethods = availableMethods
                });
            }

            // Verify MFA if provided
            if (user.MfaEnabled && !string.IsNullOrEmpty(request.MfaCode))
            {
                var mfaValid = await VerifyMfaCodeAsync(user, request.MfaCode);
                if (!mfaValid)
                {
                    var mfaFailedAuditLog = AuditLog.CreateMfaEvent(
                        user.Id,
                        "VERIFICATION",
                        "Unknown",
                        request.IpAddress ?? "Unknown",
                        request.UserAgent ?? "Unknown",
                        false,
                        "Invalid MFA code");

                    await _auditLogRepository.AddAsync(mfaFailedAuditLog, cancellationToken);

                    return Result<LoginResultDto>.Failure("Invalid MFA code");
                }
            }

            // Check session limits before creating session
            var canCreateSession = await _sessionManagementService.CanCreateSessionAsync(user.Id, cancellationToken);
            if (!canCreateSession)
            {
                var maxAllowed = await _sessionManagementService.GetMaxAllowedSessionsAsync(user.Id, cancellationToken);
                var currentActive = await _sessionManagementService.GetActiveSessionCountAsync(user.Id, cancellationToken);
                
                _logger.LogWarning("Session limit exceeded for user {Email}. Max: {MaxAllowed}, Current: {CurrentActive}", 
                    user.Email.Value, maxAllowed, currentActive);

                return Result<LoginResultDto>.Failure($"Session limit exceeded. Maximum allowed sessions: {maxAllowed}");
            }

            // Create session
            var session = user.CreateSession(
                request.DeviceInfo ?? "Unknown Device",
                request.IpAddress ?? "Unknown");

            var sessionDuration = request.RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24);

            // Save session first
            await _sessionRepository.AddAsync(session, cancellationToken);

            // Enforce session limits (this might revoke old sessions)
            var revokedSessions = await _sessionManagementService.EnforceSessionLimitsAsync(user.Id, session, cancellationToken);
            if (revokedSessions.Any())
            {
                _logger.LogInformation("Revoked {Count} old sessions for user {Email} due to session limits", 
                    revokedSessions.Count(), user.Email.Value);
            }

            // Update user
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Generate reference tokens after session is saved
            var accessToken = await _tokenService.GenerateReferenceAccessTokenAsync(user, new[] { "openid", "profile", "email" });
            var refreshToken = await _tokenService.GenerateReferenceRefreshTokenAsync(user.Id, session.Id.ToString());

            // Update session with refresh token
            session.SetRefreshToken(refreshToken, DateTime.UtcNow.Add(sessionDuration));
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            // Create successful login audit log
            var successAuditLog = AuditLog.CreateLoginAttempt(
                user.Id,
                user.Email.Value,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            _logger.LogInformation("User {Email} logged in successfully", user.Email.Value);

            return Result<LoginResultDto>.Success(new LoginResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = session.ExpiresAt,
                RequiresMfa = false
            });
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("Login failed: {Error}", ex.Message);
            return Result<LoginResultDto>.Failure("Invalid credentials");
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning("Login failed: {Error}", ex.Message);
            return Result<LoginResultDto>.Failure("Invalid credentials");
        }
        catch (AccountLockedException ex)
        {
            _logger.LogWarning("Login failed: {Error}", ex.Message);
            return Result<LoginResultDto>.Failure($"Account is locked until {ex.LockedUntil}");
        }
        catch (EmailNotConfirmedException ex)
        {
            _logger.LogWarning("Login failed: {Error}", ex.Message);
            return Result<LoginResultDto>.Failure("Email address must be confirmed before login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return Result<LoginResultDto>.Failure("An unexpected error occurred during login");
        }
    }

    private async Task<bool> VerifyMfaCodeAsync(User user, string code)
    {
        foreach (var device in user.MfaDevices.Where(d => d.IsActive && d.CanBeUsed()))
        {
            var isValid = device.Type switch
            {
                MfaDeviceType.Totp => _mfaService.ValidateTotpCode(device.Secret, code),
                MfaDeviceType.EmailOtp => _mfaService.ValidateEmailOtp(device.Secret, code, device.UpdatedAt ?? device.CreatedAt, TimeSpan.FromMinutes(5)),
                MfaDeviceType.WebAuthn => await _mfaService.ValidateWebAuthnAsync(device.Secret, code, "challenge"),
                _ => false
            };

            if (isValid)
            {
                device.RecordUsage();
                return true;
            }
        }

        return false;
    }


}
