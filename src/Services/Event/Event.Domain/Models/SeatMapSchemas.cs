using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Event.Domain.Models;

/// <summary>
/// Complete seat map schema for import/export operations
/// </summary>
public class SeatMapSchema
{
    /// <summary>
    /// Schema version for compatibility checking
    /// </summary>
    [Required]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Venue information
    /// </summary>
    [Required]
    [JsonPropertyName("venue")]
    public VenueSchemaInfo Venue { get; set; } = null!;

    /// <summary>
    /// Seat map metadata
    /// </summary>
    [Required]
    [JsonPropertyName("metadata")]
    public SeatMapMetadataSchema Metadata { get; set; } = null!;

    /// <summary>
    /// All sections in the venue
    /// </summary>
    [Required]
    [JsonPropertyName("sections")]
    public List<SectionSchema> Sections { get; set; } = new();

    /// <summary>
    /// Visual layout information (optional)
    /// </summary>
    [JsonPropertyName("layout")]
    public VenueLayoutSchema? Layout { get; set; }

    /// <summary>
    /// Accessibility features and information
    /// </summary>
    [JsonPropertyName("accessibility")]
    public AccessibilitySchema? Accessibility { get; set; }

    /// <summary>
    /// Pricing category definitions
    /// </summary>
    [JsonPropertyName("price_categories")]
    public List<PriceCategorySchema> PriceCategories { get; set; } = new();
}

/// <summary>
/// Venue information in seat map schema
/// </summary>
public class VenueSchemaInfo
{
    /// <summary>
    /// Venue identifier (optional for import, required for export)
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Venue name
    /// </summary>
    [Required]
    [MaxLength(200)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Venue description
    /// </summary>
    [MaxLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Venue address
    /// </summary>
    [JsonPropertyName("address")]
    public AddressSchema? Address { get; set; }

    /// <summary>
    /// Venue timezone
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? TimeZone { get; set; }
}

/// <summary>
/// Address schema for venue
/// </summary>
public class AddressSchema
{
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}

/// <summary>
/// Seat map metadata
/// </summary>
public class SeatMapMetadataSchema
{
    /// <summary>
    /// When the seat map was created/exported
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification time
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of seats
    /// </summary>
    [JsonPropertyName("total_seats")]
    public int TotalSeats { get; set; }

    /// <summary>
    /// Number of sections
    /// </summary>
    [JsonPropertyName("total_sections")]
    public int TotalSections { get; set; }

    /// <summary>
    /// Seat map checksum for integrity verification
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    /// <summary>
    /// Validation status
    /// </summary>
    [JsonPropertyName("validation_status")]
    public string ValidationStatus { get; set; } = "pending";

    /// <summary>
    /// Source system or format
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    [JsonPropertyName("custom_fields")]
    public Dictionary<string, object>? CustomFields { get; set; }
}

/// <summary>
/// Section schema
/// </summary>
public class SectionSchema
{
    /// <summary>
    /// Section identifier/name
    /// </summary>
    [Required]
    [MaxLength(20)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Section display name
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Section type (e.g., Orchestra, Balcony, Field, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Section capacity
    /// </summary>
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    /// <summary>
    /// Default price category for this section
    /// </summary>
    [JsonPropertyName("default_price_category")]
    public string? DefaultPriceCategory { get; set; }

    /// <summary>
    /// Accessibility features for this section
    /// </summary>
    [JsonPropertyName("accessibility_features")]
    public List<string> AccessibilityFeatures { get; set; } = new();

    /// <summary>
    /// All rows in this section
    /// </summary>
    [Required]
    [JsonPropertyName("rows")]
    public List<RowSchema> Rows { get; set; } = new();

    /// <summary>
    /// Visual position/coordinates for layout
    /// </summary>
    [JsonPropertyName("layout_position")]
    public PositionSchema? LayoutPosition { get; set; }
}

/// <summary>
/// Row schema
/// </summary>
public class RowSchema
{
    /// <summary>
    /// Row identifier/name
    /// </summary>
    [Required]
    [MaxLength(10)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Row display name
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Row capacity
    /// </summary>
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    /// <summary>
    /// Row curve information for visual layout
    /// </summary>
    [JsonPropertyName("curve")]
    public double? Curve { get; set; }

    /// <summary>
    /// Skew angle for visual layout
    /// </summary>
    [JsonPropertyName("skew")]
    public double? Skew { get; set; }

    /// <summary>
    /// All seats in this row
    /// </summary>
    [Required]
    [JsonPropertyName("seats")]
    public List<SeatSchema> Seats { get; set; } = new();

    /// <summary>
    /// Visual position for layout
    /// </summary>
    [JsonPropertyName("layout_position")]
    public PositionSchema? LayoutPosition { get; set; }
}

/// <summary>
/// Individual seat schema
/// </summary>
public class SeatSchema
{
    /// <summary>
    /// Seat number/identifier
    /// </summary>
    [Required]
    [MaxLength(10)]
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Seat display name
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether this seat is accessible
    /// </summary>
    [JsonPropertyName("is_accessible")]
    public bool IsAccessible { get; set; }

    /// <summary>
    /// Whether this seat has restricted view
    /// </summary>
    [JsonPropertyName("has_restricted_view")]
    public bool HasRestrictedView { get; set; }

    /// <summary>
    /// Price category for this seat
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("price_category")]
    public string? PriceCategory { get; set; }

    /// <summary>
    /// Additional notes about this seat
    /// </summary>
    [MaxLength(500)]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Seat type (Standard, Premium, Companion, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Visual position for layout
    /// </summary>
    [JsonPropertyName("layout_position")]
    public PositionSchema? LayoutPosition { get; set; }

    /// <summary>
    /// Seat attributes (e.g., isle, obstructed, etc.)
    /// </summary>
    [JsonPropertyName("attributes")]
    public List<string> Attributes { get; set; } = new();

    /// <summary>
    /// Status for import (Available, Blocked, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Available";
}

/// <summary>
/// Position schema for visual layout
/// </summary>
public class PositionSchema
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double? Z { get; set; }

    [JsonPropertyName("rotation")]
    public double? Rotation { get; set; }

    [JsonPropertyName("scale")]
    public double? Scale { get; set; }
}

/// <summary>
/// Venue layout schema for visual representation
/// </summary>
public class VenueLayoutSchema
{
    /// <summary>
    /// Layout dimensions
    /// </summary>
    [JsonPropertyName("dimensions")]
    public DimensionsSchema? Dimensions { get; set; }

    /// <summary>
    /// Stage/performance area position
    /// </summary>
    [JsonPropertyName("stage_position")]
    public PositionSchema? StagePosition { get; set; }

    /// <summary>
    /// Entry/exit points
    /// </summary>
    [JsonPropertyName("entry_points")]
    public List<PositionSchema> EntryPoints { get; set; } = new();

    /// <summary>
    /// Emergency exits
    /// </summary>
    [JsonPropertyName("emergency_exits")]
    public List<PositionSchema> EmergencyExits { get; set; } = new();

    /// <summary>
    /// Concession areas
    /// </summary>
    [JsonPropertyName("concession_areas")]
    public List<AreaSchema> ConcessionAreas { get; set; } = new();

    /// <summary>
    /// Restroom locations
    /// </summary>
    [JsonPropertyName("restrooms")]
    public List<PositionSchema> Restrooms { get; set; } = new();
}

/// <summary>
/// Dimensions schema
/// </summary>
public class DimensionsSchema
{
    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("depth")]
    public double? Depth { get; set; }
}

/// <summary>
/// Area schema for layout elements
/// </summary>
public class AreaSchema
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("position")]
    public PositionSchema? Position { get; set; }

    [JsonPropertyName("dimensions")]
    public DimensionsSchema? Dimensions { get; set; }
}

/// <summary>
/// Accessibility schema
/// </summary>
public class AccessibilitySchema
{
    /// <summary>
    /// Total accessible seats
    /// </summary>
    [JsonPropertyName("total_accessible_seats")]
    public int TotalAccessibleSeats { get; set; }

