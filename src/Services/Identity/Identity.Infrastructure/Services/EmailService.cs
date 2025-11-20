using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP configuration is incomplete.");
                return;
            }

            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
            client.Timeout = 30000; // 30 seconds timeout

            using var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromEmail ?? smtpUsername, fromName ?? "BlockTicket");
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl)
    {
        var subject = "Confirm your email address - BlockTicket";
        var body = $"Please confirm your email address by clicking the link below: <a href='{callbackUrl}?token={confirmationToken}'>Confirm Email</a>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetToken, string callbackUrl)
    {
        var subject = "Reset your password - BlockTicket";
        var body = $"You requested to reset your password. Click the link below to reset it: <a href='{callbackUrl}?token={resetToken}'>Reset Password</a>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendMfaCodeAsync(string email, string code)
    {
        var subject = "Your BlockTicket verification code";
        var body = $"Your BlockTicket verification code is: {code}";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendSecurityAlertAsync(string email, string alertMessage)
    {
        var subject = "Security Alert - BlockTicket";
        var body = $"Security Alert: {alertMessage}";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var subject = "Welcome to BlockTicket!";
        var body = $"Welcome to BlockTicket, {name}!";
        await SendEmailAsync(email, subject, body);
    }
}
