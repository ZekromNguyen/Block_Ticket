namespace Event.Domain.Enums;

/// <summary>
/// Specifies the consistency mode for data retrieval.
/// </summary>
public enum ConsistencyMode
{
    /// <summary>
    /// The latest, most up-to-date data is required.
    /// </summary>
    Consistent,

    /// <summary>
    /// Stale data from a cache is acceptable.
    /// </summary>
    Stale
}

