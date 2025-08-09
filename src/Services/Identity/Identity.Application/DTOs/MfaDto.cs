namespace Identity.Application.DTOs;

public record MfaDeviceDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SetupTotpDto
{
    public string Secret { get; init; } = string.Empty;
    public string QrCodeUri { get; init; } = string.Empty;
    public string[] BackupCodes { get; init; } = Array.Empty<string>();
}

public record VerifyTotpSetupDto
{
    public string Secret { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
}

public record SetupEmailOtpDto
{
    public string Email { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
}

public record VerifyEmailOtpDto
{
    public string Code { get; init; } = string.Empty;
}

public record SetupWebAuthnDto
{
    public string DeviceName { get; init; } = string.Empty;
    public string Challenge { get; init; } = string.Empty;
    public string CredentialId { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
}

public record WebAuthnChallengeDto
{
    public string Challenge { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record VerifyWebAuthnDto
{
    public string Challenge { get; init; } = string.Empty;
    public string CredentialId { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}

public record GenerateBackupCodesDto
{
    public string[] BackupCodes { get; init; } = Array.Empty<string>();
    public DateTime GeneratedAt { get; init; }
}
