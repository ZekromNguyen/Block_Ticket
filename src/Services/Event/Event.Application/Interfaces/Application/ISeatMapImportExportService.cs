using Event.Domain.Models;

namespace Event.Application.Interfaces.Application;

/// <summary>
/// Service for importing and exporting seat maps
/// </summary>
public interface ISeatMapImportExportService
{
    /// <summary>
    /// Import seat map from stream
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="stream">File stream</param>
    /// <param name="format">File format</param>
    /// <param name="options">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result</returns>
    Task<SeatMapImportResult> ImportSeatMapAsync(
        Guid venueId,
        Stream stream,
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import seat map from schema object
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="schema">Seat map schema</param>
    /// <param name="options">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result</returns>
    Task<SeatMapImportResult> ImportSeatMapFromSchemaAsync(
        Guid venueId,
        SeatMapSchema schema,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export seat map to stream
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="format">Export format</param>
    /// <param name="options">Export options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing exported data</returns>
    Task<Stream> ExportSeatMapToStreamAsync(
        Guid venueId,
        SeatMapFormat format,
        SeatMapExportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema
    /// </summary>
    /// <param name="schema">Schema to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<SeatMapValidationResult> ValidateSchemaAsync(
        SeatMapSchema schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported import formats
    /// </summary>
    /// <returns>List of supported formats</returns>
    IReadOnlyList<SeatMapFormat> GetSupportedImportFormats();

    /// <summary>
    /// Get supported export formats
    /// </summary>
    /// <returns>List of supported formats</returns>
    IReadOnlyList<SeatMapFormat> GetSupportedExportFormats();

    /// <summary>
    /// Preview import without applying changes
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="stream">File stream</param>
    /// <param name="format">File format</param>
    /// <param name="options">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import preview</returns>
    Task<SeatMapImportPreview> PreviewImportAsync(
        Guid venueId,
        Stream stream,
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema from stream
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="format">File format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Schema validation result</returns>
    Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        Stream stream,
        SeatMapFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema from object
    /// </summary>
    /// <param name="schema">Schema object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Schema validation result</returns>
    Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        SeatMapSchema schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported formats
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported format info</returns>
    Task<List<SeatMapFormatInfo>> GetSupportedFormatsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate template file
    /// </summary>
    /// <param name="format">Template format</param>
    /// <param name="options">Template options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template stream</returns>
    Task<Stream> GenerateTemplateAsync(
        SeatMapFormat format,
        SeatMapTemplateOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seat map format enumeration
/// </summary>
public enum SeatMapFormat
{
    Json = 0,
    Csv = 1,
    Excel = 2,
    Xml = 3
}

/// <summary>
/// Seat map import options
/// </summary>
public class SeatMapImportOptions
{
    /// <summary>
    /// Whether to validate schema before import
    /// </summary>
    public bool ValidateSchema { get; set; } = true;

    /// <summary>
    /// Whether to replace existing seat map
    /// </summary>
    public bool ReplaceExisting { get; set; } = false;

    /// <summary>
    /// Whether to perform dry run validation only
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to ignore validation warnings
    /// </summary>
    public bool IgnoreWarnings { get; set; } = false;

    /// <summary>
    /// Whether to update existing seats or skip them
    /// </summary>
    public bool UpdateExistingSeats { get; set; } = true;
}

/// <summary>
/// Seat map export options
/// </summary>
public class SeatMapExportOptions
{
    /// <summary>
    /// Include layout information
    /// </summary>
    public bool IncludeLayout { get; set; } = true;

    /// <summary>
    /// Include current seat statuses
    /// </summary>
    public bool IncludeStatuses { get; set; } = false;

    /// <summary>
    /// Include current allocations
    /// </summary>
    public bool IncludeAllocations { get; set; } = false;

    /// <summary>
    /// Include metadata
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Export only specific sections
    /// </summary>
    public List<string>? SectionFilter { get; set; }

    /// <summary>
    /// Export only specific rows
    /// </summary>
    public List<string>? RowFilter { get; set; }
}

/// <summary>
/// Seat map import result
/// </summary>
public class SeatMapImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Import identifier
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Number of seats processed
    /// </summary>
    public int SeatsProcessed { get; set; }

    /// <summary>
    /// Number of seats created
    /// </summary>
    public int SeatsCreated { get; set; }

    /// <summary>
    /// Number of seats updated
    /// </summary>
    public int SeatsUpdated { get; set; }

    /// <summary>
    /// Number of seats skipped
    /// </summary>
    public int SeatsSkipped { get; set; }

    /// <summary>
    /// Processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Processing messages
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Summary of changes
    /// </summary>
    public SeatMapChangeSummary? Changes { get; set; }
}

/// <summary>
/// Seat map validation result
/// </summary>
public class SeatMapValidationResult
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

/// <summary>
/// Summary of changes made during import
/// </summary>
public class SeatMapChangeSummary
{
    /// <summary>
    /// Sections added
    /// </summary>
    public List<string> SectionsAdded { get; set; } = new();

    /// <summary>
    /// Sections modified
    /// </summary>
    public List<string> SectionsModified { get; set; } = new();

    /// <summary>
    /// Sections removed
    /// </summary>
    public List<string> SectionsRemoved { get; set; } = new();

    /// <summary>
    /// Total seat capacity before import
    /// </summary>
    public int CapacityBefore { get; set; }

    /// <summary>
    /// Total seat capacity after import
    /// </summary>
    public int CapacityAfter { get; set; }

    /// <summary>
    /// Net change in capacity
    /// </summary>
    public int CapacityChange => CapacityAfter - CapacityBefore;
}

/// <summary>
/// Seat map statistics
/// </summary>
public class SeatMapStatistics
{
    /// <summary>
    /// Total number of seats
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Number of sections
    /// </summary>
    public int SectionCount { get; set; }

    /// <summary>
    /// Number of rows
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Seats by section
    /// </summary>
    public Dictionary<string, int> SeatsBySection { get; set; } = new();

    /// <summary>
    /// Seats by type
    /// </summary>
    public Dictionary<string, int> SeatsByType { get; set; } = new();
}

/// <summary>
/// Seat map import preview result
/// </summary>
public class SeatMapImportPreview
{
    /// <summary>
    /// Whether the preview was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Preview of seats that would be imported
    /// </summary>
    public List<SeatPreview> SeatPreviews { get; set; } = new();

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Preview statistics
    /// </summary>
    public SeatMapStatistics? Statistics { get; set; }

    /// <summary>
    /// Estimated processing time
    /// </summary>
    public TimeSpan EstimatedProcessingTime { get; set; }
}

/// <summary>
/// Individual seat preview for import
/// </summary>
public class SeatPreview
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public string SeatId { get; set; } = string.Empty;

    /// <summary>
    /// Section name
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Row identifier
    /// </summary>
    public string Row { get; set; } = string.Empty;

    /// <summary>
    /// Seat number
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Action to be performed
    /// </summary>
    public SeatPreviewAction Action { get; set; }

    /// <summary>
    /// Current seat status
    /// </summary>
    public string? CurrentStatus { get; set; }

    /// <summary>
    /// New seat status
    /// </summary>
    public string? NewStatus { get; set; }
}

/// <summary>
/// Action to be performed on seat during import
/// </summary>
public enum SeatPreviewAction
{
    Create,
    Update,
    Skip,
    Error
}

/// <summary>
/// Seat map format information
/// </summary>
public class SeatMapFormatInfo
{
    /// <summary>
    /// Format type
    /// </summary>
    public SeatMapFormat Format { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Whether format supports import
    /// </summary>
    public bool SupportsImport { get; set; }

    /// <summary>
    /// Whether format supports export
    /// </summary>
    public bool SupportsExport { get; set; }

    /// <summary>
    /// Maximum file size for this format
    /// </summary>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>
    /// Format description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Template generation options
/// </summary>
public class SeatMapTemplateOptions
{
    /// <summary>
    /// Include example data
    /// </summary>
    public bool IncludeExampleData { get; set; } = true;

    /// <summary>
    /// Include column headers
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Include documentation/comments
    /// </summary>
    public bool IncludeDocumentation { get; set; } = true;

    /// <summary>
    /// Template language/locale
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Venue type for template customization
    /// </summary>
    public string? VenueType { get; set; }

    /// <summary>
    /// Number of example sections to include
    /// </summary>
    public int ExampleSectionCount { get; set; } = 3;

    /// <summary>
    /// Number of example rows per section
    /// </summary>
    public int ExampleRowsPerSection { get; set; } = 10;

    /// <summary>
    /// Number of example seats per row
    /// </summary>
    public int ExampleSeatsPerRow { get; set; } = 20;
}
