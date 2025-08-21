using Event.Domain.Models;

namespace Event.Application.Interfaces.Infrastructure;

/// <summary>
/// Service for collecting and tracking performance metrics
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Record a performance metric
    /// </summary>
    Task RecordMetricAsync(PerformanceMetrics metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record multiple metrics in batch
    /// </summary>
    Task RecordMetricsBatchAsync(IEnumerable<PerformanceMetrics> metrics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance summary for a time period
    /// </summary>
    Task<PerformanceSummary> GetPerformanceSummaryAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time metrics
    /// </summary>
    Task<RealTimeMetrics> GetRealTimeMetricsAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get metrics for a specific time range
    /// </summary>
    Task<IEnumerable<PerformanceMetrics>> GetMetricsAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int limit = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate percentile response times
    /// </summary>
    Task<Dictionary<int, double>> GetResponseTimePercentilesAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int[]? percentiles = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get error metrics
    /// </summary>
    Task<Dictionary<string, int>> GetErrorMetricsAsync(
        string serviceName,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear old metrics (for cleanup)
    /// </summary>
    Task CleanupOldMetricsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for SLA monitoring and tracking
/// </summary>
public interface ISlaMonitoringService
{
    /// <summary>
    /// Create a new SLA definition
    /// </summary>
    Task<SlaDefinition> CreateSlaDefinitionAsync(SlaDefinition slaDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing SLA definition
    /// </summary>
    Task UpdateSlaDefinitionAsync(SlaDefinition slaDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active SLA definitions
    /// </summary>
    Task<IEnumerable<SlaDefinition>> GetActiveSlaDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check SLA compliance for all active SLAs
    /// </summary>
    Task CheckSlaComplianceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA violations for a specific period
    /// </summary>
    Task<IEnumerable<SlaViolation>> GetSlaViolationsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? serviceName = null,
        bool includeResolved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA compliance report
    /// </summary>
    Task<SlaComplianceReport> GetSlaComplianceReportAsync(
        Guid slaDefinitionId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve an SLA violation
    /// </summary>
    Task ResolveSlaViolationAsync(Guid violationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for performance alerting
/// </summary>
public interface IPerformanceAlertingService
{
    /// <summary>
    /// Create a new performance alert
    /// </summary>
    Task<PerformanceAlert> CreateAlertAsync(PerformanceAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing alert
    /// </summary>
    Task UpdateAlertAsync(PerformanceAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an alert
    /// </summary>
    Task DeleteAlertAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active alerts
    /// </summary>
    Task<IEnumerable<PerformanceAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate all alerts for recent metrics
    /// </summary>
    Task EvaluateAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send alert notification
    /// </summary>
    Task SendAlertNotificationAsync(PerformanceAlert alert, double currentValue, CancellationToken cancellationToken = default);
}

/// <summary>
/// SLA compliance report
/// </summary>
public class SlaComplianceReport
{
    public Guid SlaDefinitionId { get; set; }
    public string SlaName { get; set; } = string.Empty;
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public bool IsCompliant { get; set; }
    public double CompliancePercentage { get; set; }
    public TimeSpan TotalDowntime { get; set; }
    public int ViolationCount { get; set; }
    public double ActualAvailability { get; set; }
    public double ActualResponseTimeP95 { get; set; }
    public double ActualErrorRate { get; set; }
    public double ActualThroughput { get; set; }
    public List<SlaViolation> Violations { get; set; } = new();
}

/// <summary>
/// Performance monitoring extension methods
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Create a basic performance metric
    /// </summary>
    public static PerformanceMetrics CreateMetric(
        string serviceName,
        string operationName,
        double durationMs,
        int? statusCode = null,
        string? errorMessage = null)
    {
        return new PerformanceMetrics
        {
            Id = Guid.NewGuid(),
            ServiceName = serviceName,
            OperationName = operationName,
            DurationMs = durationMs,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create an HTTP request metric
    /// </summary>
    public static PerformanceMetrics CreateHttpMetric(
        string serviceName,
        string httpMethod,
        string requestPath,
        int statusCode,
        double durationMs,
        string? errorMessage = null)
    {
        return new PerformanceMetrics
        {
            Id = Guid.NewGuid(),
            ServiceName = serviceName,
            OperationName = $"{httpMethod} {requestPath}",
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            StatusCode = statusCode,
            DurationMs = durationMs,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a database operation metric
    /// </summary>
    public static PerformanceMetrics CreateDatabaseMetric(
        string serviceName,
        string operationName,
        double durationMs,
        int queryCount,
        string? errorMessage = null)
    {
        return new PerformanceMetrics
        {
            Id = Guid.NewGuid(),
            ServiceName = serviceName,
            OperationName = operationName,
            DurationMs = durationMs,
            DatabaseQueryCount = queryCount,
            DatabaseDurationMs = durationMs,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Check if a metric indicates a failure
    /// </summary>
    public static bool IsFailure(this PerformanceMetrics metric)
    {
        return !string.IsNullOrEmpty(metric.ErrorMessage) ||
               (metric.StatusCode.HasValue && metric.StatusCode >= 400);
    }

    /// <summary>
    /// Check if a metric indicates success
    /// </summary>
    public static bool IsSuccess(this PerformanceMetrics metric)
    {
        return !IsFailure(metric);
    }
}
