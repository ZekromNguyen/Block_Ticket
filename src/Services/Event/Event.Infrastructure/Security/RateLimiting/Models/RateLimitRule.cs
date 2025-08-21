namespace Event.Infrastructure.Security.RateLimiting.Models;

/// <summary>
/// Represents a rate limiting rule configuration
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Unique identifier for the rule
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of rate limiting (IP, Client, Organization, Endpoint)
    /// </summary>
    public RateLimitType Type { get; set; }

    /// <summary>
    /// Maximum number of requests allowed
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Time window for the limit (in seconds)
    /// </summary>
    public int WindowSizeInSeconds { get; set; }

    /// <summary>
    /// Endpoint pattern this rule applies to (for endpoint-specific rules)
    /// </summary>
    public string? EndpointPattern { get; set; }

    /// <summary>
    /// HTTP methods this rule applies to
    /// </summary>
    public string[]? HttpMethods { get; set; }

    /// <summary>
    /// Priority of this rule (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this rule is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Custom message to return when rate limit is exceeded
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Additional metadata for the rule
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of rate limiting strategies
/// </summary>
public enum RateLimitType
{
    /// <summary>
    /// Rate limit based on client IP address
    /// </summary>
    IpAddress,

    /// <summary>
    /// Rate limit based on client identifier (API key, JWT subject)
    /// </summary>
    Client,

    /// <summary>
    /// Rate limit based on organization
    /// </summary>
    Organization,

    /// <summary>
    /// Rate limit based on specific endpoint
    /// </summary>
    Endpoint,

    /// <summary>
    /// Global rate limit across all requests
    /// </summary>
    Global
}

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// The rule that was applied
    /// </summary>
    public RateLimitRule? AppliedRule { get; set; }

    /// <summary>
    /// Current count of requests in the window
    /// </summary>
    public long CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public long Limit { get; set; }

    /// <summary>
    /// Time until the window resets (in seconds)
    /// </summary>
    public int ResetTimeInSeconds { get; set; }

    /// <summary>
    /// Retry after time in seconds (when rate limited)
    /// </summary>
    public int? RetryAfterSeconds { get; set; }

    /// <summary>
    /// Additional metadata about the rate limit check
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context information for rate limiting
/// </summary>
public class RateLimitContext
{
    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Client identifier (from JWT, API key, etc.)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Organization identifier
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Request endpoint path
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// HTTP method
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Configuration for rate limiting
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Whether rate limiting is enabled globally
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Redis connection string for distributed rate limiting
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Prefix for Redis keys
    /// </summary>
    public string RedisKeyPrefix { get; set; } = "rate_limit:";

    /// <summary>
    /// Default rate limit rules
    /// </summary>
    public List<RateLimitRule> Rules { get; set; } = new();

    /// <summary>
    /// Whether to use sliding window algorithm (more accurate but more expensive)
    /// </summary>
    public bool UseSlidingWindow { get; set; } = true;

    /// <summary>
    /// How long to cache rate limit results (in seconds)
    /// </summary>
    public int CacheTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to fail open (allow requests) when rate limiting service is unavailable
    /// </summary>
    public bool FailOpen { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for rate limit check (in milliseconds)
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 100;

    /// <summary>
    /// Whether to include rate limit headers in responses
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Custom header names for rate limit information
    /// </summary>
    public RateLimitHeaders Headers { get; set; } = new();
}

/// <summary>
/// Configuration for rate limit response headers
/// </summary>
public class RateLimitHeaders
{
    /// <summary>
    /// Header name for current request count
    /// </summary>
    public string Limit { get; set; } = "X-RateLimit-Limit";

    /// <summary>
    /// Header name for remaining requests
    /// </summary>
    public string Remaining { get; set; } = "X-RateLimit-Remaining";

    /// <summary>
    /// Header name for window reset time
    /// </summary>
    public string Reset { get; set; } = "X-RateLimit-Reset";

    /// <summary>
    /// Header name for retry after time (when rate limited)
    /// </summary>
    public string RetryAfter { get; set; } = "Retry-After";
}
