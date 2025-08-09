using Identity.Domain.Entities;
using Identity.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Identity.API.Middleware;

/// <summary>
/// Advanced security middleware for threat detection and prevention
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public SecurityMiddleware(
        RequestDelegate next,
        IServiceProvider serviceProvider,
        IDistributedCache cache,
        ILogger<SecurityMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        try
        {
            // Check if IP is blocked
            if (await IsIpBlockedAsync(ipAddress))
            {
                await HandleBlockedIpAsync(context, ipAddress);
                return;
            }

            // Check rate limiting for sensitive endpoints
            if (IsSensitiveEndpoint(path))
            {
                if (await IsRateLimitExceededAsync(context, ipAddress))
                {
                    await HandleRateLimitExceededAsync(context, ipAddress);
                    return;
                }
            }

            // Detect suspicious patterns
            await DetectSuspiciousPatternsAsync(context, ipAddress, userAgent, path, method);

            // Continue to next middleware
            await _next(context);

            // Log successful requests to sensitive endpoints
            if (IsSensitiveEndpoint(path) && context.Response.StatusCode < 400)
            {
                await LogSecurityEventAsync(context, ipAddress, userAgent, "ENDPOINT_ACCESS", SecurityEventSeverity.Low);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security middleware for IP {IpAddress}", ipAddress);
            await _next(context);
        }
    }

    private async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        var cacheKey = $"blocked_ip:{ipAddress}";
        var blockedInfo = await _cache.GetStringAsync(cacheKey);
        return !string.IsNullOrEmpty(blockedInfo);
    }

    private async Task HandleBlockedIpAsync(HttpContext context, string ipAddress)
    {
        _logger.LogWarning("Blocked IP address attempted access: {IpAddress}", ipAddress);
        
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        await context.Response.WriteAsync("Access denied");

        // Log security event
        await LogSecurityEventAsync(context, ipAddress, null, "BLOCKED_IP_ACCESS", SecurityEventSeverity.High);
    }

    private async Task<bool> IsRateLimitExceededAsync(HttpContext context, string ipAddress)
    {
        using var scope = _serviceProvider.CreateScope();
        var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

        var userId = GetCurrentUserId(context);
        var key = userId?.ToString() ?? ipAddress;
        
        var limit = GetRateLimitForEndpoint(context.Request.Path.Value ?? "");
        var window = TimeSpan.FromMinutes(1);

        if (await securityService.IsRateLimitExceededAsync(key, limit, window))
        {
            return true;
        }

        await securityService.IncrementRateLimitCounterAsync(key, window);
        return false;
    }

    private async Task HandleRateLimitExceededAsync(HttpContext context, string ipAddress)
    {
        _logger.LogWarning("Rate limit exceeded for IP {IpAddress} on endpoint {Endpoint}", 
            ipAddress, context.Request.Path);

        context.Response.StatusCode = 429;
        context.Response.Headers["Retry-After"] = "60";
        await context.Response.WriteAsync("Rate limit exceeded");

        // Log security event
        await LogSecurityEventAsync(context, ipAddress, null, SecurityEventTypes.RateLimitExceeded, SecurityEventSeverity.Medium);

        // Consider temporary IP blocking for repeated violations
        await ConsiderIpBlockingAsync(ipAddress);
    }

    private async Task DetectSuspiciousPatternsAsync(HttpContext context, string ipAddress, string userAgent, string path, string method)
    {
        var suspiciousPatterns = new List<string>();

        // Check for suspicious user agents
        if (IsSuspiciousUserAgent(userAgent))
        {
            suspiciousPatterns.Add("Suspicious user agent detected");
        }

        // Check for SQL injection patterns
        if (ContainsSqlInjectionPatterns(context.Request.QueryString.Value))
        {
            suspiciousPatterns.Add("Potential SQL injection attempt");
        }

        // Check for XSS patterns
        if (ContainsXssPatterns(context.Request.QueryString.Value))
        {
            suspiciousPatterns.Add("Potential XSS attempt");
        }

        // Check for path traversal attempts
        if (ContainsPathTraversalPatterns(path))
        {
            suspiciousPatterns.Add("Potential path traversal attempt");
        }

        // Check for unusual request patterns
        if (await IsUnusualRequestPatternAsync(ipAddress, path, method))
        {
            suspiciousPatterns.Add("Unusual request pattern detected");
        }

        // Log suspicious activities
        if (suspiciousPatterns.Any())
        {
            await LogSuspiciousActivityAsync(context, ipAddress, userAgent, suspiciousPatterns);
        }
    }

    private async Task LogSecurityEventAsync(HttpContext context, string ipAddress, string? userAgent, string eventType, SecurityEventSeverity severity)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

            var userId = GetCurrentUserId(context);
            var location = await GetLocationFromIpAsync(ipAddress);

            var securityEvent = new SecurityEvent(
                userId,
                eventType,
                SecurityEventCategories.Security,
                severity,
                $"Security event: {eventType}",
                ipAddress,
                userAgent,
                location);

            await securityService.LogSecurityEventAsync(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event");
        }
    }

    private async Task LogSuspiciousActivityAsync(HttpContext context, string ipAddress, string userAgent, List<string> patterns)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

            var userId = GetCurrentUserId(context);
            var riskScore = await securityService.CalculateRiskScoreAsync(userId, ipAddress, userAgent, "SUSPICIOUS_REQUEST");

            var activity = new SuspiciousActivity(
                userId,
                "SUSPICIOUS_REQUEST",
                string.Join("; ", patterns),
                ipAddress,
                riskScore,
                userAgent);

            await securityService.LogSuspiciousActivityAsync(activity);

            _logger.LogWarning("Suspicious activity detected from IP {IpAddress}: {Patterns}", 
                ipAddress, string.Join(", ", patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging suspicious activity");
        }
    }

    private async Task ConsiderIpBlockingAsync(string ipAddress)
    {
        try
        {
            var cacheKey = $"rate_limit_violations:{ipAddress}";
            var violationsStr = await _cache.GetStringAsync(cacheKey);
            var violations = int.TryParse(violationsStr, out var count) ? count : 0;
            violations++;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, violations.ToString(), options);

            // Block IP after multiple violations
            var maxViolations = _configuration.GetValue<int>("Security:MaxRateLimitViolations", 5);
            if (violations >= maxViolations)
            {
                using var scope = _serviceProvider.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();
                
                await securityService.BlockIpAddressAsync(ipAddress, "Multiple rate limit violations", TimeSpan.FromHours(24));
                _logger.LogWarning("IP address blocked due to repeated violations: {IpAddress}", ipAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error considering IP blocking for {IpAddress}", ipAddress);
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP addresses
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static Guid? GetCurrentUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/auth/change-password",
            "/api/v1/mfa/",
            "/api/v1/oauth/",
            "/api/v1/roles/",
            "/connect/"
        };

        return sensitiveEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetRateLimitForEndpoint(string path)
    {
        return path.ToLower() switch
        {
            var p when p.Contains("/auth/login") => 5,
            var p when p.Contains("/auth/register") => 3,
            var p when p.Contains("/mfa/") => 10,
            var p when p.Contains("/oauth/") => 50,
            var p when p.Contains("/roles/") => 100,
            _ => 60
        };
    }

    private static bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return true;

        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python-requests",
            "java/", "go-http-client", "libwww-perl", "php", "ruby"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsSqlInjectionPatterns(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var sqlPatterns = new[]
        {
            "union select", "drop table", "insert into", "delete from",
            "update set", "exec(", "execute(", "sp_", "xp_", "' or '1'='1",
            "' or 1=1", "'; drop", "'; exec", "'; insert"
        };

        return sqlPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsXssPatterns(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var xssPatterns = new[]
        {
            "<script", "javascript:", "onload=", "onerror=", "onclick=",
            "onmouseover=", "alert(", "document.cookie", "window.location"
        };

        return xssPatterns.Any(pattern => 
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsPathTraversalPatterns(string path)
    {
        var traversalPatterns = new[] { "../", "..\\", "%2e%2e", "%252e%252e" };
        return traversalPatterns.Any(pattern => 
            path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> IsUnusualRequestPatternAsync(string ipAddress, string path, string method)
    {
        // This would implement more sophisticated pattern detection
        // For now, just check for rapid requests to different endpoints
        var cacheKey = $"request_pattern:{ipAddress}";
        var patternData = await _cache.GetStringAsync(cacheKey);
        
        if (patternData != null)
        {
            var pattern = JsonSerializer.Deserialize<RequestPattern>(patternData);
            if (pattern != null && pattern.IsUnusual(path, method))
            {
                return true;
            }
        }

        // Store current request pattern
        var newPattern = new RequestPattern { LastPath = path, LastMethod = method, Count = 1 };
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(newPattern), options);

        return false;
    }

    private async Task<string?> GetLocationFromIpAsync(string ipAddress)
    {
        // This would integrate with a geolocation service
        await Task.Delay(1); // Simulate async call
        return "Unknown"; // TODO: Implement actual geolocation
    }

    private class RequestPattern
    {
        public string LastPath { get; set; } = string.Empty;
        public string LastMethod { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastRequest { get; set; } = DateTime.UtcNow;

        public bool IsUnusual(string currentPath, string currentMethod)
        {
            // Simple pattern detection - could be much more sophisticated
            var timeDiff = DateTime.UtcNow - LastRequest;
            return timeDiff.TotalSeconds < 1 && (LastPath != currentPath || LastMethod != currentMethod);
        }
    }
}

/// <summary>
/// Extension method to add security middleware
/// </summary>
public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseAdvancedSecurity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMiddleware>();
    }
}
