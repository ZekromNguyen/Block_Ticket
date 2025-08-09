using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using Identity.Domain.Services;

using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Mfa.Commands;

public record SetupEmailOtpCommand(
    Guid UserId,
    string DeviceName,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class SetupEmailOtpCommandHandler : ICommandHandler<SetupEmailOtpCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMfaService _mfaService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SetupEmailOtpCommandHandler> _logger;

    public SetupEmailOtpCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IMfaService mfaService,
        IEmailService emailService,
        ILogger<SetupEmailOtpCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _mfaService = mfaService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(SetupEmailOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Check if user already has an active Email OTP device
            var existingEmailOtp = user.MfaDevices
                .FirstOrDefault(d => d.Type == MfaDeviceType.EmailOtp && d.IsActive);

            if (existingEmailOtp != null)
            {
                return Result.Failure("Email OTP is already configured for this user");
            }

            // Generate OTP for verification
            var otp = _mfaService.GenerateEmailOtp();
            
            // Create MFA device (store OTP temporarily for verification)
            var mfaDevice = new MfaDevice(user.Id, MfaDeviceType.EmailOtp, request.DeviceName, otp);
            mfaDevice.Deactivate(); // Keep inactive until verified

            user.AddMfaDevice(mfaDevice);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Send OTP via email
            await _emailService.SendMfaCodeAsync(user.Email.Value, otp);

            // Create audit log
            var auditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "SETUP_INITIATED",
                "EMAIL_OTP",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Email OTP setup initiated for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up Email OTP for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while setting up Email OTP");
        }
    }
}

public record VerifyEmailOtpSetupCommand(
    Guid UserId,
    string Code,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class VerifyEmailOtpSetupCommandHandler : ICommandHandler<VerifyEmailOtpSetupCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMfaService _mfaService;
    private readonly ILogger<VerifyEmailOtpSetupCommandHandler> _logger;

    public VerifyEmailOtpSetupCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IMfaService mfaService,
        ILogger<VerifyEmailOtpSetupCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _mfaService = mfaService;
        _logger = logger;
    }

    public async Task<Result> Handle(VerifyEmailOtpSetupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Find the inactive Email OTP device
            var emailOtpDevice = user.MfaDevices
                .FirstOrDefault(d => d.Type == MfaDeviceType.EmailOtp && !d.IsActive);

            if (emailOtpDevice == null)
            {
                return Result.Failure("No Email OTP setup found");
            }

            // Verify the OTP code
            var isValid = _mfaService.ValidateEmailOtp(
                emailOtpDevice.Secret, 
                request.Code, 
                emailOtpDevice.CreatedAt, 
                TimeSpan.FromMinutes(5));

            if (!isValid)
            {
                var failedAuditLog = AuditLog.CreateMfaEvent(
                    user.Id,
                    "SETUP_VERIFICATION_FAILED",
                    "EMAIL_OTP",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    false,
                    "Invalid OTP code");

                await _auditLogRepository.AddAsync(failedAuditLog, cancellationToken);

                return Result.Failure("Invalid OTP code");
            }

            // Activate the device
            emailOtpDevice.Activate();
            user.EnableMfa();

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = AuditLog.CreateMfaEvent(
                user.Id,
                "SETUP_COMPLETED",
                "EMAIL_OTP",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true);

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            _logger.LogInformation("Email OTP setup completed for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Email OTP setup for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while verifying Email OTP setup");
        }
    }
}
