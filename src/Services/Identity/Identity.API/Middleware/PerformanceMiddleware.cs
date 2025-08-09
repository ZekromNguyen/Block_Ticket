using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace Identity.API.Middleware;

/// <summary>
/// Performance monitoring middleware for API Gateway integration
/// </summary>
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public PerformanceMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<PerformanceMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;
        var endpoint = $"{method} {path}";

        try
        {
            // Add correlation ID for tracing
            if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
            {
                var correlationId = Guid.NewGuid().ToString();
                context.Request.Headers["X-Correlation-ID"] = correlationId;
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }

            // Add request timestamp
            context.Response.Headers["X-Request-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            // Add performance headers only if response hasn't started
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Response-Time"] = $"{responseTime}ms";
                context.Response.Headers["X-Server-Name"] = Environment.MachineName;
            }

            // Log performance metrics
            await LogPerformanceMetricsAsync(endpoint, responseTime, context.Response.StatusCode);

            // Log slow requests
            var slowRequestThreshold = _configuration.GetValue<int>("Performance:SlowRequestThresholdMs", 1000);
            if (responseTime > slowRequestThreshold)
            {
                _logger.LogWarning("Slow request detected: {Endpoint} took {ResponseTime}ms", endpoint, responseTime);
            }

            // Update performance counters
            await UpdatePerformanceCountersAsync(endpoint, responseTime, context.Response.StatusCode);
        }
    }

    private async Task LogPerformanceMetricsAsync(string endpoint, long responseTime, int statusCode)
    {
        try
        {
            var metric = new PerformanceMetric
            {
                Endpoint = endpoint,
                ResponseTime = responseTime,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };

            // Store in cache for metrics collection
            var cacheKey = $"perf_metric:{Guid.NewGuid()}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(metric), options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging performance metrics");
        }
    }

    private async Task UpdatePerformanceCountersAsync(string endpoint, long responseTime, int statusCode)
    {
        try
        {
            // Update endpoint counter
            var endpointCounterKey = $"counter:endpoint:{endpoint}";
            var currentCountStr = await _cache.GetStringAsync(endpointCounterKey);
            var currentCount = long.TryParse(currentCountStr, out var count) ? count : 0;
            currentCount++;

            var counterOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(endpointCounterKey, currentCount.ToString(), counterOptions);

            // Update response time average
            var avgResponseTimeKey = $"avg_response_time:{endpoint}";
            var avgData = await _cache.GetStringAsync(avgResponseTimeKey);
            
            AverageResponseTime avgResponseTime;
            if (avgData != null)
            {
                avgResponseTime = JsonSerializer.Deserialize<AverageResponseTime>(avgData) ?? new AverageResponseTime();
            }
            else
            {
                avgResponseTime = new AverageResponseTime();
            }

            avgResponseTime.AddSample(responseTime);

            await _cache.SetStringAsync(avgResponseTimeKey, JsonSerializer.Serialize(avgResponseTime), counterOptions);

            // Update status code counters
            var statusCounterKey = $"counter:status:{statusCode}";
            var statusCountStr = await _cache.GetStringAsync(statusCounterKey);
            var statusCount = long.TryParse(statusCountStr, out var sCount) ? sCount : 0;
            statusCount++;

            await _cache.SetStringAsync(statusCounterKey, statusCount.ToString(), counterOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance counters");
        }
    }

    private class PerformanceMetric
    {
        public string Endpoint { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class AverageResponseTime
    {
        public double Average { get; set; }
        public long SampleCount { get; set; }
        public long TotalTime { get; set; }

        public void AddSample(long responseTime)
        {
            TotalTime += responseTime;
            SampleCount++;
            Average = (double)TotalTime / SampleCount;
        }
    }
}

/// <summary>
/// Extension method to add performance middleware
/// </summary>
public static class PerformanceMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceMiddleware>();
    }
}

/// <summary>
/// Performance metrics service for collecting and reporting metrics
/// </summary>
public interface IPerformanceMetricsService
{
    Task<Dictionary<string, object>> GetMetricsAsync();
    Task<Dictionary<string, long>> GetEndpointCountersAsync();
    Task<Dictionary<string, double>> GetAverageResponseTimesAsync();
    Task ResetMetricsAsync();
}

public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PerformanceMetricsService> _logger;

    public PerformanceMetricsService(IDistributedCache cache, ILogger<PerformanceMetricsService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> GetMetricsAsync()
    {
        try
        {
            var metrics = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "endpoint_counters", await GetEndpointCountersAsync() },
                { "average_response_times", await GetAverageResponseTimesAsync() },
                { "status_code_distribution", await GetStatusCodeDistributionAsync() }
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return new Dictionary<string, object>();
        }
    }

    public async Task<Dictionary<string, long>> GetEndpointCountersAsync()
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you'd iterate through cache keys or use a dedicated metrics store
            var counters = new Dictionary<string, long>();

            var commonEndpoints = new[]
            {
                "POST /api/v1/gateway/validate-token",
                "GET /api/v1/gateway/user/{userId}",
                "POST /api/v1/gateway/check-permission",
                "POST /api/v1/auth/login",
                "POST /api/v1/auth/register"
            };

            foreach (var endpoint in commonEndpoints)
            {
                var key = $"counter:endpoint:{endpoint}";
                var countStr = await _cache.GetStringAsync(key);
                if (long.TryParse(countStr, out var count))
                {
                    counters[endpoint] = count;
                }
            }

            return counters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting endpoint counters");
            return new Dictionary<string, long>();
        }
    }

    public async Task<Dictionary<string, double>> GetAverageResponseTimesAsync()
    {
        try
        {
            var averages = new Dictionary<string, double>();

            var commonEndpoints = new[]
            {
                "POST /api/v1/gateway/validate-token",
                "GET /api/v1/gateway/user/{userId}",
                "POST /api/v1/gateway/check-permission",
                "POST /api/v1/auth/login",
                "POST /api/v1/auth/register"
            };

            foreach (var endpoint in commonEndpoints)
            {
                var key = $"avg_response_time:{endpoint}";
                var avgDataStr = await _cache.GetStringAsync(key);
                if (avgDataStr != null)
                {
                    var avgData = JsonSerializer.Deserialize<AverageResponseTime>(avgDataStr);
                    if (avgData != null)
                    {
                        averages[endpoint] = avgData.Average;
                    }
                }
            }

            return averages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting average response times");
            return new Dictionary<string, double>();
        }
    }

    public async Task ResetMetricsAsync()
    {
        try
        {
            // This would reset all performance metrics
            // Implementation depends on cache capabilities
            _logger.LogInformation("Performance metrics reset requested");
            await Task.Delay(1); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting performance metrics");
        }
    }

    private async Task<Dictionary<string, long>> GetStatusCodeDistributionAsync()
    {
        try
        {
            var distribution = new Dictionary<string, long>();

            var statusCodes = new[] { 200, 201, 400, 401, 403, 404, 500 };

            foreach (var statusCode in statusCodes)
            {
                var key = $"counter:status:{statusCode}";
                var countStr = await _cache.GetStringAsync(key);
                if (long.TryParse(countStr, out var count))
                {
                    distribution[statusCode.ToString()] = count;
                }
            }

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status code distribution");
            return new Dictionary<string, long>();
        }
    }

    private class AverageResponseTime
    {
        public double Average { get; set; }
        public long SampleCount { get; set; }
        public long TotalTime { get; set; }
    }
}
