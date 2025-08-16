namespace Event.Domain.Configuration;

/// <summary>
/// Configuration for rate limiting policies
/// </summary>
public class RateLimitConfiguration
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default rate limit settings
    /// </summary>
    public RateLimitPolicy Default { get; set; } = new();

    /// <summary>
    /// IP-based rate limiting settings
    /// </summary>
    public RateLimitPolicy IpRateLimit { get; set; } = new();

    /// <summary>
    /// Client ID-based rate limiting settings
    /// </summary>
    public RateLimitPolicy ClientRateLimit { get; set; } = new();

    /// <summary>
    /// Endpoint-specific rate limiting rules
    /// </summary>
    public List<EndpointRateLimitRule> EndpointRules { get; set; } = new();

    /// <summary>
    /// Whitelist of IPs that bypass rate limiting
    /// </summary>
    public List<string> IpWhitelist { get; set; } = new();

    /// <summary>
    /// Whitelist of client IDs that bypass rate limiting
    /// </summary>
    public List<string> ClientWhitelist { get; set; } = new();

    /// <summary>
    /// Redis connection string for distributed rate limiting
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Stack blocking behavior when rate limit exceeded
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// Real IP header name (for proxy scenarios)
    /// </summary>
    public string RealIPHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Client ID header name
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// HTTP status code to return when rate limit exceeded
    /// </summary>
    public int HttpStatusCode { get; set; } = 429;

    /// <summary>
    /// Rate limit quota exceeded message
    /// </summary>
    public string QuotaExceededMessage { get; set; } = "API calls quota exceeded! Maximum allowed: {0} per {1}.";

    /// <summary>
    /// Enable request count metrics
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
}

/// <summary>
/// Rate limit policy settings
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Rate limit rules
    /// </summary>
    public List<RateLimitRule> Rules { get; set; } = new();

    /// <summary>
    /// Whether to enable endpoint-specific rate limiting
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;
}

/// <summary>
/// Individual rate limit rule
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Time period (e.g., "1s", "1m", "1h", "1d")
    /// </summary>
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Period timespan (calculated from Period)
    /// </summary>
    public TimeSpan PeriodTimespan { get; set; }

    /// <summary>
    /// Maximum number of requests allowed in the period
    /// </summary>
    public long Limit { get; set; }
}

/// <summary>
/// Endpoint-specific rate limiting rule
/// </summary>
public class EndpointRateLimitRule
{
    /// <summary>
    /// Endpoint pattern (supports wildcards)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, etc. or * for all)
    /// </summary>
    public string Method { get; set; } = "*";

    /// <summary>
    /// Rate limit rules for this endpoint
    /// </summary>
    public List<RateLimitRule> Rules { get; set; } = new();

    /// <summary>
    /// Limit per IP for this endpoint
    /// </summary>
    public long? LimitPerIP { get; set; }

    /// <summary>
    /// Limit per client for this endpoint
    /// </summary>
    public long? LimitPerClient { get; set; }

    /// <summary>
    /// Period for endpoint-specific limits
    /// </summary>
    public string Period { get; set; } = "1m";
}

/// <summary>
/// Rate limit response model
/// </summary>
public class RateLimitResponse
{
    public string Message { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public long CurrentRequests { get; set; }
    public long MaxRequests { get; set; }
    public TimeSpan Period { get; set; }
    public DateTime RetryAfter { get; set; }
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Rate limit metrics
/// </summary>
public class RateLimitMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long BlockedCount { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}

/// <summary>
/// Rate limit violation event
/// </summary>
public class RateLimitViolation
{
    public string ClientId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long Limit { get; set; }
    public TimeSpan Period { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
