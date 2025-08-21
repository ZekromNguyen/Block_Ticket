using Event.Domain.Models;
using Event.Application.Interfaces.Infrastructure;
using System.Diagnostics;
using System.Text.Json;

namespace Event.API.Middleware;

/// <summary>
/// Middleware to monitor and track performance metrics for all HTTP requests
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly PerformanceMonitoringOptions _options;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = configuration.GetSection("PerformanceMonitoring").Get<PerformanceMonitoringOptions>() 
                   ?? new PerformanceMonitoringOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip monitoring for certain paths
        if (ShouldSkipMonitoring(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var startTimestamp = DateTime.UtcNow;
        var initialMemory = GC.GetTotalMemory(false);
        
        // Get correlation ID or generate one
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? context.TraceIdentifier;

        Exception? capturedException = null;
        var responseStatusCode = 200;

        try
        {
            await _next(context);
            responseStatusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            responseStatusCode = 500;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Get the performance monitoring service from the current scope
            var performanceMonitoringService = context.RequestServices.GetRequiredService<IPerformanceMonitoringService>();
            
            // Record the performance metric asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await RecordPerformanceMetricAsync(
                        context, 
                        stopwatch.ElapsedMilliseconds, 
                        startTimestamp,
                        initialMemory,
                        responseStatusCode,
                        correlationId,
                        capturedException,
                        performanceMonitoringService);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record performance metric for {Path}", context.Request.Path);
                }
            });
        }
    }

    private async Task RecordPerformanceMetricAsync(
        HttpContext context,
        long durationMs,
        DateTime startTimestamp,
        long initialMemory,
        int statusCode,
        string correlationId,
        Exception? exception,
        IPerformanceMonitoringService performanceMonitoringService)
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryUsed = currentMemory - initialMemory;

        // Get additional context from headers or claims
        var userId = GetUserId(context);
        var organizationId = GetOrganizationId(context);

        // Check for database metrics if available
        var dbQueryCount = GetDatabaseQueryCount(context);
        var dbDuration = GetDatabaseDuration(context);

        // Check for cache hit information
        var cacheHit = GetCacheHitStatus(context);

        var metric = new PerformanceMetrics
        {
            Id = Guid.NewGuid(),
            Timestamp = startTimestamp,
            ServiceName = _options.ServiceName,
            OperationName = GetOperationName(context),
            HttpMethod = context.Request.Method,
            RequestPath = GetNormalizedPath(context.Request.Path),
            StatusCode = statusCode,
            DurationMs = durationMs,
            MemoryUsageBytes = memoryUsed > 0 ? memoryUsed : null,
            DatabaseQueryCount = dbQueryCount,
            DatabaseDurationMs = dbDuration,
            CacheHit = cacheHit,
            ErrorMessage = exception?.Message,
            ExceptionType = exception?.GetType().Name,
            UserId = userId,
            OrganizationId = organizationId,
            CorrelationId = correlationId,
            Metadata = CreateMetadata(context, exception)
        };

        // Only record if enabled and within sampling rate
        if (_options.Enabled && ShouldSample())
        {
            await performanceMonitoringService.RecordMetricAsync(metric);
        }

        // Log critical performance issues immediately
        if (durationMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms (Threshold: {Threshold}ms)",
                context.Request.Method, context.Request.Path, durationMs, _options.SlowRequestThresholdMs);
        }

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Server error for {Method} {Path}: {StatusCode}",
                context.Request.Method, context.Request.Path, statusCode);
        }
    }

    private bool ShouldSkipMonitoring(string path)
    {
        return _options.ExcludedPaths.Any(excludedPath => 
            path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldSample()
    {
        return Random.Shared.NextDouble() < _options.SamplingRate;
    }

    private string GetOperationName(HttpContext context)
    {
        // Try to get the action name from the route
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var actionDescriptor = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();
            if (actionDescriptor != null)
            {
                return $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
            }
        }

        // Fallback to HTTP method and path
        return $"{context.Request.Method} {GetNormalizedPath(context.Request.Path)}";
    }

    private string GetNormalizedPath(string path)
    {
        // Replace dynamic segments with placeholders
        // e.g., /api/v1/events/123/tickets -> /api/v1/events/{id}/tickets
        
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalizedSegments = new List<string>();

        foreach (var segment in segments)
        {
            // Check if segment looks like a GUID or ID
            if (Guid.TryParse(segment, out _) || 
                (int.TryParse(segment, out _) && segment.Length > 2))
            {
                normalizedSegments.Add("{id}");
            }
            else
            {
                normalizedSegments.Add(segment);
            }
        }

        return "/" + string.Join("/", normalizedSegments);
    }

    private string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value ??
               context.User?.FindFirst("user_id")?.Value ??
               context.Request.Headers["X-User-ID"].FirstOrDefault();
    }

    private Guid? GetOrganizationId(HttpContext context)
    {
        var orgIdString = context.User?.FindFirst("org_id")?.Value ??
                         context.Request.Headers["X-Organization-ID"].FirstOrDefault();

        return Guid.TryParse(orgIdString, out var orgId) ? orgId : null;
    }

    private int? GetDatabaseQueryCount(HttpContext context)
    {
        // This would be set by Entity Framework interceptors
        return context.Items.TryGetValue("DatabaseQueryCount", out var count) 
            ? count as int? : null;
    }

    private double? GetDatabaseDuration(HttpContext context)
    {
        // This would be set by Entity Framework interceptors
        return context.Items.TryGetValue("DatabaseDuration", out var duration) 
            ? duration as double? : null;
    }

    private bool? GetCacheHitStatus(HttpContext context)
    {
        // This would be set by cache services
        return context.Items.TryGetValue("CacheHit", out var hit) 
            ? hit as bool? : null;
    }

    private string? CreateMetadata(HttpContext context, Exception? exception)
    {
        var metadata = new Dictionary<string, object?>();

        // Add request headers (filtered)
        var allowedHeaders = new[] { "User-Agent", "Accept", "Content-Type", "X-Forwarded-For" };
        foreach (var header in allowedHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var value))
            {
                metadata[header] = value.ToString();
            }
        }

        // Add query string count
        metadata["QueryParameterCount"] = context.Request.Query.Count;

        // Add request body size if available
        if (context.Request.ContentLength.HasValue)
        {
            metadata["RequestBodySize"] = context.Request.ContentLength.Value;
        }

        // Add response body size if available
        if (context.Response.ContentLength.HasValue)
        {
            metadata["ResponseBodySize"] = context.Response.ContentLength.Value;
        }

        // Add exception details if present
        if (exception != null)
        {
            metadata["ExceptionStackTrace"] = exception.StackTrace?.Substring(0, Math.Min(1000, exception.StackTrace.Length));
            metadata["InnerExceptionType"] = exception.InnerException?.GetType().Name;
        }

        return metadata.Any() ? JsonSerializer.Serialize(metadata) : null;
    }
}

