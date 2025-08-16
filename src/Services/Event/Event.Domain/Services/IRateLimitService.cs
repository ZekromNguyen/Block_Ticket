using Event.Domain.Configuration;

namespace Event.Domain.Services;

/// <summary>
/// Service for handling rate limiting logic
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request should be rate limited
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        string method,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a request for rate limiting purposes
    /// </summary>
    Task RecordRequestAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        string method,
        bool wasBlocked,
        string? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current rate limit status for a client
    /// </summary>
    Task<RateLimitStatus> GetRateLimitStatusAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rate limit metrics for monitoring
    /// </summary>
    Task<IEnumerable<RateLimitMetrics>> GetMetricsAsync(
        TimeSpan? window = null,
        string? clientId = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears rate limit counters for a client (admin operation)
    /// </summary>
    Task ClearRateLimitAsync(
        string? clientId = null,
        string? ipAddress = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a client to the whitelist temporarily
    /// </summary>
    Task AddToWhitelistAsync(
        string clientId,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a client from the whitelist
    /// </summary>
    Task RemoveFromWhitelistAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a client or IP is whitelisted
    /// </summary>
    Task<bool> IsWhitelistedAsync(
        string? clientId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration for rate limiting
    /// </summary>
    RateLimitConfiguration GetConfiguration();

    /// <summary>
    /// Updates rate limiting configuration (hot reload)
    /// </summary>
    Task UpdateConfigurationAsync(
        RateLimitConfiguration configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request should be blocked
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Current request count in the window
    /// </summary>
    public long CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests in the window
    /// </summary>
    public long Limit { get; set; }

    /// <summary>
    /// Time period for the rate limit
    /// </summary>
    public TimeSpan Period { get; set; }

    /// <summary>
    /// Time when the rate limit window resets
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Retry after duration
    /// </summary>
    public TimeSpan RetryAfter { get; set; }

    /// <summary>
    /// Rate limit rule that was violated (if any)
    /// </summary>
    public RateLimitRule? ViolatedRule { get; set; }

    /// <summary>
    /// Endpoint that was rate limited
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Reason for rate limiting
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Current rate limit status
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Client identifier
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Current request count
    /// </summary>
    public long CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public long Limit { get; set; }

    /// <summary>
    /// Remaining requests in current window
    /// </summary>
    public long RemainingRequests { get; set; }

    /// <summary>
    /// Window start time
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// Window end time
    /// </summary>
    public DateTime WindowEnd { get; set; }

    /// <summary>
    /// Whether client is currently blocked
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Time until rate limit resets
    /// </summary>
    public TimeSpan TimeUntilReset { get; set; }
}
