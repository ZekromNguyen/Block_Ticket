using Event.Domain.ValueObjects;

namespace Event.Domain.Models;

/// <summary>
/// Represents a time window when tickets are available for sale
/// </summary>
public class OnSaleWindow
{
    /// <summary>
    /// The date and time range when tickets are on sale
    /// </summary>
    public DateTimeRange TimeRange { get; private set; }

    /// <summary>
    /// Optional label for this sale window (e.g., "Early Bird", "General Sale")
    /// </summary>
    public string? Label { get; private set; }

    /// <summary>
    /// Whether this sale window is currently active
    /// </summary>
    public bool IsActive => TimeRange.Contains(DateTime.UtcNow);

    /// <summary>
    /// Alternative name for IsActive for backward compatibility
    /// </summary>
    public bool IsActiveNow() => IsActive;

    /// <summary>
    /// Creates a new on-sale window
    /// </summary>
    /// <param name="saleWindow">The date and time range for the sale</param>
    /// <param name="label">Optional label for this sale window</param>
    public OnSaleWindow(DateTimeRange saleWindow, string? label = null)
    {
        TimeRange = saleWindow ?? throw new ArgumentNullException(nameof(saleWindow));
        Label = label;
    }

    /// <summary>
    /// Creates a new on-sale window from an existing DateTimeRange
    /// </summary>
    /// <param name="dateTimeRange">The source DateTimeRange</param>
    public OnSaleWindow(DateTimeRange dateTimeRange) : this(dateTimeRange, null)
    {
    }

    /// <summary>
    /// Checks if this sale window overlaps with another
    /// </summary>
    /// <param name="other">The other sale window to check</param>
    /// <returns>True if the windows overlap</returns>
    public bool OverlapsWith(OnSaleWindow other)
    {
        if (other == null) return false;
        return TimeRange.Overlaps(other.TimeRange);
    }

    /// <summary>
    /// Checks if this sale window contains the specified date
    /// </summary>
    /// <param name="dateTime">The date to check</param>
    /// <returns>True if the date is within this sale window</returns>
    public bool Contains(DateTime dateTime)
    {
        return TimeRange.Contains(dateTime);
    }

    /// <summary>
    /// Gets the start date of this sale window
    /// </summary>
    public DateTime StartDate => TimeRange.StartDate;

    /// <summary>
    /// Gets the end date of this sale window
    /// </summary>
    public DateTime EndDate => TimeRange.EndDate;

    /// <summary>
    /// Gets the time zone of this sale window
    /// </summary>
    public TimeZoneId TimeZone => TimeRange.TimeZone;

    public override string ToString()
    {
        var labelPart = !string.IsNullOrEmpty(Label) ? $"{Label}: " : "";
        return $"{labelPart}{TimeRange}";
    }

    public override bool Equals(object? obj)
    {
        return obj is OnSaleWindow other && 
               TimeRange.Equals(other.TimeRange) && 
               Label == other.Label;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TimeRange, Label);
    }
}
