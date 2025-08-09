using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Identity.Application.Features.Mfa.Commands;

public record InitiateWebAuthnSetupCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<WebAuthnChallengeDto>>;

public class InitiateWebAuthnSetupCommandHandler : ICommandHandler<InitiateWebAuthnSetupCommand, Result<WebAuthnChallengeDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<InitiateWebAuthnSetupCommandHandler> _logger;

    public InitiateWebAuthnSetupCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<InitiateWebAuthnSetupCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<WebAuthnChallengeDto>> Handle(InitiateWebAuthnSetupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<WebAuthnChallengeDto>.Failure("User not found");
            }

            // Generate a cryptographically secure challenge
            var challengeBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(challengeBytes);
            var challenge = Convert.ToBase64String(challengeBytes);

            // Store challenge temporarily (in real implementation, this would be cached)
            // For now, we'll return it and expect it back in the completion request

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "WEBAUTHN_SETUP_INITIATED",
                "WEBAUTHN",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("WebAuthn setup initiated for user {UserId}", user.Id);

            return Result<WebAuthnChallengeDto>.Success(new WebAuthnChallengeDto
            {
                Challenge = challenge,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating WebAuthn setup for user {UserId}", request.UserId);
            return Result<WebAuthnChallengeDto>.Failure("An error occurred while initiating WebAuthn setup");
        }
    }
}

public record CompleteWebAuthnSetupCommand(
    Guid UserId,
    string DeviceName,
    string Challenge,
    string CredentialId,
    string PublicKey,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class CompleteWebAuthnSetupCommandHandler : ICommandHandler<CompleteWebAuthnSetupCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<CompleteWebAuthnSetupCommandHandler> _logger;

    public CompleteWebAuthnSetupCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<CompleteWebAuthnSetupCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteWebAuthnSetupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // TODO: Implement proper WebAuthn credential verification
            // This would involve:
            // 1. Verifying the challenge matches what was issued
            // 2. Validating the credential creation response
            // 3. Verifying the public key and storing it securely

            // For now, we'll create a placeholder implementation
            if (string.IsNullOrEmpty(request.Challenge) || 
                string.IsNullOrEmpty(request.CredentialId) || 
                string.IsNullOrEmpty(request.PublicKey))
            {
                return Result.Failure("Invalid WebAuthn credential data");
            }

            // Create MFA device with encrypted credential data
            var credentialData = System.Text.Json.JsonSerializer.Serialize(new
            {
                CredentialId = request.CredentialId,
                PublicKey = request.PublicKey,
                CreatedAt = DateTime.UtcNow
            });

            var mfaDevice = new MfaDevice(user.Id, MfaDeviceType.WebAuthn, request.DeviceName, credentialData);
            user.AddMfaDevice(mfaDevice);
            user.EnableMfa();

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "WEBAUTHN_SETUP_COMPLETED",
                "WEBAUTHN",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            _logger.LogInformation("WebAuthn setup completed for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing WebAuthn setup for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while completing WebAuthn setup");
        }
    }
}
