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
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IAuditLogRepository auditLogRepository,
        UserDomainService userDomainService,
        ITokenService tokenService,
        IMfaService mfaService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
        _userDomainService = userDomainService;
        _tokenService = tokenService;
        _mfaService = mfaService;
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
                    AvailableMfaMethods = availableMethods,
                    User = MapToUserDto(user)
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

            // Create session
            var session = user.CreateSession(
                request.DeviceInfo ?? "Unknown Device",
                request.IpAddress ?? "Unknown");

            var sessionDuration = request.RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24);
            
            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user, new[] { "openid", "profile", "email" });
            var refreshToken = _tokenService.GenerateRefreshToken();
            
            session.SetRefreshToken(refreshToken, DateTime.UtcNow.Add(sessionDuration));

            // Save session
            await _sessionRepository.AddAsync(session, cancellationToken);

            // Update user
            await _userRepository.UpdateAsync(user, cancellationToken);

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
                User = MapToUserDto(user),
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

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            WalletAddress = user.WalletAddress?.Value,
            UserType = user.UserType.ToString(),
            Status = user.Status.ToString(),
            EmailConfirmed = user.EmailConfirmed,
            MfaEnabled = user.MfaEnabled,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
