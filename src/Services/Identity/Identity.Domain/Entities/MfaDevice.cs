using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class MfaDevice : BaseEntity
{
    public Guid UserId { get; private set; }
    public MfaDeviceType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty; // Encrypted
    public bool IsActive { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public int UsageCount { get; private set; }
    public string? BackupCodes { get; private set; } // Encrypted JSON array
    public DateTime? BackupCodesGeneratedAt { get; private set; }

    private MfaDevice() { } // For EF Core

    public MfaDevice(Guid userId, MfaDeviceType type, string name, string secret)
    {
        UserId = userId;
        Type = type;
        Name = name;
        Secret = secret;
        IsActive = true;
        UsageCount = 0;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        UsageCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBackupCodes(string encryptedBackupCodes)
    {
        BackupCodes = encryptedBackupCodes;
        BackupCodesGeneratedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearBackupCodes()
    {
        BackupCodes = null;
        BackupCodesGeneratedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeUsed()
    {
        return IsActive && Type != MfaDeviceType.BackupCodes;
    }
}

public enum MfaDeviceType
{
    Totp = 0,           // Time-based One-Time Password (Google Authenticator, etc.)
    EmailOtp = 1,       // Email-based OTP
    SmsOtp = 2,         // SMS-based OTP
    WebAuthn = 3,       // FIDO2/WebAuthn
    BackupCodes = 4     // Backup recovery codes
}
