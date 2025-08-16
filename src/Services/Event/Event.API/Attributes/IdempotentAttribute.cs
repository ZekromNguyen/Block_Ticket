namespace Event.API.Attributes;

/// <summary>
/// Attribute to mark an action as requiring idempotency
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IdempotentAttribute : Attribute
{
    /// <summary>
    /// Time-to-live for the idempotency record in hours (default: 24)
    /// </summary>
    public double TTLHours { get; set; } = 24;

    /// <summary>
    /// Whether to require an idempotency key (default: true)
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Custom error message when idempotency key is missing
    /// </summary>
    public string? MissingKeyMessage { get; set; }

    /// <summary>
    /// Whether to auto-generate an idempotency key if missing (default: false)
    /// </summary>
    public bool AutoGenerate { get; set; } = false;

    public IdempotentAttribute()
    {
    }

    public IdempotentAttribute(double ttlHours)
    {
        TTLHours = ttlHours;
    }

    /// <summary>
    /// Gets the TTL as a TimeSpan
    /// </summary>
    public TimeSpan GetTTL() => TimeSpan.FromHours(TTLHours);
}

/// <summary>
/// Attribute to explicitly exclude an action from idempotency checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class NoIdempotencyAttribute : Attribute
{
}
