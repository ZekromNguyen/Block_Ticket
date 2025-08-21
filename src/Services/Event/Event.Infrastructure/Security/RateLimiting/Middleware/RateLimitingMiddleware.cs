using Event.Application.Common.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Event.Infrastructure.Security.RateLimiting.Middleware;

/// <summary>
/// Middleware for applying rate limiting to HTTP requests
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitConfiguration _configuration;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitConfiguration> configuration,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiter rateLimiter, IOrganizationContextProvider? organizationContextProvider = null)
    {
        // Skip rate limiting for certain paths
        if (ShouldSkipRateLimit(context))
        {
            await _next(context);
            return;
        }

        try
        {
            // Build rate limiting context
            var rateLimitContext = BuildRateLimitContext(context, organizationContextProvider);

            // Check rate limits
            var result = await rateLimiter.CheckAsync(rateLimitContext);

            // Add rate limit headers if configured
            if (_configuration.IncludeHeaders)
            {
                AddRateLimitHeaders(context, result);
            }

            if (!result.IsAllowed)
            {
                await HandleRateLimitExceeded(context, result);
                return;
            }

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware");

            // Fail open if configured
            if (_configuration.FailOpen)
            {
                await _next(context);
            }
            else
            {
                await HandleRateLimitError(context, ex);
            }
        }
    }

    /// <summary>
    /// Determines if rate limiting should be skipped for this request
    /// </summary>
    private bool ShouldSkipRateLimit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        // Skip health checks and metrics endpoints
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico"
        };

        return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
    }

    /// <summary>
    /// Builds rate limiting context from HTTP context
    /// </summary>
    private RateLimitContext BuildRateLimitContext(HttpContext context, IOrganizationContextProvider? organizationContextProvider)
    {
        var rateLimitContext = new RateLimitContext
        {
            IpAddress = GetClientIpAddress(context),
            Endpoint = context.Request.Path.Value,
            HttpMethod = context.Request.Method,
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault()
        };

        // Add headers
        foreach (var header in context.Request.Headers)
        {
            rateLimitContext.Headers[header.Key] = header.Value.FirstOrDefault() ?? string.Empty;
        }

        // Get client ID from JWT claims
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            rateLimitContext.ClientId = context.User.FindFirst("sub")?.Value ??
                                       context.User.FindFirst("client_id")?.Value ??
                                       context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        // Get organization ID
        try
        {
            rateLimitContext.OrganizationId = organizationContextProvider?.GetCurrentOrganizationIdOrNull()?.ToString();
        }
        catch
        {
            // Ignore errors getting organization context
        }

        // Add API key if present
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            rateLimitContext.ClientId ??= apiKey.FirstOrDefault();
        }

        return rateLimitContext;
    }

    /// <summary>
    /// Gets the client IP address from the request
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
            {
                return firstIp;
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Adds rate limit headers to the response
    /// </summary>
    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        if (result.AppliedRule == null) return;

        var headers = _configuration.Headers;
        var remaining = Math.Max(0, result.Limit - result.CurrentCount);

        context.Response.Headers[headers.Limit] = result.Limit.ToString();
        context.Response.Headers[headers.Remaining] = remaining.ToString();
        context.Response.Headers[headers.Reset] = DateTimeOffset.UtcNow.AddSeconds(result.ResetTimeInSeconds).ToUnixTimeSeconds().ToString();

        if (result.RetryAfterSeconds.HasValue)
        {
            context.Response.Headers[headers.RetryAfter] = result.RetryAfterSeconds.Value.ToString();
        }
    }

    /// <summary>
    /// Handles rate limit exceeded scenario
    /// </summary>
    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Rate limit exceeded",
            message = result.AppliedRule?.CustomMessage ?? "Too many requests. Please try again later.",
            details = new
            {
                limit = result.Limit,
                current = result.CurrentCount,
                resetTime = DateTimeOffset.UtcNow.AddSeconds(result.ResetTimeInSeconds),
                retryAfter = result.RetryAfterSeconds
            }
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogWarning("Rate limit exceeded: {ClientIp} - {Endpoint} - {Rule}",
            GetClientIpAddress(context),
            context.Request.Path,
            result.AppliedRule?.Id);

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Handles rate limiting errors
    /// </summary>
    private async Task HandleRateLimitError(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = 503; // Service Unavailable
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Rate limiting service unavailable",
            message = "Unable to process rate limiting. Please try again later."
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogError(ex, "Rate limiting service error");

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extensions for registering rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds rate limiting middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
