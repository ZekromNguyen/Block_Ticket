namespace Event.Application.Interfaces.Application;

/// <summary>
/// Represents a seat map schema
/// </summary>
public class SeatMapSchema
{
    /// <summary>
    /// Schema version
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Venue identifier
    /// </summary>
    public Guid? VenueId { get; set; }

    /// <summary>
    /// Venue name
    /// </summary>
    public string? VenueName { get; set; }

    /// <summary>
    /// Sections in the seat map
    /// </summary>
    public List<SeatMapSection> Sections { get; set; } = new();

    /// <summary>
    /// Schema metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Section in the seat map schema
/// </summary>
public class SeatMapSection
{
    /// <summary>
    /// Section identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Section display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Section description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Section capacity
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Section type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Rows in this section
    /// </summary>
    public List<SeatMapRow> Rows { get; set; } = new();

    /// <summary>
    /// Section properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Row in the seat map schema
/// </summary>
public class SeatMapRow
{
    /// <summary>
    /// Row identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Row display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Row description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Row capacity
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Seats in this row
    /// </summary>
    public List<SeatMapSeat> Seats { get; set; } = new();

    /// <summary>
    /// Row properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Seat in the seat map schema
/// </summary>
public class SeatMapSeat
{
    /// <summary>
    /// Seat number
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Seat display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Seat type
    /// </summary>
    public string Type { get; set; } = "Standard";

    /// <summary>
    /// Seat status
    /// </summary>
    public string Status { get; set; } = "Available";

    /// <summary>
    /// Seat position
    /// </summary>
    public SeatPosition? Position { get; set; }

    /// <summary>
    /// Price tier
    /// </summary>
    public string? PriceTier { get; set; }

    /// <summary>
    /// Accessibility features
    /// </summary>
    public List<string>? AccessibilityFeatures { get; set; }

    /// <summary>
    /// Seat properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Schema validation result
/// </summary>
public class SchemaValidationResult
{
    /// <summary>
    /// Whether the schema is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Schema statistics
    /// </summary>
    public SeatMapStatistics? Statistics { get; set; }
}
