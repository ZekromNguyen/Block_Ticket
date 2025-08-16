using System.ComponentModel.DataAnnotations;

namespace Event.Domain.Models;

/// <summary>
/// Performance metrics data model
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Unique identifier for the metric entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the metric was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Service name (e.g., "Event.API")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Operation name (e.g., "SearchEvents", "CreateReservation")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method for API operations
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path or endpoint
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Response status code
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long? MemoryUsageBytes { get; set; }

    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public double? CpuUsagePercent { get; set; }

    /// <summary>
    /// Number of database queries executed
    /// </summary>
    public int? DatabaseQueryCount { get; set; }

    /// <summary>
    /// Total database query time in milliseconds
    /// </summary>
    public double? DatabaseDurationMs { get; set; }

    /// <summary>
    /// Cache hit/miss indicator
    /// </summary>
    public bool? CacheHit { get; set; }

    /// <summary>
    /// Number of items processed (for batch operations)
    /// </summary>
    public int? ItemCount { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception type if operation failed
    /// </summary>
    [MaxLength(200)]
    public string? ExceptionType { get; set; }

    /// <summary>
    /// User or client identifier
    /// </summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Organization identifier
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Request correlation ID
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// SLA (Service Level Agreement) definition
/// </summary>
public class SlaDefinition
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// SLA name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service name this SLA applies to
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Operation name (null for all operations)
    /// </summary>
    [MaxLength(200)]
    public string? OperationName { get; set; }

    /// <summary>
    /// Target response time in milliseconds
    /// </summary>
    public double TargetResponseTimeMs { get; set; }

    /// <summary>
    /// Target availability percentage (0-100)
    /// </summary>
    public double TargetAvailabilityPercent { get; set; } = 99.9;

    /// <summary>
    /// Target error rate percentage (0-100)
    /// </summary>
    public double TargetErrorRatePercent { get; set; } = 1.0;

    /// <summary>
    /// Target throughput (requests per second)
    /// </summary>
    public double? TargetThroughputRps { get; set; }

    /// <summary>
    /// Percentile for response time measurement (e.g., 95 for P95)
    /// </summary>
    public int ResponseTimePercentile { get; set; } = 95;

    /// <summary>
    /// Time window for SLA calculation in minutes
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Whether this SLA is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Alert threshold percentage (when to trigger alerts)
    /// </summary>
    public double AlertThresholdPercent { get; set; } = 80.0;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// SLA violation record
/// </summary>
public class SlaViolation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// SLA definition that was violated
    /// </summary>
    public Guid SlaDefinitionId { get; set; }

    /// <summary>
    /// When the violation occurred
    /// </summary>
    public DateTime ViolationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the violation was resolved (null if ongoing)
    /// </summary>
    public DateTime? ResolvedTime { get; set; }

    /// <summary>
    /// Type of violation
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ViolationType { get; set; } = string.Empty; // ResponseTime, Availability, ErrorRate, Throughput

    /// <summary>
    /// Expected value based on SLA
    /// </summary>
    public double ExpectedValue { get; set; }

    /// <summary>
    /// Actual measured value
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    /// <summary>
    /// Additional details about the violation
    /// </summary>
    [MaxLength(1000)]
    public string? Details { get; set; }

    /// <summary>
    /// Whether alerts have been sent
    /// </summary>
    public bool AlertSent { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public SlaDefinition SlaDefinition { get; set; } = null!;
}

/// <summary>
/// Performance summary for reporting
/// </summary>
public class PerformanceSummary
{
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Operation name
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Time period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Time period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total number of requests
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// P50 response time in milliseconds
    /// </summary>
    public double P50ResponseTimeMs { get; set; }

    /// <summary>
    /// P95 response time in milliseconds
    /// </summary>
    public double P95ResponseTimeMs { get; set; }

    /// <summary>
    /// P99 response time in milliseconds
    /// </summary>
    public double P99ResponseTimeMs { get; set; }

    /// <summary>
    /// Maximum response time in milliseconds
    /// </summary>
    public double MaxResponseTimeMs { get; set; }

    /// <summary>
    /// Minimum response time in milliseconds
    /// </summary>
    public double MinResponseTimeMs { get; set; }

    /// <summary>
    /// Requests per second
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRatePercent { get; set; }

    /// <summary>
    /// Availability percentage
    /// </summary>
    public double AvailabilityPercent { get; set; }

    /// <summary>
    /// Average CPU usage percentage
    /// </summary>
    public double? AverageCpuUsagePercent { get; set; }

    /// <summary>
    /// Average memory usage in bytes
    /// </summary>
    public long? AverageMemoryUsageBytes { get; set; }

    /// <summary>
    /// Cache hit rate percentage
    /// </summary>
    public double? CacheHitRatePercent { get; set; }

    /// <summary>
    /// Average database query time
    /// </summary>
    public double? AverageDatabaseDurationMs { get; set; }
}

/// <summary>
/// Real-time performance metrics
/// </summary>
public class RealTimeMetrics
{
    /// <summary>
    /// Current timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current requests per second
    /// </summary>
    public double CurrentRps { get; set; }

    /// <summary>
    /// Current average response time (last 1 minute)
    /// </summary>
    public double CurrentAvgResponseTimeMs { get; set; }

    /// <summary>
    /// Current error rate (last 1 minute)
    /// </summary>
    public double CurrentErrorRatePercent { get; set; }

    /// <summary>
    /// Current CPU usage
    /// </summary>
    public double CurrentCpuUsagePercent { get; set; }

    /// <summary>
    /// Current memory usage
    /// </summary>
    public long CurrentMemoryUsageBytes { get; set; }

    /// <summary>
    /// Active connections count
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Database connection pool usage
    /// </summary>
    public int DatabasePoolUsage { get; set; }

    /// <summary>
    /// Cache hit rate (last 5 minutes)
    /// </summary>
    public double CacheHitRatePercent { get; set; }

    /// <summary>
    /// Queue depth (if applicable)
    /// </summary>
    public int QueueDepth { get; set; }
}

/// <summary>
/// Performance alert configuration
/// </summary>
public class PerformanceAlert
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Alert name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service name to monitor
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Metric type to monitor
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MetricType { get; set; } = string.Empty; // ResponseTime, ErrorRate, Throughput, Availability

    /// <summary>
    /// Threshold value
    /// </summary>
    public double ThresholdValue { get; set; }

    /// <summary>
    /// Comparison operator
    /// </summary>
    [MaxLength(10)]
    public string Operator { get; set; } = ">="; // >=, <=, >, <, ==

    /// <summary>
    /// Time window for evaluation in minutes
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Evaluation frequency in minutes
    /// </summary>
    public int EvaluationFrequencyMinutes { get; set; } = 1;

    /// <summary>
    /// Alert severity
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium";

    /// <summary>
    /// Whether the alert is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Alert recipients (emails, webhooks, etc.)
    /// </summary>
    public string? Recipients { get; set; }

    /// <summary>
    /// Cooldown period in minutes to prevent alert spam
    /// </summary>
    public int CooldownMinutes { get; set; } = 15;

    /// <summary>
    /// Last time this alert was triggered
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enums for metrics
/// </summary>
public static class MetricTypes
{
    public const string ResponseTime = "ResponseTime";
    public const string ErrorRate = "ErrorRate";
    public const string Throughput = "Throughput";
    public const string Availability = "Availability";
    public const string CpuUsage = "CpuUsage";
    public const string MemoryUsage = "MemoryUsage";
    public const string DatabaseDuration = "DatabaseDuration";
    public const string CacheHitRate = "CacheHitRate";
}

public static class ViolationTypes
{
    public const string ResponseTime = "ResponseTime";
    public const string Availability = "Availability";
    public const string ErrorRate = "ErrorRate";
    public const string Throughput = "Throughput";
}

public static class SeverityLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}
