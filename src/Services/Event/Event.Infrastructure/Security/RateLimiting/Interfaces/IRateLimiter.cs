using Event.Infrastructure.Security.RateLimiting.Models;

namespace Event.Infrastructure.Security.RateLimiting.Interfaces;

/// <summary>
/// Interface for rate limiting service
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request is allowed based on rate limiting rules
    /// </summary>
    /// <param name="context">Rate limiting context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit result</returns>
    Task<RateLimitResult> CheckAsync(RateLimitContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a request is allowed for a specific rule
    /// </summary>
    /// <param name="rule">Rate limit rule to apply</param>
    /// <param name="key">Unique key for the rate limit bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit result</returns>
    Task<RateLimitResult> CheckRuleAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rate limit status for a key
    /// </summary>
    /// <param name="rule">Rate limit rule</param>
    /// <param name="key">Unique key for the rate limit bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current rate limit status</returns>
    Task<RateLimitResult> GetStatusAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for a specific key
    /// </summary>
    /// <param name="rule">Rate limit rule</param>
    /// <param name="key">Unique key for the rate limit bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if reset was successful</returns>
    Task<bool> ResetAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for rate limit key generator
/// </summary>
public interface IRateLimitKeyGenerator
{
    /// <summary>
    /// Generates a unique key for rate limiting based on the rule and context
    /// </summary>
    /// <param name="rule">Rate limit rule</param>
    /// <param name="context">Rate limiting context</param>
    /// <returns>Unique key for the rate limit bucket</returns>
    string GenerateKey(RateLimitRule rule, RateLimitContext context);

    /// <summary>
    /// Generates multiple keys for different rate limiting strategies
    /// </summary>
    /// <param name="rules">Rate limit rules</param>
    /// <param name="context">Rate limiting context</param>
    /// <returns>Dictionary of rule ID to key mappings</returns>
    Dictionary<string, string> GenerateKeys(IEnumerable<RateLimitRule> rules, RateLimitContext context);
}

/// <summary>
/// Interface for rate limit rule provider
/// </summary>
public interface IRateLimitRuleProvider
{
    /// <summary>
    /// Gets all applicable rate limit rules for a context
    /// </summary>
    /// <param name="context">Rate limiting context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applicable rules ordered by priority</returns>
    Task<IEnumerable<RateLimitRule>> GetApplicableRulesAsync(RateLimitContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific rule by ID
    /// </summary>
    /// <param name="ruleId">Rule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit rule or null if not found</returns>
    Task<RateLimitRule?> GetRuleAsync(string ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates or adds a rate limit rule
    /// </summary>
    /// <param name="rule">Rate limit rule to update or add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> UpsertRuleAsync(RateLimitRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a rate limit rule
    /// </summary>
    /// <param name="ruleId">Rule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> RemoveRuleAsync(string ruleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for rate limit storage
/// </summary>
public interface IRateLimitStorage
{
    /// <summary>
    /// Increments the counter for a key and returns the current count
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="windowSizeSeconds">Window size in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current count and expiration time</returns>
    Task<(long count, DateTime expiration)> IncrementAsync(string key, int windowSizeSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count for a key
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current count and expiration time</returns>
    Task<(long count, DateTime expiration)> GetCountAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the counter for a key
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments using sliding window algorithm
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <param name="windowSizeSeconds">Window size in seconds</param>
    /// <param name="limit">Maximum allowed count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current count and whether the limit is exceeded</returns>
    Task<(long count, bool isExceeded)> SlidingWindowIncrementAsync(string key, int windowSizeSeconds, int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for rate limit metrics collection
/// </summary>
public interface IRateLimitMetrics
{
    /// <summary>
    /// Records a rate limit check
    /// </summary>
    /// <param name="context">Rate limiting context</param>
    /// <param name="result">Rate limit result</param>
    /// <param name="duration">Time taken for the check</param>
    void RecordRateLimitCheck(RateLimitContext context, RateLimitResult result, TimeSpan duration);

    /// <summary>
    /// Records a rate limit violation
    /// </summary>
    /// <param name="context">Rate limiting context</param>
    /// <param name="rule">Rule that was violated</param>
    void RecordRateLimitViolation(RateLimitContext context, RateLimitRule rule);

    /// <summary>
    /// Records a rate limit error
    /// </summary>
    /// <param name="context">Rate limiting context</param>
    /// <param name="error">Error that occurred</param>
    void RecordRateLimitError(RateLimitContext context, Exception error);

    /// <summary>
    /// Gets rate limit statistics
    /// </summary>
    /// <param name="timeRange">Time range for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit statistics</returns>
    Task<RateLimitStatistics> GetStatisticsAsync(TimeSpan timeRange, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limit statistics
/// </summary>
public class RateLimitStatistics
{
    /// <summary>
    /// Total number of requests checked
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of requests that were rate limited
    /// </summary>
    public long RateLimitedRequests { get; set; }

    /// <summary>
    /// Rate limit violation rate (0.0 to 1.0)
    /// </summary>
    public double ViolationRate => TotalRequests > 0 ? (double)RateLimitedRequests / TotalRequests : 0.0;

    /// <summary>
    /// Average response time for rate limit checks
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// Number of errors during rate limit checks
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Statistics by rule type
    /// </summary>
    public Dictionary<RateLimitType, RateLimitTypeStatistics> ByRuleType { get; set; } = new();

    /// <summary>
    /// Top violating IP addresses
    /// </summary>
    public List<(string ipAddress, long violations)> TopViolatingIPs { get; set; } = new();

    /// <summary>
    /// Top violating clients
    /// </summary>
    public List<(string clientId, long violations)> TopViolatingClients { get; set; } = new();
}

/// <summary>
/// Statistics for a specific rate limit type
/// </summary>
public class RateLimitTypeStatistics
{
    /// <summary>
    /// Total requests for this type
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Rate limited requests for this type
    /// </summary>
    public long RateLimitedRequests { get; set; }

    /// <summary>
    /// Average response time for this type
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }
}
