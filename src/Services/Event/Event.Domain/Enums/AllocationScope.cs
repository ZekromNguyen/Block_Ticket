namespace Event.Domain.Enums;

/// <summary>
/// Defines the scope of an allocation, whether it's a general quantity or specific seats.
/// </summary>
public enum AllocationScope
{
    /// <summary>
    /// The allocation is a pool of tickets defined by a quantity.
    /// </summary>
    ByQuantity = 0,

    /// <summary>
    /// The allocation is for a specific, enumerated list of seats.
    /// </summary>
    BySeat = 1
}

