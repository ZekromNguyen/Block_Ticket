namespace Identity.API.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        var headers = context.Response.Headers;

        // Prevent clickjacking
        headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        headers.Append("X-Content-Type-Options", "nosniff");

        // Enable XSS protection
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer policy
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content Security Policy
        var csp = "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self'; " +
                  "connect-src 'self'; " +
                  "frame-ancestors 'none';";
        headers.Append("Content-Security-Policy", csp);

        // Strict Transport Security (HTTPS only)
        if (context.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // Permissions Policy
        headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()");

        // Remove server information
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await _next(context);
    }
}

/// <summary>
/// Extension method to add security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
