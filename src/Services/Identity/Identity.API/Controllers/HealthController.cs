using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace Identity.API.Controllers;

/// <summary>
/// Health check endpoints for monitoring and load balancing
/// </summary>
[ApiController]
[Route("health")]
[ApiExplorerSettings(IgnoreApi = true)]
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
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            }),
            duration = report.TotalDuration.ToString()
        };

        return report.Status == HealthStatus.Healthy 
            ? Ok(response) 
            : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
    }

    /// <summary>
    /// Readiness probe for Kubernetes
    /// </summary>
    /// <returns>Readiness status</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        if (report.Status == HealthStatus.Healthy)
        {
            return Ok(new { status = "Ready" });
        }

        _logger.LogWarning("Readiness check failed: {Status}", report.Status);
        return StatusCode((int)HttpStatusCode.ServiceUnavailable, new { status = "Not Ready" });
    }

    /// <summary>
    /// Liveness probe for Kubernetes
    /// </summary>
    /// <returns>Liveness status</returns>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Alive" });
    }

    /// <summary>
    /// Detailed health information for administrators
    /// </summary>
    /// <returns>Detailed health report</returns>
    [HttpGet("detailed")]
    public async Task<IActionResult> Detailed()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString(),
                data = x.Value.Data
            }),
            timestamp = DateTime.UtcNow
        };

        return report.Status == HealthStatus.Healthy 
            ? Ok(response) 
            : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
    }
}