/// <summary>
/// Configuration options for performance monitoring
/// </summary>
public class PerformanceMonitoringOptions
{
    /// <summary>
    /// Whether performance monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Service name for metrics
    /// </summary>
    public string ServiceName { get; set; } = "Event.API";

    /// <summary>
    /// Sampling rate (0.0 to 1.0)
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Threshold for slow request logging (ms)
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Paths to exclude from monitoring
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico"
    };

    /// <summary>
    /// Maximum metadata size in characters
    /// </summary>
    public int MaxMetadataSize { get; set; } = 2000;

    /// <summary>
    /// Whether to track memory usage
    /// </summary>
    public bool TrackMemoryUsage { get; set; } = true;

    /// <summary>
    /// Whether to track database metrics
    /// </summary>
    public bool TrackDatabaseMetrics { get; set; } = true;

    /// <summary>
    /// Whether to track cache metrics
    /// </summary>
    public bool TrackCacheMetrics { get; set; } = true;
}

/// <summary>
/// Extension methods for registering performance monitoring
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Add performance monitoring middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceMonitoringMiddleware>();
    }

    /// <summary>
    /// Configure performance monitoring services
    /// </summary>
    public static IServiceCollection AddPerformanceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PerformanceMonitoringOptions>(
            configuration.GetSection("PerformanceMonitoring"));

        return services;
    }
}
