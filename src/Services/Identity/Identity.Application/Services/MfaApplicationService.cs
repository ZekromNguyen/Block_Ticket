using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Mfa.Commands;
using Identity.Application.Features.Mfa.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class MfaApplicationService : IMfaApplicationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<MfaApplicationService> _logger;

    public MfaApplicationService(IMediator mediator, ILogger<MfaApplicationService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<SetupTotpDto>> SetupTotpAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var command = new SetupTotpCommand(userId, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> VerifyTotpSetupAsync(Guid userId, VerifyTotpSetupDto verifyTotpDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new VerifyTotpSetupCommand(
            userId,
            verifyTotpDto.Secret,
            verifyTotpDto.Code,
            verifyTotpDto.DeviceName,
            null, // BackupCodes - will be implemented later
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> DisableTotpAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("DisableTotpAsync called for user {UserId}", userId);
        return Result.Failure("Not implemented yet");
    }

    public async Task<Result> SetupEmailOtpAsync(Guid userId, SetupEmailOtpDto setupEmailOtpDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new SetupEmailOtpCommand(userId, setupEmailOtpDto.DeviceName, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> SendEmailOtpAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        // For email OTP, the OTP is sent during setup, not separately
        _logger.LogInformation("SendEmailOtpAsync called for user {UserId}", userId);
        return Result.Failure("Email OTP is sent automatically during setup");
    }

    public async Task<Result> VerifyEmailOtpAsync(Guid userId, VerifyEmailOtpDto verifyEmailOtpDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new VerifyEmailOtpSetupCommand(userId, verifyEmailOtpDto.Code, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result<WebAuthnChallengeDto>> InitiateWebAuthnSetupAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var command = new InitiateWebAuthnSetupCommand(userId, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> CompleteWebAuthnSetupAsync(Guid userId, SetupWebAuthnDto setupWebAuthnDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new CompleteWebAuthnSetupCommand(
            userId,
            setupWebAuthnDto.DeviceName,
            setupWebAuthnDto.Challenge,
            setupWebAuthnDto.CredentialId,
            setupWebAuthnDto.PublicKey,
            ipAddress,
            userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result<WebAuthnChallengeDto>> InitiateWebAuthnVerificationAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        // For verification during login, we would use the same challenge generation
        var command = new InitiateWebAuthnSetupCommand(userId, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> VerifyWebAuthnAsync(Guid userId, VerifyWebAuthnDto verifyWebAuthnDto, string? ipAddress = null, string? userAgent = null)
    {
        // This would be handled in the login flow, not as a separate command
        _logger.LogInformation("VerifyWebAuthnAsync called for user {UserId}", userId);
        return Result.Failure("WebAuthn verification is handled during login");
    }

    public async Task<Result<GenerateBackupCodesDto>> GenerateBackupCodesAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var command = new GenerateBackupCodesCommand(userId, ipAddress, userAgent);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Result<GenerateBackupCodesDto>.Success(new GenerateBackupCodesDto
            {
                BackupCodes = result.Value!,
                GeneratedAt = DateTime.UtcNow
            });
        }

        return Result<GenerateBackupCodesDto>.Failure(result.Error);
    }

    public async Task<Result> VerifyBackupCodeAsync(Guid userId, string backupCode, string? ipAddress = null, string? userAgent = null)
    {
        // Backup code verification would be handled during login flow
        _logger.LogInformation("VerifyBackupCodeAsync called for user {UserId}", userId);
        return Result.Failure("Backup code verification is handled during login");
    }

    public async Task<Result<IEnumerable<MfaDeviceDto>>> GetUserMfaDevicesAsync(Guid userId)
    {
        var query = new GetUserMfaDevicesQuery(userId);
        return await _mediator.Send(query);
    }

    public async Task<Result> RemoveMfaDeviceAsync(Guid userId, Guid deviceId, string? ipAddress = null, string? userAgent = null)
    {
        var command = new RemoveMfaDeviceCommand(userId, deviceId, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> UpdateMfaDeviceNameAsync(Guid userId, Guid deviceId, string newName, string? ipAddress = null, string? userAgent = null)
    {
        // This would require a separate command - for now, return not implemented
        _logger.LogInformation("UpdateMfaDeviceNameAsync called for user {UserId}, device {DeviceId}", userId, deviceId);
        return Result.Failure("Device name update not implemented yet");
    }

    public async Task<Result> EnableMfaAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        // MFA is enabled automatically when first device is added
        _logger.LogInformation("EnableMfaAsync called for user {UserId}", userId);
        return Result.Failure("MFA is enabled automatically when adding first device");
    }

    public async Task<Result> DisableMfaAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var command = new DisableMfaCommand(userId, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result<bool>> IsMfaEnabledAsync(Guid userId)
    {
        var query = new GetMfaStatusQuery(userId);
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            return Result<bool>.Success(result.Value!.IsEnabled);
        }

        return Result<bool>.Failure(result.Error);
    }
}
