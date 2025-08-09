using Identity.API.Middleware;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers.V1;

/// <summary>
/// Metrics and monitoring endpoints for API Gateway integration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/metrics")]
[Authorize(Roles = "admin,super_admin,api_gateway")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IGatewayService _gatewayService;
    private readonly IPerformanceMetricsService _performanceMetricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IGatewayService gatewayService,
        IPerformanceMetricsService performanceMetricsService,
        ILogger<MetricsController> logger)
    {
        _gatewayService = gatewayService;
        _performanceMetricsService = performanceMetricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive system metrics
    /// </summary>
    /// <returns>System metrics including performance, security, and usage data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var gatewayMetrics = await _gatewayService.GetMetricsAsync();
            var performanceMetrics = await _performanceMetricsService.GetMetricsAsync();
            var healthInfo = await _gatewayService.GetHealthInfoAsync();

            var response = new SystemMetricsResponse
            {
                Timestamp = DateTime.UtcNow,
                Health = healthInfo.IsSuccess ? healthInfo.Value! : null,
                Gateway = gatewayMetrics.IsSuccess ? gatewayMetrics.Value! : null,
                Performance = performanceMetrics,
                System = GetSystemMetrics()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Metrics Error",
                Detail = "An error occurred while retrieving system metrics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    /// <returns>Performance metrics including response times and throughput</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            var metrics = await _performanceMetricsService.GetMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Performance Metrics Error",
                Detail = "An error occurred while retrieving performance metrics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get endpoint usage statistics
    /// </summary>
    /// <returns>Endpoint usage counters and statistics</returns>
    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(EndpointMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEndpointMetrics()
    {
        try
        {
            var counters = await _performanceMetricsService.GetEndpointCountersAsync();
            var averageResponseTimes = await _performanceMetricsService.GetAverageResponseTimesAsync();

            var response = new EndpointMetricsResponse
            {
                Timestamp = DateTime.UtcNow,
                RequestCounts = counters,
                AverageResponseTimes = averageResponseTimes,
                TotalRequests = counters.Values.Sum()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving endpoint metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Endpoint Metrics Error",
                Detail = "An error occurred while retrieving endpoint metrics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get gateway-specific metrics
    /// </summary>
    /// <returns>API Gateway integration metrics</returns>
    [HttpGet("gateway")]
    [ProducesResponseType(typeof(GatewayMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGatewayMetrics()
    {
        try
        {
            var gatewayMetrics = await _gatewayService.GetMetricsAsync();
            var healthInfo = await _gatewayService.GetHealthInfoAsync();

            if (!gatewayMetrics.IsSuccess)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Gateway Metrics Error",
                    Detail = gatewayMetrics.Error,
                    Status = StatusCodes.Status500InternalServerError
                });
            }

            var response = new GatewayMetricsResponse
            {
                Timestamp = DateTime.UtcNow,
                Health = healthInfo.IsSuccess ? healthInfo.Value! : null,
                Metrics = gatewayMetrics.Value!
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gateway metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Gateway Metrics Error",
                Detail = "An error occurred while retrieving gateway metrics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Reset performance metrics (admin only)
    /// </summary>
    /// <returns>Reset confirmation</returns>
    [HttpPost("reset")]
    [Authorize(Roles = "admin,super_admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetMetrics()
    {
        try
        {
            await _performanceMetricsService.ResetMetricsAsync();
            _logger.LogInformation("Performance metrics reset by admin");
            
            return Ok(new { message = "Performance metrics reset successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting performance metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Reset Metrics Error",
                Detail = "An error occurred while resetting performance metrics",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get system health summary
    /// </summary>
    /// <returns>System health information</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealthSummary()
    {
        try
        {
            var healthInfo = await _gatewayService.GetHealthInfoAsync();
            
            if (!healthInfo.IsSuccess)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
                {
                    Title = "Service Unavailable",
                    Detail = healthInfo.Error,
                    Status = StatusCodes.Status503ServiceUnavailable
                });
            }

            var response = new HealthSummaryResponse
            {
                Status = healthInfo.Value!.Status,
                IsHealthy = healthInfo.Value.IsHealthy,
                Timestamp = healthInfo.Value.CheckedAt,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown",
                Uptime = GetUptime()
            };

            return healthInfo.Value.IsHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health summary");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Health Check Error",
                Detail = "An error occurred while checking system health",
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
    }

    private static Dictionary<string, object> GetSystemMetrics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        
        return new Dictionary<string, object>
        {
            { "memory_usage_mb", process.WorkingSet64 / 1024 / 1024 },
            { "cpu_time_ms", process.TotalProcessorTime.TotalMilliseconds },
            { "thread_count", process.Threads.Count },
            { "gc_gen0_collections", GC.CollectionCount(0) },
            { "gc_gen1_collections", GC.CollectionCount(1) },
            { "gc_gen2_collections", GC.CollectionCount(2) },
            { "total_memory_mb", GC.GetTotalMemory(false) / 1024 / 1024 }
        };
    }

    private static TimeSpan GetUptime()
    {
        return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }
}

// Response DTOs
public record SystemMetricsResponse
{
    public DateTime Timestamp { get; init; }
    public GatewayHealthInfo? Health { get; init; }
    public GatewayMetrics? Gateway { get; init; }
    public Dictionary<string, object> Performance { get; init; } = new();
    public Dictionary<string, object> System { get; init; } = new();
}

public record EndpointMetricsResponse
{
    public DateTime Timestamp { get; init; }
    public Dictionary<string, long> RequestCounts { get; init; } = new();
    public Dictionary<string, double> AverageResponseTimes { get; init; } = new();
    public long TotalRequests { get; init; }
}

public record GatewayMetricsResponse
{
    public DateTime Timestamp { get; init; }
    public GatewayHealthInfo? Health { get; init; }
    public GatewayMetrics Metrics { get; init; } = new();
}

public record HealthSummaryResponse
{
    public string Status { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public DateTime Timestamp { get; init; }
    public string Version { get; init; } = string.Empty;
    public TimeSpan Uptime { get; init; }
}
