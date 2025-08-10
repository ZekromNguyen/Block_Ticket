using Identity.Application.Common.Interfaces;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Infrastructure.Configuration;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Add Entity Framework
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), 
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                });

            // Enable sensitive data logging in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOAuthClientRepository, OAuthClientRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<IAccountLockoutRepository, AccountLockoutRepository>();
        services.AddScoped<ISuspiciousActivityRepository, SuspiciousActivityRepository>();
        services.AddScoped<IReferenceTokenRepository, ReferenceTokenRepository>();

        // Add Domain Services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IRiskAnalysisService, RiskAnalysisService>();

        // Add External Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IEncryptionService, EncryptionService>();

        // Add Caching
        AddCaching(services, configuration);

        // Add MassTransit (temporarily disabled for migration)
        // TODO: Enable MassTransit after setting up RabbitMQ

        // Add OpenIddict
        services.AddOpenIddictConfiguration(configuration, environment);

        // Add Background Services
        services.AddHostedService<SessionCleanupService>();
        services.AddHostedService<AuditLogCleanupService>();
        services.AddHostedService<SecurityMonitoringService>();

        return services;
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        // Temporarily use in-memory cache (TODO: Enable Redis later)
        services.AddMemoryCache();
        services.AddSingleton<IDistributedCache>(provider =>
            new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
                Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions())));
    }
}

// Email service implementation using SMTP
public class EmailService : Identity.Domain.Services.IEmailService
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

            _logger.LogInformation("Starting email send process to {To} with subject {Subject}", to, subject);
            _logger.LogDebug("SMTP Configuration - Host: {Host}, Port: {Port}, Username: {Username}, FromEmail: {FromEmail}",
                smtpHost, smtpPort, smtpUsername, fromEmail);

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP configuration is incomplete. Host: {Host}, Username: {Username}, Password: {HasPassword}",
                    smtpHost, smtpUsername, !string.IsNullOrEmpty(smtpPassword));
                return;
            }

            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
            client.Timeout = 30000; // 30 seconds timeout
            client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;

            _logger.LogDebug("SMTP client configured - SSL: {EnableSsl}, Timeout: {Timeout}ms", client.EnableSsl, client.Timeout);

            using var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromEmail ?? smtpUsername, fromName ?? "BlockTicket");
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            _logger.LogDebug("Email message created - From: {From}, To: {To}, IsHtml: {IsHtml}",
                message.From.Address, to, isHtml);

            _logger.LogInformation("Attempting to send email via SMTP...");
            await client.SendMailAsync(message);
            _logger.LogInformation("✅ Email sent successfully to {To} with subject {Subject}", to, subject);
        }
        catch (System.Net.Mail.SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "❌ SMTP Error sending email to {To}. StatusCode: {StatusCode}, Message: {Message}",
                to, smtpEx.StatusCode, smtpEx.Message);
            throw;
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            _logger.LogError(socketEx, "❌ Network/Socket Error sending email to {To}. ErrorCode: {ErrorCode}",
                to, socketEx.ErrorCode);
            throw;
        }
        catch (System.Security.Authentication.AuthenticationException authEx)
        {
            _logger.LogError(authEx, "❌ Authentication Error sending email to {To}. Check Gmail App Password and 2FA settings", to);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error sending email to {To} with subject {Subject}", to, subject);
            throw;
        }
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl)
    {
        var subject = "Confirm your email address - BlockTicket";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to BlockTicket!</h2>
                <p>Please confirm your email address by clicking the link below:</p>
                <p><a href='{callbackUrl}?token={confirmationToken}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                <p>If you didn't create this account, please ignore this email.</p>
                <p>Best regards,<br>BlockTicket Team</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetToken, string callbackUrl)
    {
        var subject = "Reset your password - BlockTicket";
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>You requested to reset your password. Click the link below to reset it:</p>
                <p><a href='{callbackUrl}?token={resetToken}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this, please ignore this email.</p>
                <p>Best regards,<br>BlockTicket Team</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendMfaCodeAsync(string email, string code)
    {
        var subject = "Your BlockTicket verification code";
        var body = $@"
            <html>
            <body>
                <h2>Your Verification Code</h2>
                <p>Your BlockTicket verification code is:</p>
                <h1 style='color: #007bff; font-size: 32px; letter-spacing: 5px; text-align: center; margin: 20px 0;'>{code}</h1>
                <p>This code will expire in 5 minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
                <p>Best regards,<br>BlockTicket Team</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendSecurityAlertAsync(string email, string alertMessage)
    {
        var subject = "Security Alert - BlockTicket";
        var body = $@"
            <html>
            <body>
                <h2>Security Alert</h2>
                <p><strong>Alert:</strong> {alertMessage}</p>
                <p>If this was you, no action is needed. If not, please secure your account immediately.</p>
                <p>Best regards,<br>BlockTicket Security Team</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var subject = "Welcome to BlockTicket!";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to BlockTicket, {name}!</h2>
                <p>Thank you for joining BlockTicket, the future of event ticketing on the blockchain.</p>
                <p>You can now:</p>
                <ul>
                    <li>Browse and purchase event tickets</li>
                    <li>Manage your digital wallet</li>
                    <li>Trade tickets securely</li>
                </ul>
                <p>Get started by exploring our platform!</p>
                <p>Best regards,<br>BlockTicket Team</p>
            </body>
            </html>";
        await SendEmailAsync(email, subject, body);
    }
}

public class SmsService : Identity.Domain.Services.ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task SendSmsAsync(string to, string message)
    {
        // TODO: Implement SMS sending (Twilio, AWS SNS, etc.)
        _logger.LogInformation("Sending SMS to {To} with message: {Message}", to, message);
        await Task.Delay(100); // Simulate async operation
    }

    public async Task SendMfaCodeAsync(string phoneNumber, string code)
    {
        var message = $"Your BlockTicket verification code is: {code}";
        await SendSmsAsync(phoneNumber, message);
    }

    public async Task SendSecurityAlertAsync(string phoneNumber, string alertMessage)
    {
        var message = $"BlockTicket Security Alert: {alertMessage}";
        await SendSmsAsync(phoneNumber, message);
    }
}

public class EncryptionService : Identity.Domain.Services.IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        // TODO: Implement AES encryption for sensitive data
        _logger.LogDebug("Encrypting data");
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText)); // Placeholder
    }

    public string Decrypt(string cipherText)
    {
        // TODO: Implement AES decryption
        _logger.LogDebug("Decrypting data");
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cipherText)); // Placeholder
    }

    public string Hash(string input)
    {
        // TODO: Implement SHA-256 hashing
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyHash(string input, string hash)
    {
        // TODO: Implement hash verification
        var inputHash = Hash(input);
        return inputHash == hash;
    }
}
