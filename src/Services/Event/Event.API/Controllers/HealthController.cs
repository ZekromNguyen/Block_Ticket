using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Health check API controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(HealthStatusResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusResponse>> GetHealth(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Health check requested");

        var healthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new HealthStatusResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration,
            Checks = healthReport.Entries.Select(entry => new HealthCheckResponse
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration,
                Description = entry.Value.Description,
                Data = entry.Value.Data,
                Exception = entry.Value.Exception?.Message
            }).ToList()
        };

        var statusCode = healthReport.Status == HealthStatus.Healthy 
            ? HttpStatusCode.OK 
            : HttpStatusCode.ServiceUnavailable;

        return StatusCode((int)statusCode, response);
    }

    /// <summary>
    /// Get readiness status (detailed health check)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Readiness status</returns>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(HealthStatusResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(HealthStatusResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusResponse>> GetReadiness(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Readiness check requested");

        var healthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new HealthStatusResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration,
            Checks = healthReport.Entries.Select(entry => new HealthCheckResponse
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration,
                Description = entry.Value.Description,
                Data = entry.Value.Data,
                Exception = entry.Value.Exception?.Message
            }).ToList()
        };

        var statusCode = healthReport.Status == HealthStatus.Healthy 
            ? HttpStatusCode.OK 
            : HttpStatusCode.ServiceUnavailable;

        return StatusCode((int)statusCode, response);
    }

    /// <summary>
    /// Get liveness status (basic health check)
    /// </summary>
    /// <returns>Liveness status</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LivenessResponse), (int)HttpStatusCode.OK)]
    public ActionResult<LivenessResponse> GetLiveness()
    {
        _logger.LogInformation("Liveness check requested");

        var response = new LivenessResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Event.API",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown"
        };

        return Ok(response);
    }

    /// <summary>
    /// Get service information
    /// </summary>
    /// <returns>Service information</returns>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ServiceInfoResponse), (int)HttpStatusCode.OK)]
    public ActionResult<ServiceInfoResponse> GetServiceInfo()
    {
        _logger.LogInformation("Service info requested");

        var assembly = GetType().Assembly;
        var buildDate = System.IO.File.GetCreationTime(assembly.Location);

        var response = new ServiceInfoResponse
        {
            ServiceName = "Block Ticket Event API",
            Version = assembly.GetName().Version?.ToString() ?? "Unknown",
            BuildDate = buildDate,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            StartTime = Process.GetCurrentProcess().StartTime,
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
            MemoryUsage = new MemoryUsageInfo
            {
                WorkingSet = Environment.WorkingSet,
                PrivateMemorySize = Process.GetCurrentProcess().PrivateMemorySize64,
                VirtualMemorySize = Process.GetCurrentProcess().VirtualMemorySize64
            }
        };

        return Ok(response);
    }
}

/// <summary>
/// Health status response
/// </summary>
public record HealthStatusResponse
{
    public string Status { get; init; } = string.Empty;
    public TimeSpan TotalDuration { get; init; }
    public List<HealthCheckResponse> Checks { get; init; } = new();
}

/// <summary>
/// Individual health check response
/// </summary>
public record HealthCheckResponse
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }
    public string? Exception { get; init; }
}

/// <summary>
/// Liveness response
/// </summary>
public record LivenessResponse
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Service { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}

/// <summary>
/// Service information response
/// </summary>
public record ServiceInfoResponse
{
    public string ServiceName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTime BuildDate { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public DateTime StartTime { get; init; }
    public TimeSpan Uptime { get; init; }
    public MemoryUsageInfo MemoryUsage { get; init; } = new();
}

/// <summary>
/// Memory usage information
/// </summary>
public record MemoryUsageInfo
{
    public long WorkingSet { get; init; }
    public long PrivateMemorySize { get; init; }
    public long VirtualMemorySize { get; init; }
}
