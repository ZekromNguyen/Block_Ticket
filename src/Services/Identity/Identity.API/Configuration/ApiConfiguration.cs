using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Threading.RateLimiting;

namespace Identity.API.Configuration;

/// <summary>
/// API configuration extensions
/// </summary>
public static class ApiConfiguration
{
    /// <summary>
    /// Configure API versioning
    /// </summary>
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Version"),
                new QueryStringApiVersionReader("version"));
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Configure Swagger/OpenAPI
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BlockTicket Identity API",
                Version = "v1",
                Description = "Identity and authentication service for BlockTicket platform",
                Contact = new OpenApiContact
                {
                    Name = "BlockTicket Support",
                    Email = "support@blockticket.com",
                    Url = new Uri("https://blockticket.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add OAuth2 authentication to Swagger
            options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("/connect/authorize", UriKind.Relative),
                        TokenUrl = new Uri("/connect/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID Connect scope" },
                            { "profile", "Profile information" },
                            { "email", "Email address" },
                            { "api:read", "Read access to API" },
                            { "api:write", "Write access to API" }
                        }
                    }
                }
            });

            // Group endpoints by version
            options.DocInclusionPredicate((version, desc) =>
            {
                if (desc.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
                    return false;

                var methodInfo = controllerActionDescriptor.MethodInfo;
                var versions = methodInfo.DeclaringType?
                    .GetCustomAttributes<ApiVersionAttribute>(true)
                    .SelectMany(attr => attr.Versions);

                return versions?.Any(v => $"v{v}" == version) == true;
            });

            // Custom operation IDs
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
                    return controllerActionDescriptor.MethodInfo.Name;
                return null;
            });
        });

        return services;
    }

    /// <summary>
    /// Configure CORS policy
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // Restrictive policy for admin endpoints
            options.AddPolicy("AdminPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins.Where(o => o.Contains("admin")).ToArray())
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configure rate limiting
    /// </summary>
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // Authentication endpoints
            var authConfig = rateLimitConfig.GetSection("AuthEndpoints");
            options.AddPolicy("AuthPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authConfig.GetValue<int>("PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(authConfig.GetValue<int>("WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // MFA endpoints
            var mfaConfig = rateLimitConfig.GetSection("MfaEndpoints");
            options.AddPolicy("MfaPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = mfaConfig.GetValue<int>("PermitLimit", 5),
                        Window = TimeSpan.FromMinutes(mfaConfig.GetValue<int>("WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Admin endpoints
            var adminConfig = rateLimitConfig.GetSection("AdminEndpoints");
            options.AddPolicy("AdminPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = adminConfig.GetValue<int>("PermitLimit", 100),
                        Window = TimeSpan.FromMinutes(adminConfig.GetValue<int>("WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Global rate limiting
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            };
        });

        return services;
    }

    /// <summary>
    /// Configure health checks
    /// </summary>
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(connectionString, name: "database", tags: new[] { "db", "postgresql" });
        }

        // Redis health check
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache", "redis" });
        }

        // Custom application health check
        healthChecksBuilder.AddCheck<ApplicationHealthCheck>("application", tags: new[] { "application" });

        return services;
    }
}

/// <summary>
/// Custom application health check
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform application-specific health checks here
            // For example: check if critical services are available, validate configuration, etc.

            var data = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown" },
                { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown" }
            };

            return Task.FromResult(HealthCheckResult.Healthy("Application is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check failed", ex));
        }
    }
}
