using Event.API.Filters;
using Event.Application.Common.Interfaces;
using Event.Infrastructure.Middleware;
using Event.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Event.API.Configuration;

/// <summary>
/// Configuration for Row-Level Security and multi-tenancy
/// </summary>
public static class RowLevelSecurityConfiguration
{
    /// <summary>
    /// Configures RLS services and middleware
    /// </summary>
    public static IServiceCollection AddRowLevelSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Add organization authorization services
        services.AddOrganizationAuthorization();

        // Configure JWT authentication with organization claims
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
                    ClockSkew = TimeSpan.Zero,
                    
                    // Ensure organization claims are preserved
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                // Add custom claim validation
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Validate that organization_id claim exists
                        var orgClaim = context.Principal?.FindFirst("organization_id") ?? 
                                      context.Principal?.FindFirst("org_id");
                        
                        if (orgClaim == null)
                        {
                            context.Fail("Missing organization claim in JWT token");
                            return Task.CompletedTask;
                        }

                        if (!Guid.TryParse(orgClaim.Value, out _))
                        {
                            context.Fail("Invalid organization ID format in JWT token");
                            return Task.CompletedTask;
                        }

                        return Task.CompletedTask;
                    },
                    
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication and organization context
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("organization_id")
                .Build();

            // Admin policy for cross-organization access (if needed)
            options.AddPolicy("AdminAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "admin", "super_admin"));

            // Organization admin policy
            options.AddPolicy("OrganizationAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("organization_id")
                      .RequireClaim("role", "org_admin", "admin"));
        });

        return services;
    }

    /// <summary>
    /// Configures RLS middleware and filters
    /// </summary>
    public static IApplicationBuilder UseRowLevelSecurity(this IApplicationBuilder app)
    {
        // Add organization context middleware early in the pipeline
        app.UseOrganizationContext();

        return app;
    }

    /// <summary>
    /// Configures MVC options for RLS
    /// </summary>
    public static void ConfigureRowLevelSecurityMvc(this MvcOptions options)
    {
        // Add global organization authorization
        options.ConfigureOrganizationAuthorization();
    }

    /// <summary>
    /// Validates RLS configuration
    /// </summary>
    public static void ValidateRowLevelSecurityConfiguration(this IConfiguration configuration)
    {
        var requiredSettings = new[]
        {
            "Jwt:Key",
            "Jwt:Issuer",
            "Jwt:Audience",
            "ConnectionStrings:DefaultConnection"
        };

        var missingSettings = requiredSettings
            .Where(setting => string.IsNullOrEmpty(configuration[setting]))
            .ToList();

        if (missingSettings.Any())
        {
            throw new InvalidOperationException(
                $"Missing required configuration settings for RLS: {string.Join(", ", missingSettings)}");
        }

        // Validate database connection supports PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!connectionString?.Contains("Host=") == true && !connectionString?.Contains("Server=") == true)
        {
            throw new InvalidOperationException("RLS requires PostgreSQL database connection");
        }
    }
}

/// <summary>
/// Extensions for organization context in controllers
/// </summary>
public static class ControllerOrganizationExtensions
{
    /// <summary>
    /// Gets the current organization ID from the controller context
    /// </summary>
    public static Guid GetCurrentOrganizationId(this ControllerBase controller)
    {
        var orgClaim = controller.User.FindFirst("organization_id") ?? 
                      controller.User.FindFirst("org_id");
        
        if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("No organization context available");
    }

    /// <summary>
    /// Gets the current user ID from the controller context
    /// </summary>
    public static Guid GetCurrentUserId(this ControllerBase controller)
    {
        var userClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier) ??
                       controller.User.FindFirst("sub") ??
                       controller.User.FindFirst("user_id");

        if (userClaim != null && Guid.TryParse(userClaim.Value, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("No user context available");
    }

    /// <summary>
    /// Validates that the user has access to the specified organization
    /// </summary>
    public static void ValidateOrganizationAccess(this ControllerBase controller, Guid organizationId)
    {
        var currentOrgId = controller.GetCurrentOrganizationId();
        if (currentOrgId != organizationId)
        {
            throw new UnauthorizedAccessException($"Access denied to organization {organizationId}");
        }
    }

    /// <summary>
    /// Validates that the entity belongs to the current user's organization
    /// </summary>
    public static void ValidateEntityOrganization<T>(this ControllerBase controller, T entity) where T : class
    {
        var currentOrgId = controller.GetCurrentOrganizationId();
        var organizationIdProperty = typeof(T).GetProperty("OrganizationId");
        
        if (organizationIdProperty?.GetValue(entity) is Guid entityOrgId)
        {
            if (entityOrgId != currentOrgId)
            {
                throw new UnauthorizedAccessException("Access denied: Entity belongs to a different organization");
            }
        }
    }
}

/// <summary>
/// Health check for RLS configuration
/// </summary>
public class RowLevelSecurityHealthCheck : IHealthCheck
{
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly EventDbContext _dbContext;
    private readonly ILogger<RowLevelSecurityHealthCheck> _logger;

    public RowLevelSecurityHealthCheck(
        IOrganizationContextProvider organizationContextProvider,
        EventDbContext dbContext,
        ILogger<RowLevelSecurityHealthCheck> logger)
    {
        _organizationContextProvider = organizationContextProvider;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connection
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // Test RLS function exists (PostgreSQL specific)
            if (_dbContext.Database.IsNpgsql())
            {
                var rlsFunctionExists = await _dbContext.Database
                    .SqlQueryRaw<bool>("SELECT EXISTS(SELECT 1 FROM pg_proc WHERE proname = 'current_organization_id')")
                    .FirstOrDefaultAsync(cancellationToken);

                if (!rlsFunctionExists)
                {
                    return HealthCheckResult.Degraded("RLS function not found in database");
                }
            }

            return HealthCheckResult.Healthy("RLS configuration is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RLS health check failed");
            return HealthCheckResult.Unhealthy("RLS configuration check failed", ex);
        }
    }
}
