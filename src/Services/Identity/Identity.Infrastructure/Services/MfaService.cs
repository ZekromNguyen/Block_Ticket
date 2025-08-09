using Identity.Domain.Services;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

public class MfaService : IMfaService
{
    private readonly ILogger<MfaService> _logger;

    public MfaService(ILogger<MfaService> logger)
    {
        _logger = logger;
    }

    public string GenerateTotpSecret()
    {
        try
        {
            // Generate a random 20-byte secret (160 bits)
            byte[] secretBytes = new byte[20];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(secretBytes);

            // Convert to Base32 string (required for TOTP)
            return Base32Encoding.ToString(secretBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TOTP secret");
            throw;
        }
    }

    public string GenerateQrCodeUri(string email, string secret, string issuer = "BlockTicket")
    {
        try
        {
            // Create the TOTP URI according to Google Authenticator format
            var totpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
            
            // Generate QR code
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            // Return as data URI for easy embedding in HTML
            var qrCodeBytes = qrCode.GetGraphic(20);
            var qrCodeImage = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{qrCodeImage}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code URI for email {Email}", email);
            throw;
        }
    }

    public bool ValidateTotpCode(string secret, string code, int windowSize = 1)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);

            // Validate with time window (allows for clock drift)
            return totp.VerifyTotp(code, out _, new VerificationWindow(windowSize, windowSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    public string GenerateEmailOtp()
    {
        try
        {
            // Generate a 6-digit OTP
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            
            int randomNumber = Math.Abs(BitConverter.ToInt32(randomBytes, 0));
            return (randomNumber % 1000000).ToString("D6");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email OTP");
            throw;
        }
    }

    public bool ValidateEmailOtp(string storedOtp, string providedOtp, DateTime generatedAt, TimeSpan validityPeriod)
    {
        if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(providedOtp))
            return false;

        try
        {
            // Check if OTP has expired
            if (DateTime.UtcNow > generatedAt.Add(validityPeriod))
            {
                _logger.LogDebug("Email OTP has expired");
                return false;
            }

            // Compare OTPs (constant-time comparison to prevent timing attacks)
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedOtp),
                Encoding.UTF8.GetBytes(providedOtp));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email OTP");
            return false;
        }
    }

    public string[] GenerateBackupCodes(int count = 10)
    {
        try
        {
            var backupCodes = new string[count];
            using var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < count; i++)
            {
                // Generate 8-character alphanumeric backup codes
                byte[] randomBytes = new byte[6];
                rng.GetBytes(randomBytes);
                
                var code = Convert.ToBase64String(randomBytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 8)
                    .ToUpperInvariant();
                
                backupCodes[i] = code;
            }

            return backupCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes");
            throw;
        }
    }

    public bool ValidateBackupCode(string[] backupCodes, string providedCode)
    {
        if (backupCodes == null || backupCodes.Length == 0 || string.IsNullOrEmpty(providedCode))
            return false;

        try
        {
            var normalizedProvidedCode = providedCode.ToUpperInvariant().Replace("-", "").Replace(" ", "");
            
            foreach (var backupCode in backupCodes)
            {
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(backupCode),
                    Encoding.UTF8.GetBytes(normalizedProvidedCode)))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup code");
            return false;
        }
    }

    public async Task<bool> ValidateWebAuthnAsync(string credentialId, string signature, string challenge)
    {
        try
        {
            // TODO: Implement WebAuthn/FIDO2 validation
            // This would typically involve:
            // 1. Retrieving the public key for the credential ID
            // 2. Verifying the signature against the challenge
            // 3. Checking the authenticator data
            
            _logger.LogWarning("WebAuthn validation not yet implemented");
            await Task.Delay(100); // Simulate async operation
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating WebAuthn credential");
            return false;
        }
    }
}