    /// <summary>
    /// Companion seats available
    /// </summary>
    [JsonPropertyName("companion_seats_available")]
    public bool CompanionSeatsAvailable { get; set; }

    /// <summary>
    /// Wheelchair accessible areas
    /// </summary>
    [JsonPropertyName("wheelchair_areas")]
    public List<string> WheelchairAreas { get; set; } = new();

    /// <summary>
    /// Assisted listening devices available
    /// </summary>
    [JsonPropertyName("assisted_listening")]
    public bool AssistedListening { get; set; }

    /// <summary>
    /// Sign language interpretation areas
    /// </summary>
    [JsonPropertyName("sign_language_areas")]
    public List<string> SignLanguageAreas { get; set; } = new();

    /// <summary>
    /// Other accessibility features
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// Price category schema
/// </summary>
public class PriceCategorySchema
{
    /// <summary>
    /// Category identifier
    /// </summary>
    [Required]
    [MaxLength(50)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    [MaxLength(500)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Base price for this category
    /// </summary>
    [JsonPropertyName("base_price")]
    public decimal? BasePrice { get; set; }

    /// <summary>
    /// Color for visual display
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

/// <summary>
/// Schema validation result
/// </summary>
public class SchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public SchemaValidationMetrics? Metrics { get; set; }
}

/// <summary>
/// Schema validation metrics
/// </summary>
public class SchemaValidationMetrics
{
    public int TotalSections { get; set; }
    public int TotalRows { get; set; }
    public int TotalSeats { get; set; }
    public int AccessibleSeats { get; set; }
    public int RestrictedViewSeats { get; set; }
    public Dictionary<string, int> SeatsByCategory { get; set; } = new();
    public Dictionary<string, int> SeatsBySection { get; set; } = new();
    public List<string> DuplicatePositions { get; set; } = new();
    public List<string> MissingRequiredFields { get; set; } = new();
}

/// <summary>
/// Format types for import/export
/// </summary>
public enum SeatMapFormat
{
    Json,
    Csv,
    Excel,
    Xml,
    Custom
}

/// <summary>
/// Import/export operation types
/// </summary>
public enum SeatMapOperation
{
    Import,
    Export,
    Validate,
    Preview,
    Merge
}
