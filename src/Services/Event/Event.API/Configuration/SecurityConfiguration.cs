using Event.Infrastructure.Security.Middleware;
using Event.Infrastructure.Security.Monitoring;
using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Middleware;
using Event.Infrastructure.Security.RateLimiting.Models;
using Event.Infrastructure.Security.RateLimiting.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Reflection;

namespace Event.API.Configuration;

/// <summary>
/// Configuration for comprehensive security features
/// </summary>
public static class SecurityConfiguration
{
    /// <summary>
    /// Adds comprehensive security services
    /// </summary>
    public static IServiceCollection AddComprehensiveSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Add rate limiting
        services.AddRateLimiting(configuration);

        // Add input validation
        services.AddInputValidation();

        // Add security monitoring
        services.AddSecurityMonitoring();

        // Add security headers configuration
        services.AddSecurityHeaders(configuration);

        // Add request size limits
        services.AddRequestSizeLimits(configuration);

        // Configure forwarded headers for proper IP detection
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiting services
    /// </summary>
    private static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure rate limiting options
        services.Configure<RateLimitConfiguration>(configuration.GetSection("RateLimiting"));

        // Add Redis for distributed rate limiting
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            config.ConnectTimeout = 5000;
            return ConnectionMultiplexer.Connect(config);
        });

        // Add rate limiting services
        services.AddScoped<IRateLimitStorage, RedisRateLimitStorage>();
        services.AddScoped<IRateLimitKeyGenerator, RateLimitKeyGenerator>();
        services.AddScoped<IRateLimitRuleProvider, ConfigurationRateLimitRuleProvider>();
        services.AddScoped<IRateLimiter, RateLimiter>();

        return services;
    }

    /// <summary>
    /// Adds input validation services
    /// </summary>
    private static IServiceCollection AddInputValidation(this IServiceCollection services)
    {
        // Add FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add custom validation filters
        services.AddScoped<ValidationActionFilter>();

        return services;
    }

    /// <summary>
    /// Adds security monitoring services
    /// </summary>
    private static IServiceCollection AddSecurityMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<SecurityMetrics>();
        services.AddScoped<IRateLimitMetrics>(provider => provider.GetRequiredService<SecurityMetrics>());

        return services;
    }

    /// <summary>
    /// Adds security headers configuration
    /// </summary>
    private static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(configuration.GetSection("SecurityHeaders"));
        return services;
    }

    /// <summary>
    /// Adds request size limit configuration
    /// </summary>
    private static IServiceCollection AddRequestSizeLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RequestSizeLimitOptions>(configuration.GetSection("RequestSizeLimits"));
        return services;
    }

    /// <summary>
    /// Configures the security middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseComprehensiveSecurity(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Security headers should be first
        app.UseSecurityHeaders();

        // Forwarded headers for proper IP detection
        app.UseForwardedHeaders();

        // Request size limits
        app.UseRequestSizeLimit();

        // Rate limiting (before authentication)
        app.UseRateLimiting();

        return app;
    }

    /// <summary>
    /// Gets default rate limiting rules
    /// </summary>
    public static List<RateLimitRule> GetDefaultRateLimitRules()
    {
        return new List<RateLimitRule>
        {
            // IP-based rate limiting: 100 requests per minute
            new RateLimitRule
            {
                Id = "ip_global",
                Type = RateLimitType.IpAddress,
                Limit = 100,
                WindowSizeInSeconds = 60,
                Priority = 1,
                IsEnabled = true,
                CustomMessage = "Too many requests from your IP address. Please try again later."
            },

            // Client-based rate limiting: 500 requests per hour
            new RateLimitRule
            {
                Id = "client_global",
                Type = RateLimitType.Client,
                Limit = 500,
                WindowSizeInSeconds = 3600,
                Priority = 2,
                IsEnabled = true,
                CustomMessage = "Client rate limit exceeded. Please reduce request frequency."
            },

            // Organization-based rate limiting: 1000 requests per hour
            new RateLimitRule
            {
                Id = "organization_global",
                Type = RateLimitType.Organization,
                Limit = 1000,
                WindowSizeInSeconds = 3600,
                Priority = 3,
                IsEnabled = true,
                CustomMessage = "Organization rate limit exceeded."
            },

            // Reservation endpoints: 10 requests per minute (stricter)
            new RateLimitRule
            {
                Id = "reservations_endpoint",
                Type = RateLimitType.Endpoint,
                Limit = 10,
                WindowSizeInSeconds = 60,
                EndpointPattern = "/api/reservations",
                HttpMethods = new[] { "POST", "PUT", "DELETE" },
                Priority = 10,
                IsEnabled = true,
                CustomMessage = "Reservation rate limit exceeded. Please wait before making another reservation."
            },

            // Pricing endpoints: 20 requests per minute
            new RateLimitRule
            {
                Id = "pricing_endpoint",
                Type = RateLimitType.Endpoint,
                Limit = 20,
                WindowSizeInSeconds = 60,
                EndpointPattern = "/api/pricing",
                Priority = 9,
                IsEnabled = true,
                CustomMessage = "Pricing API rate limit exceeded."
            },

            // Authentication endpoints: 5 requests per minute (prevent brute force)
            new RateLimitRule
            {
                Id = "auth_endpoint",
                Type = RateLimitType.Endpoint,
                Limit = 5,
                WindowSizeInSeconds = 60,
                EndpointPattern = "/api/auth",
                HttpMethods = new[] { "POST" },
                Priority = 15,
                IsEnabled = true,
                CustomMessage = "Authentication rate limit exceeded. Please wait before trying again."
            },

            // Admin endpoints: 50 requests per hour
            new RateLimitRule
            {
                Id = "admin_endpoint",
                Type = RateLimitType.Endpoint,
                Limit = 50,
                WindowSizeInSeconds = 3600,
                EndpointPattern = "/api/admin",
                Priority = 12,
                IsEnabled = true,
                CustomMessage = "Admin API rate limit exceeded."
            }
        };
    }

    /// <summary>
    /// Validates security configuration
    /// </summary>
    public static void ValidateSecurityConfiguration(this IConfiguration configuration)
    {
        var requiredSettings = new[]
        {
            "ConnectionStrings:Redis",
            "RateLimiting:IsEnabled",
            "SecurityHeaders:EnableHsts"
        };

        var missingSettings = requiredSettings
            .Where(setting => string.IsNullOrEmpty(configuration[setting]))
            .ToList();

        if (missingSettings.Any())
        {
            throw new InvalidOperationException(
                $"Missing required security configuration settings: {string.Join(", ", missingSettings)}");
        }
    }
}

