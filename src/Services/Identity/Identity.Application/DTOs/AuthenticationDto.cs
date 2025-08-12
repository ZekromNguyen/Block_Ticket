namespace Identity.Application.DTOs;

public record LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? MfaCode { get; init; }
    public string? DeviceInfo { get; init; }
    public bool RememberMe { get; init; }
}

public record LoginResultDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool RequiresMfa { get; init; }
    public string[]? AvailableMfaMethods { get; init; }
}

public record RefreshTokenDto
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record MfaChallengeDto
{
    public string ChallengeId { get; init; } = string.Empty;
    public string[] AvailableMethods { get; init; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; init; }
}

public record MfaVerificationDto
{
    public string ChallengeId { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public record ForgotPasswordDto
{
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordDto
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}

public record ResendEmailConfirmationDto
{
    public string Email { get; init; } = string.Empty;
}
