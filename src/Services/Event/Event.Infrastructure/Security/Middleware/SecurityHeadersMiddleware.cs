using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Event.Infrastructure.Security.Middleware;

/// <summary>
/// Middleware to add security headers for OWASP ASVS compliance
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? new SecurityHeadersOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await _next(context);

        // Add additional headers after processing if needed
        AddPostProcessingHeaders(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        try
        {
            // Strict Transport Security (HSTS)
            if (_options.EnableHsts && context.Request.IsHttps)
            {
                response.Headers["Strict-Transport-Security"] = _options.HstsValue;
            }

            // Content Security Policy
            if (_options.EnableCsp && !string.IsNullOrEmpty(_options.CspValue))
            {
                response.Headers["Content-Security-Policy"] = _options.CspValue;
            }

            // X-Frame-Options
            if (_options.EnableXFrameOptions)
            {
                response.Headers["X-Frame-Options"] = _options.XFrameOptionsValue;
            }

            // X-Content-Type-Options
            if (_options.EnableXContentTypeOptions)
            {
                response.Headers["X-Content-Type-Options"] = "nosniff";
            }

            // X-XSS-Protection
            if (_options.EnableXXssProtection)
            {
                response.Headers["X-XSS-Protection"] = _options.XXssProtectionValue;
            }

            // Referrer Policy
            if (_options.EnableReferrerPolicy)
            {
                response.Headers["Referrer-Policy"] = _options.ReferrerPolicyValue;
            }

            // Permissions Policy
            if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicyValue))
            {
                response.Headers["Permissions-Policy"] = _options.PermissionsPolicyValue;
            }

            // Remove server information
            if (_options.RemoveServerHeader)
            {
                response.Headers.Remove("Server");
            }

            // Add custom security headers
            foreach (var header in _options.CustomHeaders)
            {
                response.Headers[header.Key] = header.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error adding security headers");
        }
    }

    private void AddPostProcessingHeaders(HttpContext context)
    {
        var response = context.Response;

        try
        {
            // Cache control for sensitive endpoints
            if (IsSensitiveEndpoint(context.Request.Path))
            {
                response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                response.Headers["Pragma"] = "no-cache";
                response.Headers["Expires"] = "0";
            }

            // Add correlation ID header if available
            if (context.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                response.Headers["X-Correlation-ID"] = correlationId?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error adding post-processing headers");
        }
    }

    private bool IsSensitiveEndpoint(PathString path)
    {
        var sensitivePatterns = new[]
        {
            "/api/auth",
            "/api/users",
            "/api/admin",
            "/api/reservations",
            "/api/payments"
        };

        return sensitivePatterns.Any(pattern => 
            path.Value?.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) == true);
    }
}

/// <summary>
/// Configuration options for security headers
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Enable HTTP Strict Transport Security
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// HSTS header value
    /// </summary>
    public string HstsValue { get; set; } = "max-age=31536000; includeSubDomains; preload";

    /// <summary>
    /// Enable Content Security Policy
    /// </summary>
    public bool EnableCsp { get; set; } = true;

    /// <summary>
    /// CSP header value
    /// </summary>
    public string CspValue { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";

    /// <summary>
    /// Enable X-Frame-Options
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    /// X-Frame-Options header value
    /// </summary>
    public string XFrameOptionsValue { get; set; } = "DENY";

    /// <summary>
    /// Enable X-Content-Type-Options
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    /// Enable X-XSS-Protection
    /// </summary>
    public bool EnableXXssProtection { get; set; } = true;

    /// <summary>
    /// X-XSS-Protection header value
    /// </summary>
    public string XXssProtectionValue { get; set; } = "1; mode=block";

    /// <summary>
    /// Enable Referrer Policy
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Referrer Policy header value
    /// </summary>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Enable Permissions Policy
    /// </summary>
    public bool EnablePermissionsPolicy { get; set; } = true;

    /// <summary>
    /// Permissions Policy header value
    /// </summary>
    public string PermissionsPolicyValue { get; set; } = "camera=(), microphone=(), geolocation=(), payment=()";

    /// <summary>
    /// Remove Server header
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Custom security headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Extensions for registering security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Adds security headers middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder, Action<SecurityHeadersOptions> configureOptions)
    {
        var options = new SecurityHeadersOptions();
        configureOptions(options);

        return builder.UseMiddleware<SecurityHeadersMiddleware>(Options.Create(options));
    }
}
