using Identity.Domain.Entities;

namespace Identity.Domain.Services;

public interface IMfaService
{
    string GenerateTotpSecret();
    string GenerateQrCodeUri(string email, string secret, string issuer = "BlockTicket");
    bool ValidateTotpCode(string secret, string code, int windowSize = 1);
    string GenerateEmailOtp();
    bool ValidateEmailOtp(string storedOtp, string providedOtp, DateTime generatedAt, TimeSpan validityPeriod);
    string[] GenerateBackupCodes(int count = 10);
    bool ValidateBackupCode(string[] backupCodes, string providedCode);
    Task<bool> ValidateWebAuthnAsync(string credentialId, string signature, string challenge);
}
