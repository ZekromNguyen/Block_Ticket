using Event.Domain.Models;
using Event.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Controller for performance metrics and SLA monitoring
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/metrics")]
[ApiVersion("1.0")]
public class MetricsController : ControllerBase
{
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly ISlaMonitoringService _slaMonitoringService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IPerformanceMonitoringService performanceMonitoringService,
        ISlaMonitoringService slaMonitoringService,
        ILogger<MetricsController> logger)
    {
        _performanceMonitoringService = performanceMonitoringService;
        _slaMonitoringService = slaMonitoringService;
        _logger = logger;
    }

    /// <summary>
    /// Get performance summary for a service
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="operationName">Operation name (optional)</param>
    /// <param name="startTime">Start time (optional, defaults to 1 hour ago)</param>
    /// <param name="endTime">End time (optional, defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance summary</returns>
    [HttpGet("performance/summary")]
    [ProducesResponseType(typeof(PerformanceSummary), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PerformanceSummary>> GetPerformanceSummary(
        [FromQuery] [Required] string serviceName,
        [FromQuery] string? operationName = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _performanceMonitoringService.GetPerformanceSummaryAsync(
                serviceName, operationName, startTime, endTime, cancellationToken);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance summary for {ServiceName}.{OperationName}", 
                serviceName, operationName);
            return StatusCode(500, "Failed to retrieve performance summary");
        }
    }

    /// <summary>
    /// Get real-time metrics for a service
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time metrics</returns>
    [HttpGet("performance/realtime")]
    [ProducesResponseType(typeof(RealTimeMetrics), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<RealTimeMetrics>> GetRealTimeMetrics(
        [FromQuery] [Required] string serviceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _performanceMonitoringService.GetRealTimeMetricsAsync(serviceName, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time metrics for {ServiceName}", serviceName);
            return StatusCode(500, "Failed to retrieve real-time metrics");
        }
    }

    /// <summary>
    /// Get response time percentiles
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="operationName">Operation name (optional)</param>
    /// <param name="startTime">Start time (optional)</param>
    /// <param name="endTime">End time (optional)</param>
    /// <param name="percentiles">Percentiles to calculate (optional, defaults to 50,90,95,99)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response time percentiles</returns>
    [HttpGet("performance/percentiles")]
    [ProducesResponseType(typeof(Dictionary<int, double>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Dictionary<int, double>>> GetResponseTimePercentiles(
        [FromQuery] [Required] string serviceName,
        [FromQuery] string? operationName = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int[]? percentiles = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _performanceMonitoringService.GetResponseTimePercentilesAsync(
                serviceName, operationName, startTime, endTime, percentiles, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get response time percentiles for {ServiceName}.{OperationName}", 
                serviceName, operationName);
            return StatusCode(500, "Failed to retrieve response time percentiles");
        }
    }

    /// <summary>
    /// Get error metrics
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="startTime">Start time (optional)</param>
    /// <param name="endTime">End time (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error metrics grouped by exception type</returns>
    [HttpGet("performance/errors")]
    [ProducesResponseType(typeof(Dictionary<string, int>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Dictionary<string, int>>> GetErrorMetrics(
        [FromQuery] [Required] string serviceName,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _performanceMonitoringService.GetErrorMetricsAsync(
                serviceName, startTime, endTime, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error metrics for {ServiceName}", serviceName);
            return StatusCode(500, "Failed to retrieve error metrics");
        }
    }

    /// <summary>
    /// Get SLA definitions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active SLA definitions</returns>
    [HttpGet("sla/definitions")]
    [ProducesResponseType(typeof(IEnumerable<SlaDefinition>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<IEnumerable<SlaDefinition>>> GetSlaDefinitions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var definitions = await _slaMonitoringService.GetActiveSlaDefinitionsAsync(cancellationToken);
            return Ok(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SLA definitions");
            return StatusCode(500, "Failed to retrieve SLA definitions");
        }
    }

    /// <summary>
    /// Create a new SLA definition
    /// </summary>
    /// <param name="request">SLA definition request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created SLA definition</returns>
    [HttpPost("sla/definitions")]
    [ProducesResponseType(typeof(SlaDefinition), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SlaDefinition>> CreateSlaDefinition(
        [FromBody] CreateSlaDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var slaDefinition = new SlaDefinition
            {
                Name = request.Name,
                ServiceName = request.ServiceName,
                OperationName = request.OperationName,
                TargetResponseTimeMs = request.TargetResponseTimeMs,
                TargetAvailabilityPercent = request.TargetAvailabilityPercent,
                TargetErrorRatePercent = request.TargetErrorRatePercent,
                TargetThroughputRps = request.TargetThroughputRps,
                ResponseTimePercentile = request.ResponseTimePercentile,
                TimeWindowMinutes = request.TimeWindowMinutes,
                AlertThresholdPercent = request.AlertThresholdPercent,
                IsActive = true
            };

            var result = await _slaMonitoringService.CreateSlaDefinitionAsync(slaDefinition, cancellationToken);
            
            return CreatedAtAction(nameof(GetSlaDefinitions), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SLA definition for {ServiceName}", request.ServiceName);
            return StatusCode(500, "Failed to create SLA definition");
        }
    }

    /// <summary>
    /// Get SLA violations
    /// </summary>
    /// <param name="startTime">Start time (optional)</param>
    /// <param name="endTime">End time (optional)</param>
    /// <param name="serviceName">Service name filter (optional)</param>
    /// <param name="includeResolved">Include resolved violations (optional, defaults to true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SLA violations</returns>
    [HttpGet("sla/violations")]
    [ProducesResponseType(typeof(IEnumerable<SlaViolation>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<IEnumerable<SlaViolation>>> GetSlaViolations(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] bool includeResolved = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var violations = await _slaMonitoringService.GetSlaViolationsAsync(
                startTime, endTime, serviceName, includeResolved, cancellationToken);

            return Ok(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SLA violations");
            return StatusCode(500, "Failed to retrieve SLA violations");
        }
    }

    /// <summary>
    /// Get SLA compliance report
    /// </summary>
    /// <param name="slaDefinitionId">SLA definition ID</param>
    /// <param name="startTime">Report period start time</param>
    /// <param name="endTime">Report period end time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SLA compliance report</returns>
    [HttpGet("sla/compliance/{slaDefinitionId:guid}")]
    [ProducesResponseType(typeof(SlaComplianceReport), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<SlaComplianceReport>> GetSlaComplianceReport(
        [FromRoute] Guid slaDefinitionId,
        [FromQuery] [Required] DateTime startTime,
        [FromQuery] [Required] DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _slaMonitoringService.GetSlaComplianceReportAsync(
                slaDefinitionId, startTime, endTime, cancellationToken);

            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "SLA definition {SlaDefinitionId} not found", slaDefinitionId);
            return NotFound($"SLA definition {slaDefinitionId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SLA compliance report for {SlaDefinitionId}", slaDefinitionId);
            return StatusCode(500, "Failed to retrieve SLA compliance report");
        }
    }

    /// <summary>
    /// Trigger SLA compliance check
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("sla/check")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<ActionResult> CheckSlaCompliance(CancellationToken cancellationToken = default)
    {
        try
        {
            await _slaMonitoringService.CheckSlaComplianceAsync(cancellationToken);
            return Ok(new { message = "SLA compliance check completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SLA compliance");
            return StatusCode(500, "Failed to check SLA compliance");
        }
    }

    /// <summary>
    /// Resolve an SLA violation
    /// </summary>
    /// <param name="violationId">Violation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("sla/violations/{violationId:guid}/resolve")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> ResolveSlaViolation(
        [FromRoute] Guid violationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _slaMonitoringService.ResolveSlaViolationAsync(violationId, cancellationToken);
            return Ok(new { message = "SLA violation resolved" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "SLA violation {ViolationId} not found", violationId);
            return NotFound($"SLA violation {violationId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve SLA violation {ViolationId}", violationId);
            return StatusCode(500, "Failed to resolve SLA violation");
        }
    }

    /// <summary>
    /// Get comprehensive metrics dashboard data
    /// </summary>
    /// <param name="serviceName">Service name</param>
    /// <param name="timeRangeHours">Time range in hours (optional, defaults to 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard data</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(MetricsDashboardData), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<MetricsDashboardData>> GetDashboardData(
        [FromQuery] string serviceName = "Event.API",
        [FromQuery] int timeRangeHours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-timeRangeHours);

            var dashboardTasks = new[]
            {
                _performanceMonitoringService.GetPerformanceSummaryAsync(serviceName, null, startTime, endTime, cancellationToken),
                _performanceMonitoringService.GetRealTimeMetricsAsync(serviceName, cancellationToken),
                _performanceMonitoringService.GetErrorMetricsAsync(serviceName, startTime, endTime, cancellationToken),
                _slaMonitoringService.GetSlaViolationsAsync(startTime, endTime, serviceName, false, cancellationToken)
            };

            var results = await Task.WhenAll(dashboardTasks);

            var dashboard = new MetricsDashboardData
            {
                ServiceName = serviceName,
                TimeRangeStart = startTime,
                TimeRangeEnd = endTime,
                PerformanceSummary = (PerformanceSummary)results[0],
                RealTimeMetrics = (RealTimeMetrics)results[1],
                ErrorMetrics = (Dictionary<string, int>)results[2],
                ActiveViolations = ((IEnumerable<SlaViolation>)results[3]).ToList(),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data for {ServiceName}", serviceName);
            return StatusCode(500, "Failed to retrieve dashboard data");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for creating SLA definitions
/// </summary>
public record CreateSlaDefinitionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ServiceName { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? OperationName { get; init; }

    [Range(1, 60000)]
    public double TargetResponseTimeMs { get; init; } = 1000;

    [Range(0, 100)]
    public double TargetAvailabilityPercent { get; init; } = 99.9;

    [Range(0, 100)]
    public double TargetErrorRatePercent { get; init; } = 1.0;

    [Range(0, 10000)]
    public double? TargetThroughputRps { get; init; }

    [Range(50, 99)]
    public int ResponseTimePercentile { get; init; } = 95;

    [Range(1, 60)]
    public int TimeWindowMinutes { get; init; } = 5;

    [Range(0, 100)]
    public double AlertThresholdPercent { get; init; } = 80.0;
}

/// <summary>
/// Dashboard data model
/// </summary>
public class MetricsDashboardData
{
    public string ServiceName { get; set; } = string.Empty;
    public DateTime TimeRangeStart { get; set; }
    public DateTime TimeRangeEnd { get; set; }
    public PerformanceSummary PerformanceSummary { get; set; } = null!;
    public RealTimeMetrics RealTimeMetrics { get; set; } = null!;
    public Dictionary<string, int> ErrorMetrics { get; set; } = new();
    public List<SlaViolation> ActiveViolations { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

#endregion