/// <summary>
/// Configuration-based rate limit rule provider
/// </summary>
public class ConfigurationRateLimitRuleProvider : IRateLimitRuleProvider
{
    private readonly RateLimitConfiguration _configuration;
    private readonly ILogger<ConfigurationRateLimitRuleProvider> _logger;

    public ConfigurationRateLimitRuleProvider(
        IOptions<RateLimitConfiguration> configuration,
        ILogger<ConfigurationRateLimitRuleProvider> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;

        // Ensure we have default rules if none configured
        if (!_configuration.Rules.Any())
        {
            _configuration.Rules = SecurityConfiguration.GetDefaultRateLimitRules();
            _logger.LogInformation("Using default rate limiting rules");
        }
    }

    public Task<IEnumerable<RateLimitRule>> GetApplicableRulesAsync(RateLimitContext context, CancellationToken cancellationToken = default)
    {
        var applicableRules = _configuration.Rules
            .Where(rule => IsRuleApplicable(rule, context))
            .OrderByDescending(rule => rule.Priority)
            .ToList();

        _logger.LogDebug("Found {RuleCount} applicable rules for {Endpoint}", 
            applicableRules.Count, context.Endpoint);

        return Task.FromResult<IEnumerable<RateLimitRule>>(applicableRules);
    }

    public Task<RateLimitRule?> GetRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var rule = _configuration.Rules.FirstOrDefault(r => r.Id == ruleId);
        return Task.FromResult(rule);
    }

    public Task<bool> UpsertRuleAsync(RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        // For configuration-based provider, rules are read-only
        _logger.LogWarning("Attempted to modify read-only configuration rule: {RuleId}", rule.Id);
        return Task.FromResult(false);
    }

    public Task<bool> RemoveRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        // For configuration-based provider, rules are read-only
        _logger.LogWarning("Attempted to remove read-only configuration rule: {RuleId}", ruleId);
        return Task.FromResult(false);
    }

    private bool IsRuleApplicable(RateLimitRule rule, RateLimitContext context)
    {
        if (!rule.IsEnabled)
            return false;

        return rule.Type switch
        {
            RateLimitType.IpAddress => !string.IsNullOrEmpty(context.IpAddress),
            RateLimitType.Client => !string.IsNullOrEmpty(context.ClientId),
            RateLimitType.Organization => !string.IsNullOrEmpty(context.OrganizationId),
            RateLimitType.Endpoint => IsEndpointMatch(rule, context),
            RateLimitType.Global => true,
            _ => false
        };
    }

    private bool IsEndpointMatch(RateLimitRule rule, RateLimitContext context)
    {
        if (string.IsNullOrEmpty(rule.EndpointPattern) || string.IsNullOrEmpty(context.Endpoint))
            return false;

        var endpointMatches = context.Endpoint.StartsWith(rule.EndpointPattern, StringComparison.OrdinalIgnoreCase);
        
        if (!endpointMatches)
            return false;

        // Check HTTP method if specified
        if (rule.HttpMethods?.Any() == true && !string.IsNullOrEmpty(context.HttpMethod))
        {
            return rule.HttpMethods.Contains(context.HttpMethod, StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }
}

/// <summary>
/// Action filter for input validation
/// </summary>
public class ValidationActionFilter : IActionFilter
{
    private readonly ILogger<ValidationActionFilter> _logger;

    public ValidationActionFilter(ILogger<ValidationActionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            _logger.LogWarning("Input validation failed for {Action}: {Errors}",
                context.ActionDescriptor.DisplayName,
                string.Join(", ", errors.SelectMany(e => e.Value)));

            context.Result = new BadRequestObjectResult(new
            {
                error = "Validation failed",
                details = errors
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed
    }
}
