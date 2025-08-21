using Event.Domain.Configuration;
using Event.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Event.API.Middleware;

/// <summary>
/// Advanced rate limiting middleware with multi-layer protection
/// </summary>
public class AdvancedRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitConfiguration _config;
    private readonly ILogger<AdvancedRateLimitMiddleware> _logger;

    public AdvancedRateLimitMiddleware(
        RequestDelegate next,
        IOptions<RateLimitConfiguration> config,
        ILogger<AdvancedRateLimitMiddleware> logger)
    {
        _next = next;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting if disabled
        if (!_config.Enabled)
        {
            await _next(context);
            return;
        }

        // Get the rate limit service from the current scope
        var rateLimitService = context.RequestServices.GetRequiredService<IRateLimitService>();

        // Extract client information
        var clientInfo = ExtractClientInfo(context);

        // Check if client is whitelisted
        if (await rateLimitService.IsWhitelistedAsync(clientInfo.ClientId, clientInfo.IpAddress))
        {
            await _next(context);
            return;
        }

        try
        {
            // Check rate limits
            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(
                clientInfo.ClientId,
                clientInfo.IpAddress,
                clientInfo.Endpoint,
                clientInfo.Method,
                clientInfo.OrganizationId,
                context.RequestAborted);

            // Add rate limit headers
            AddRateLimitHeaders(context, rateLimitResult);

            if (rateLimitResult.IsBlocked)
            {
                // Log rate limit violation
                LogRateLimitViolation(clientInfo, rateLimitResult);

                // Record the blocked request
                await rateLimitService.RecordRequestAsync(
                    clientInfo.ClientId,
                    clientInfo.IpAddress,
                    clientInfo.Endpoint,
                    clientInfo.Method,
                    wasBlocked: true,
                    clientInfo.OrganizationId,
                    context.RequestAborted);

                // Return rate limit exceeded response
                await HandleRateLimitExceeded(context, rateLimitResult);
                return;
            }

            // Record the allowed request
            await rateLimitService.RecordRequestAsync(
                clientInfo.ClientId,
                clientInfo.IpAddress,
                clientInfo.Endpoint,
                clientInfo.Method,
                wasBlocked: false,
                clientInfo.OrganizationId,
                context.RequestAborted);

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for client {ClientId}", clientInfo.ClientId);
            
            // Don't block requests due to rate limiting errors
            await _next(context);
        }
    }

    private ClientInfo ExtractClientInfo(HttpContext context)
    {
        var request = context.Request;
        
        // Extract IP address
        var ipAddress = GetClientIpAddress(context);
        
        // Extract client ID from various sources
        var clientId = GetClientId(context);
        
        // Extract organization ID from claims or headers
        var organizationId = context.User?.FindFirst("org_id")?.Value ??
                           request.Headers["X-Organization-Id"].FirstOrDefault();

        // Build endpoint identifier
        var endpoint = $"{request.Method}:{request.Path.Value?.ToLowerInvariant()}";
        
        return new ClientInfo
        {
            IpAddress = ipAddress,
            ClientId = clientId,
            OrganizationId = organizationId,
            Endpoint = endpoint,
            Method = request.Method,
            UserAgent = request.Headers.UserAgent.ToString(),
            Referer = request.Headers.Referer.ToString()
        };
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var request = context.Request;
        
        // Check various headers for real IP (in order of preference)
        var ipHeaders = new[]
        {
            _config.RealIPHeader,
            "X-Forwarded-For",
            "X-Real-IP",
            "CF-Connecting-IP", // Cloudflare
            "X-Forwarded",
            "X-Cluster-Client-IP"
        };

        foreach (var header in ipHeaders)
        {
            if (request.Headers.TryGetValue(header, out var headerValue))
            {
                var ip = headerValue.FirstOrDefault();
                if (!string.IsNullOrEmpty(ip))
                {
                    // Handle comma-separated IPs (take the first one)
                    var firstIp = ip.Split(',')[0].Trim();
                    if (IPAddress.TryParse(firstIp, out _))
                    {
                        return firstIp;
                    }
                }
            }
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetClientId(HttpContext context)
    {
        var request = context.Request;
        
        // Try to get client ID from various sources
        var clientId = request.Headers[_config.ClientIdHeader].FirstOrDefault() ??
                      request.Headers["X-Client-Id"].FirstOrDefault() ??
                      request.Headers["X-API-Key"].FirstOrDefault() ??
                      context.User?.FindFirst("client_id")?.Value ??
                      context.User?.FindFirst("sub")?.Value;

        // If no explicit client ID, generate one based on IP + User-Agent
        if (string.IsNullOrEmpty(clientId))
        {
            var userAgent = request.Headers.UserAgent.ToString();
            var ipAddress = GetClientIpAddress(context);
            clientId = $"ip:{ipAddress}:ua:{userAgent.GetHashCode():X}";
        }

        return clientId;
    }

    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        var response = context.Response;
        
        // Standard rate limit headers
        response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = Math.Max(0, result.Limit - result.CurrentCount).ToString();
        response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)result.ResetTime).ToUnixTimeSeconds().ToString();
        response.Headers["X-RateLimit-Period"] = result.Period.TotalSeconds.ToString();

        if (result.IsBlocked)
        {
            response.Headers["Retry-After"] = result.RetryAfter.TotalSeconds.ToString("F0");
            response.Headers["X-RateLimit-Blocked"] = "true";
            response.Headers["X-RateLimit-Reason"] = result.Reason;
        }
    }

    private void LogRateLimitViolation(ClientInfo clientInfo, RateLimitResult result)
    {
        _logger.LogWarning(
            "Rate limit exceeded for client {ClientId} from IP {IpAddress} on endpoint {Endpoint}. " +
            "Current: {CurrentCount}, Limit: {Limit}, Period: {Period}, Reason: {Reason}",
            clientInfo.ClientId,
            clientInfo.IpAddress,
            clientInfo.Endpoint,
            result.CurrentCount,
            result.Limit,
            result.Period,
            result.Reason);

        // Log additional context for security monitoring
        _logger.LogInformation(
            "Rate limit violation details - UserAgent: {UserAgent}, Referer: {Referer}, Organization: {OrganizationId}",
            clientInfo.UserAgent,
            clientInfo.Referer,
            clientInfo.OrganizationId);
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        var response = context.Response;
        
        response.StatusCode = _config.HttpStatusCode;
        response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "rate_limit_exceeded",
            message = string.Format(_config.QuotaExceededMessage, result.Limit, result.Period),
            details = new
            {
                endpoint = result.Endpoint,
                current_requests = result.CurrentCount,
                max_requests = result.Limit,
                period_seconds = (int)result.Period.TotalSeconds,
                reset_time = result.ResetTime,
                retry_after_seconds = (int)result.RetryAfter.TotalSeconds,
                reason = result.Reason
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        });

        await response.WriteAsync(json, context.RequestAborted);
    }
}

/// <summary>
/// Client information extracted from request
/// </summary>
internal class ClientInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Referer { get; set; } = string.Empty;
}
