using Event.Domain.ValueObjects;

namespace Event.Domain.Models;

/// <summary>
/// Represents a time window when an event can be published and made visible
/// </summary>
public class PublishWindow
{
    /// <summary>
    /// The date and time range when the event is published
    /// </summary>
    public DateTimeRange PublishRange { get; private set; }

    /// <summary>
    /// Optional description of this publish window
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this publish window is currently active
    /// </summary>
    public bool IsActive => PublishRange.Contains(DateTime.UtcNow);

    /// <summary>
    /// Creates a new publish window
    /// </summary>
    /// <param name="publishRange">The date and time range for publishing</param>
    /// <param name="description">Optional description</param>
    public PublishWindow(DateTimeRange publishRange, string? description = null)
    {
        PublishRange = publishRange ?? throw new ArgumentNullException(nameof(publishRange));
        Description = description;
    }

    /// <summary>
    /// Creates a new publish window from an existing DateTimeRange
    /// </summary>
    /// <param name="dateTimeRange">The source DateTimeRange</param>
    public PublishWindow(DateTimeRange dateTimeRange) : this(dateTimeRange, null)
    {
    }

    /// <summary>
    /// Checks if this publish window contains the specified date
    /// </summary>
    /// <param name="dateTime">The date to check</param>
    /// <returns>True if the date is within this publish window</returns>
    public bool Contains(DateTime dateTime)
    {
        return PublishRange.Contains(dateTime);
    }

    /// <summary>
    /// Checks if this publish window overlaps with another
    /// </summary>
    /// <param name="other">The other publish window to check</param>
    /// <returns>True if the windows overlap</returns>
    public bool OverlapsWith(PublishWindow other)
    {
        if (other == null) return false;
        return PublishRange.Overlaps(other.PublishRange);
    }

    /// <summary>
    /// Gets the start date of this publish window
    /// </summary>
    public DateTime StartDate => PublishRange.StartDate;

    /// <summary>
    /// Gets the end date of this publish window
    /// </summary>
    public DateTime EndDate => PublishRange.EndDate;

    /// <summary>
    /// Gets the time zone of this publish window
    /// </summary>
    public TimeZoneId TimeZone => PublishRange.TimeZone;

    public override string ToString()
    {
        var descPart = !string.IsNullOrEmpty(Description) ? $"{Description}: " : "";
        return $"{descPart}{PublishRange}";
    }

    public override bool Equals(object? obj)
    {
        return obj is PublishWindow other && 
               PublishRange.Equals(other.PublishRange) && 
               Description == other.Description;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PublishRange, Description);
    }
}
