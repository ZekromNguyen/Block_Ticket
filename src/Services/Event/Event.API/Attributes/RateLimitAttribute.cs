namespace Event.API.Attributes;

/// <summary>
/// Attribute to specify custom rate limiting for an endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Maximum requests per period
    /// </summary>
    public long Limit { get; set; } = 100;

    /// <summary>
    /// Time period in string format (e.g., "1m", "1h", "1d")
    /// </summary>
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Whether to apply rate limiting per IP address
    /// </summary>
    public bool PerIP { get; set; } = false;

    /// <summary>
    /// Whether to apply rate limiting per client ID
    /// </summary>
    public bool PerClient { get; set; } = true;

    /// <summary>
    /// Whether to apply rate limiting per organization
    /// </summary>
    public bool PerOrganization { get; set; } = false;

    /// <summary>
    /// Custom error message when rate limit is exceeded
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Priority of this rate limit rule (higher = more important)
    /// </summary>
    public int Priority { get; set; } = 0;

    public RateLimitAttribute()
    {
    }

    public RateLimitAttribute(long limit, string period = "1m")
    {
        Limit = limit;
        Period = period;
    }
}

/// <summary>
/// Attribute to bypass rate limiting for an endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class NoRateLimitAttribute : Attribute
{
}

/// <summary>
/// Attribute for burst protection - allows short bursts but enforces strict limits over longer periods
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class BurstProtectionAttribute : Attribute
{
    /// <summary>
    /// Maximum burst requests allowed
    /// </summary>
    public long BurstLimit { get; set; } = 10;

    /// <summary>
    /// Burst period (short term)
    /// </summary>
    public string BurstPeriod { get; set; } = "10s";

    /// <summary>
    /// Sustained limit over longer period
    /// </summary>
    public long SustainedLimit { get; set; } = 100;

    /// <summary>
    /// Sustained period (long term)
    /// </summary>
    public string SustainedPeriod { get; set; } = "1h";

    public BurstProtectionAttribute()
    {
    }

    public BurstProtectionAttribute(long burstLimit, string burstPeriod, long sustainedLimit, string sustainedPeriod)
    {
        BurstLimit = burstLimit;
        BurstPeriod = burstPeriod;
        SustainedLimit = sustainedLimit;
        SustainedPeriod = sustainedPeriod;
    }
}

/// <summary>
/// Attribute for progressive rate limiting - increases restrictions for repeated violations
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ProgressiveRateLimitAttribute : Attribute
{
    /// <summary>
    /// Base limit for first-time users
    /// </summary>
    public long BaseLimit { get; set; } = 100;

    /// <summary>
    /// Limit after first violation
    /// </summary>
    public long FirstViolationLimit { get; set; } = 50;

    /// <summary>
    /// Limit after repeated violations
    /// </summary>
    public long RepeatedViolationLimit { get; set; } = 10;

    /// <summary>
    /// Period for rate limiting
    /// </summary>
    public string Period { get; set; } = "1h";

    /// <summary>
    /// How long the progressive penalty lasts
    /// </summary>
    public string PenaltyDuration { get; set; } = "24h";

    public ProgressiveRateLimitAttribute()
    {
    }

    public ProgressiveRateLimitAttribute(long baseLimit, long firstViolationLimit, long repeatedViolationLimit)
    {
        BaseLimit = baseLimit;
        FirstViolationLimit = firstViolationLimit;
        RepeatedViolationLimit = repeatedViolationLimit;
    }
}
