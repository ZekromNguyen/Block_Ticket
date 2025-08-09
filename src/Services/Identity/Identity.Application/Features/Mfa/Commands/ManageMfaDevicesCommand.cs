using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Mfa.Commands;

public record RemoveMfaDeviceCommand(
    Guid UserId,
    Guid DeviceId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class RemoveMfaDeviceCommandHandler : ICommandHandler<RemoveMfaDeviceCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<RemoveMfaDeviceCommandHandler> _logger;

    public RemoveMfaDeviceCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<RemoveMfaDeviceCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveMfaDeviceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            var device = user.MfaDevices.FirstOrDefault(d => d.Id == request.DeviceId);
            if (device == null)
            {
                return Result.Failure("MFA device not found");
            }

            var deviceType = device.Type.ToString();
            user.RemoveMfaDevice(request.DeviceId);

            // If no more active MFA devices, disable MFA
            if (!user.MfaDevices.Any(d => d.IsActive && d.CanBeUsed()))
            {
                user.DisableMfa();
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "DEVICE_REMOVED",
                deviceType,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("MFA device {DeviceId} removed for user {UserId}", request.DeviceId, user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing MFA device {DeviceId} for user {UserId}", request.DeviceId, request.UserId);
            return Result.Failure("An error occurred while removing MFA device");
        }
    }
}

public record GenerateBackupCodesCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<string[]>>;

public class GenerateBackupCodesCommandHandler : ICommandHandler<GenerateBackupCodesCommand, Result<string[]>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMfaService _mfaService;
    private readonly ILogger<GenerateBackupCodesCommandHandler> _logger;

    public GenerateBackupCodesCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IMfaService mfaService,
        ILogger<GenerateBackupCodesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _mfaService = mfaService;
        _logger = logger;
    }

    public async Task<Result<string[]>> Handle(GenerateBackupCodesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<string[]>.Failure("User not found");
            }

            if (!user.MfaEnabled)
            {
                return Result<string[]>.Failure("MFA must be enabled before generating backup codes");
            }

            // Generate new backup codes
            var backupCodes = _mfaService.GenerateBackupCodes();

            // Remove existing backup codes device if any
            var existingBackupDevice = user.MfaDevices
                .FirstOrDefault(d => d.Type == MfaDeviceType.BackupCodes);

            if (existingBackupDevice != null)
            {
                user.RemoveMfaDevice(existingBackupDevice.Id);
            }

            // Create new backup codes device
            var encryptedCodes = System.Text.Json.JsonSerializer.Serialize(backupCodes);
            var backupDevice = new MfaDevice(user.Id, MfaDeviceType.BackupCodes, "Backup Codes", encryptedCodes);
            user.AddMfaDevice(backupDevice);

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "BACKUP_CODES_GENERATED",
                "BACKUP_CODES",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Backup codes generated for user {UserId}", user.Id);

            return Result<string[]>.Success(backupCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes for user {UserId}", request.UserId);
            return Result<string[]>.Failure("An error occurred while generating backup codes");
        }
    }
}

public record DisableMfaCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class DisableMfaCommandHandler : ICommandHandler<DisableMfaCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<DisableMfaCommandHandler> _logger;

    public DisableMfaCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<DisableMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (!user.MfaEnabled)
            {
                return Result.Failure("MFA is not enabled for this user");
            }

            // Disable MFA and remove all devices
            user.DisableMfa();

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "MFA_DISABLED",
                "ALL",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("MFA disabled for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling MFA for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while disabling MFA");
        }
    }
}
