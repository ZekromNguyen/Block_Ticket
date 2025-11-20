using Identity.Domain.Interfaces;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Infrastructure.Configuration;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Add Redis Distributed Cache
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
        }
        
        // Add OpenIddict configuration
        services.AddOpenIddictConfiguration(configuration, environment);

        services.AddScoped<ISecurityNotificationService, SecurityNotificationService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionManagementService, SessionManagementService>();
        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();
        services.AddScoped<IDiscordNotificationService, DiscordNotificationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, SmsService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<ISuspiciousActivityRepository, SuspiciousActivityRepository>();
        services.AddScoped<IAccountLockoutRepository, AccountLockoutRepository>();
        services.AddScoped<IReferenceTokenRepository, ReferenceTokenRepository>();
        services.AddScoped<IOAuthClientRepository, OAuthClientRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddHttpClient();

        return services;
    }
}
