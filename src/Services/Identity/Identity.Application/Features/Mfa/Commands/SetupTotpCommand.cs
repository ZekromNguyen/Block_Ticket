using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Mfa.Commands;

public record SetupTotpCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<SetupTotpDto>>;

public class SetupTotpCommandHandler : ICommandHandler<SetupTotpCommand, Result<SetupTotpDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMfaService _mfaService;
    private readonly ILogger<SetupTotpCommandHandler> _logger;

    public SetupTotpCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IMfaService mfaService,
        ILogger<SetupTotpCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _mfaService = mfaService;
        _logger = logger;
    }

    public async Task<Result<SetupTotpDto>> Handle(SetupTotpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<SetupTotpDto>.Failure("User not found");
            }

            // Generate TOTP secret
            var secret = _mfaService.GenerateTotpSecret();
            var qrCodeUri = _mfaService.GenerateQrCodeUri(user.Email.Value, secret);
            var backupCodes = _mfaService.GenerateBackupCodes();

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "SETUP_INITIATED",
                "TOTP",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("TOTP setup initiated for user {UserId}", user.Id);

            return Result<SetupTotpDto>.Success(new SetupTotpDto
            {
                Secret = secret,
                QrCodeUri = qrCodeUri,
                BackupCodes = backupCodes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up TOTP for user {UserId}", request.UserId);
            return Result<SetupTotpDto>.Failure("An error occurred while setting up TOTP");
        }
    }
}

public record VerifyTotpSetupCommand(
    Guid UserId,
    string Secret,
    string Code,
    string DeviceName,
    string[]? BackupCodes = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class VerifyTotpSetupCommandHandler : ICommandHandler<VerifyTotpSetupCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMfaService _mfaService;
    private readonly ILogger<VerifyTotpSetupCommandHandler> _logger;

    public VerifyTotpSetupCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IMfaService mfaService,
        ILogger<VerifyTotpSetupCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _mfaService = mfaService;
        _logger = logger;
    }

    public async Task<Result> Handle(VerifyTotpSetupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Verify TOTP code
            if (!_mfaService.ValidateTotpCode(request.Secret, request.Code))
            {
                var failedAuditLog = AuditLog.CreateMfaEvent(
                    user.Id,
                    "SETUP_VERIFICATION_FAILED",
                    "TOTP",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    false,
                    "Invalid TOTP code");

                await _auditLogRepository.AddAsync(failedAuditLog, cancellationToken);

                return Result.Failure("Invalid TOTP code");
            }

            // Create MFA device
            var mfaDevice = new MfaDevice(user.Id, MfaDeviceType.Totp, request.DeviceName, request.Secret);
            
            // Set backup codes if provided
            if (request.BackupCodes != null && request.BackupCodes.Length > 0)
            {
                var encryptedBackupCodes = System.Text.Json.JsonSerializer.Serialize(request.BackupCodes);
                mfaDevice.SetBackupCodes(encryptedBackupCodes); // Should be encrypted in real implementation
            }

            user.AddMfaDevice(mfaDevice);
            user.EnableMfa();

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "SETUP_COMPLETED",
                "TOTP",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            _logger.LogInformation("TOTP setup completed for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP setup for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while verifying TOTP setup");
        }
    }
}
