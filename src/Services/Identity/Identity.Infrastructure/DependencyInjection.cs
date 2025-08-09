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
using Identity.Domain.Services;

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

// Placeholder services that would be implemented based on requirements
public class EmailService : Identity.Domain.Services.IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        // TODO: Implement email sending (SendGrid, SMTP, etc.)
        _logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);
        await Task.Delay(100); // Simulate async operation
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl)
    {
        var subject = "Confirm your email address";
        var body = $"Please confirm your email address by clicking this link: {callbackUrl}?token={confirmationToken}";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetToken, string callbackUrl)
    {
        var subject = "Reset your password";
        var body = $"Reset your password by clicking this link: {callbackUrl}?token={resetToken}";
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendMfaCodeAsync(string email, string code)
    {
        var subject = "Your verification code";
        var body = $"Your verification code is: {code}";
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
        var body = $"Welcome {name}! Thank you for joining BlockTicket.";
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
