using Identity.Application.Common.Models;
using Identity.Application.DTOs;

namespace Identity.Application.Services;

public interface IMfaApplicationService
{
    // TOTP Management
    Task<Result<SetupTotpDto>> SetupTotpAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> VerifyTotpSetupAsync(Guid userId, VerifyTotpSetupDto verifyTotpDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> DisableTotpAsync(Guid userId, string? ipAddress = null, string? userAgent = null);

    // Email OTP Management
    Task<Result> SetupEmailOtpAsync(Guid userId, SetupEmailOtpDto setupEmailOtpDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> SendEmailOtpAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> VerifyEmailOtpAsync(Guid userId, VerifyEmailOtpDto verifyEmailOtpDto, string? ipAddress = null, string? userAgent = null);

    // WebAuthn Management
    Task<Result<WebAuthnChallengeDto>> InitiateWebAuthnSetupAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> CompleteWebAuthnSetupAsync(Guid userId, SetupWebAuthnDto setupWebAuthnDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<WebAuthnChallengeDto>> InitiateWebAuthnVerificationAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> VerifyWebAuthnAsync(Guid userId, VerifyWebAuthnDto verifyWebAuthnDto, string? ipAddress = null, string? userAgent = null);

    // Backup Codes Management
    Task<Result<GenerateBackupCodesDto>> GenerateBackupCodesAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> VerifyBackupCodeAsync(Guid userId, string backupCode, string? ipAddress = null, string? userAgent = null);

    // Device Management
    Task<Result<IEnumerable<MfaDeviceDto>>> GetUserMfaDevicesAsync(Guid userId);
    Task<Result> RemoveMfaDeviceAsync(Guid userId, Guid deviceId, string? ipAddress = null, string? userAgent = null);
    Task<Result> UpdateMfaDeviceNameAsync(Guid userId, Guid deviceId, string newName, string? ipAddress = null, string? userAgent = null);

    // MFA Status
    Task<Result> EnableMfaAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result> DisableMfaAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result<bool>> IsMfaEnabledAsync(Guid userId);
}
