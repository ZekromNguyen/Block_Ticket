namespace Identity.Domain.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl);
    Task SendPasswordResetAsync(string email, string resetToken, string callbackUrl);
    Task SendMfaCodeAsync(string email, string code);
    Task SendSecurityAlertAsync(string email, string alertMessage);
    Task SendWelcomeEmailAsync(string email, string name);
}

public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendMfaCodeAsync(string phoneNumber, string code);
    Task SendSecurityAlertAsync(string phoneNumber, string alertMessage);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string Hash(string input);
    bool VerifyHash(string input, string hash);
}
