using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Event.Infrastructure.Security.Middleware;

/// <summary>
/// Middleware to enforce request size limits for security
/// </summary>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestSizeLimitOptions _options;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        IOptions<RequestSizeLimitOptions> options,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? new RequestSizeLimitOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Check content length
            if (context.Request.ContentLength.HasValue)
            {
                var limit = GetSizeLimitForRequest(context);
                
                if (context.Request.ContentLength.Value > limit)
                {
                    await HandleRequestTooLarge(context, context.Request.ContentLength.Value, limit);
                    return;
                }
            }

            // For requests without content-length, we need to check during reading
            if (ShouldCheckRequestSize(context))
            {
                context.Request.Body = new SizeLimitedStream(
                    context.Request.Body, 
                    GetSizeLimitForRequest(context),
                    () => HandleStreamSizeExceeded(context));
            }

            await _next(context);
        }
        catch (RequestTooLargeException ex)
        {
            await HandleRequestTooLarge(context, ex.ActualSize, ex.MaxSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in request size limit middleware");
            throw;
        }
    }

    private bool ShouldCheckRequestSize(HttpContext context)
    {
        // Only check for requests with body content
        return context.Request.ContentLength > 0 || 
               (context.Request.Method != "GET" && context.Request.Method != "HEAD" && context.Request.Method != "DELETE");
    }

    private long GetSizeLimitForRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        var contentType = context.Request.ContentType?.ToLowerInvariant();

        // Check for file upload endpoints
        if (IsFileUploadEndpoint(path, contentType))
        {
            return _options.MaxFileUploadSize;
        }

        // Check for specific endpoint limits
        foreach (var endpointLimit in _options.EndpointLimits)
        {
            if (path?.StartsWith(endpointLimit.Key, StringComparison.OrdinalIgnoreCase) == true)
            {
                return endpointLimit.Value;
            }
        }

        return _options.MaxRequestSize;
    }

    private bool IsFileUploadEndpoint(string? path, string? contentType)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Check for multipart content type
        if (contentType?.Contains("multipart/form-data") == true)
            return true;

        // Check for known file upload endpoints
        var fileUploadPatterns = new[]
        {
            "/api/files",
            "/api/upload",
            "/api/images",
            "/api/documents"
        };

        return fileUploadPatterns.Any(pattern => 
            path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private async Task HandleRequestTooLarge(HttpContext context, long actualSize, long maxSize)
    {
        context.Response.StatusCode = 413; // Payload Too Large
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Request too large",
            message = $"Request size ({actualSize:N0} bytes) exceeds the maximum allowed size ({maxSize:N0} bytes)",
            maxSize = maxSize,
            actualSize = actualSize
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogWarning("Request too large: {ActualSize} bytes exceeds limit of {MaxSize} bytes for {Path}",
            actualSize, maxSize, context.Request.Path);

        await context.Response.WriteAsync(json);
    }

    private void HandleStreamSizeExceeded(HttpContext context)
    {
        throw new RequestTooLargeException(GetSizeLimitForRequest(context), GetSizeLimitForRequest(context));
    }
}

/// <summary>
/// Stream wrapper that enforces size limits
/// </summary>
public class SizeLimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxSize;
    private readonly Action _onSizeExceeded;
    private long _totalBytesRead;

    public SizeLimitedStream(Stream innerStream, long maxSize, Action onSizeExceeded)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _maxSize = maxSize;
        _onSizeExceeded = onSizeExceeded ?? throw new ArgumentNullException(nameof(onSizeExceeded));
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position 
    { 
        get => _innerStream.Position; 
        set => _innerStream.Position = value; 
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _totalBytesRead += bytesRead;

        if (_totalBytesRead > _maxSize)
        {
            _onSizeExceeded();
        }

        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _totalBytesRead += bytesRead;

        if (_totalBytesRead > _maxSize)
        {
            _onSizeExceeded();
        }

        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => 
        _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Exception thrown when request size exceeds limits
/// </summary>
public class RequestTooLargeException : Exception
{
    public long MaxSize { get; }
    public long ActualSize { get; }

    public RequestTooLargeException(long maxSize, long actualSize) 
        : base($"Request size ({actualSize}) exceeds maximum allowed size ({maxSize})")
    {
        MaxSize = maxSize;
        ActualSize = actualSize;
    }
}

/// <summary>
/// Configuration options for request size limits
/// </summary>
public class RequestSizeLimitOptions
{
    /// <summary>
    /// Maximum request size in bytes (default: 1MB)
    /// </summary>
    public long MaxRequestSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Maximum file upload size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileUploadSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Endpoint-specific size limits
    /// </summary>
    public Dictionary<string, long> EndpointLimits { get; set; } = new()
    {
        { "/api/events", 512 * 1024 }, // 512KB for event creation
        { "/api/reservations", 64 * 1024 }, // 64KB for reservations
        { "/api/auth", 16 * 1024 } // 16KB for authentication
    };
}

/// <summary>
/// Extensions for registering request size limit middleware
/// </summary>
public static class RequestSizeLimitMiddlewareExtensions
{
    /// <summary>
    /// Adds request size limit middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRequestSizeLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestSizeLimitMiddleware>();
    }

    /// <summary>
    /// Adds request size limit middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseRequestSizeLimit(this IApplicationBuilder builder, Action<RequestSizeLimitOptions> configureOptions)
    {
        var options = new RequestSizeLimitOptions();
        configureOptions(options);

        return builder.UseMiddleware<RequestSizeLimitMiddleware>(Options.Create(options));
    }
}
