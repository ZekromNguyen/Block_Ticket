using Event.Domain.Models;
using System.IO;

namespace Event.Domain.Services;

/// <summary>
/// Service for seat map import/export operations with schema validation
/// </summary>
public interface ISeatMapImportExportService
{
    /// <summary>
    /// Import seat map from various formats
    /// </summary>
    Task<SeatMapImportResult> ImportSeatMapAsync(
        Guid venueId, 
        Stream dataStream, 
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import seat map from schema object
    /// </summary>
    Task<SeatMapImportResult> ImportSeatMapFromSchemaAsync(
        Guid venueId,
        SeatMapSchema seatMapSchema,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export seat map to various formats
    /// </summary>
    Task<SeatMapExportResult> ExportSeatMapAsync(
        Guid venueId,
        SeatMapFormat format,
        SeatMapExportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export seat map to stream
    /// </summary>
    Task<Stream> ExportSeatMapToStreamAsync(
        Guid venueId,
        SeatMapFormat format,
        SeatMapExportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema without importing
    /// </summary>
    Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        Stream dataStream,
        SeatMapFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema object
    /// </summary>
    Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        SeatMapSchema seatMapSchema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview import results without applying changes
    /// </summary>
    Task<SeatMapImportPreview> PreviewImportAsync(
        Guid venueId,
        Stream dataStream,
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported formats and their capabilities
    /// </summary>
    Task<List<SeatMapFormatInfo>> GetSupportedFormatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate seat map template for a given format
    /// </summary>
    Task<Stream> GenerateTemplateAsync(
        SeatMapFormat format,
        SeatMapTemplateOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk import multiple seat maps
    /// </summary>
    Task<BulkSeatMapImportResult> BulkImportSeatMapsAsync(
        List<BulkSeatMapImportItem> imports,
        BulkImportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for seat map bulk operations
/// </summary>
public interface ISeatMapBulkOperationsService
{
    /// <summary>
    /// Perform bulk seat operations (block, unblock, allocate, etc.)
    /// </summary>
    Task<BulkSeatOperationResult> PerformBulkSeatOperationAsync(
        Guid venueId,
        BulkSeatOperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update seat attributes
    /// </summary>
    Task<BulkSeatUpdateResult> BulkUpdateSeatAttributesAsync(
        Guid venueId,
        BulkSeatAttributeUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy seat map from one venue to another
    /// </summary>
    Task<SeatMapCopyResult> CopySeatMapAsync(
        Guid sourceVenueId,
        Guid targetVenueId,
        SeatMapCopyOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge seat maps (for venue modifications)
    /// </summary>
    Task<SeatMapMergeResult> MergeSeatMapsAsync(
        Guid venueId,
        SeatMapSchema newSeatMap,
        SeatMapMergeOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive current seat map and create new version
    /// </summary>
    Task<SeatMapVersioningResult> CreateSeatMapVersionAsync(
        Guid venueId,
        string versionNote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore seat map from archived version
    /// </summary>
    Task<SeatMapRestoreResult> RestoreSeatMapVersionAsync(
        Guid venueId,
        Guid versionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Import options
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
    public bool ReplaceExisting { get; set; } = true;

    /// <summary>
    /// Whether to preserve existing seat statuses
    /// </summary>
    public bool PreserveStatuses { get; set; } = false;

    /// <summary>
    /// Whether to preserve existing seat allocations
    /// </summary>
    public bool PreserveAllocations { get; set; } = false;

    /// <summary>
    /// Maximum allowed errors before aborting import
    /// </summary>
    public int MaxErrorsAllowed { get; set; } = 100;

    /// <summary>
    /// Import mode (Insert, Update, Upsert)
    /// </summary>
    public ImportMode Mode { get; set; } = ImportMode.Upsert;

    /// <summary>
    /// Whether to perform dry run validation only
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Custom field mappings for CSV/Excel imports
    /// </summary>
    public Dictionary<string, string> FieldMappings { get; set; } = new();

    /// <summary>
    /// Default values for optional fields
    /// </summary>
    public Dictionary<string, object> DefaultValues { get; set; } = new();
}

/// <summary>
/// Export options
/// </summary>
public class SeatMapExportOptions
{
    /// <summary>
    /// Include visual layout information
    /// </summary>
    public bool IncludeLayout { get; set; } = true;

    /// <summary>
    /// Include accessibility information
    /// </summary>
    public bool IncludeAccessibility { get; set; } = true;

    /// <summary>
    /// Include current seat statuses
    /// </summary>
    public bool IncludeStatuses { get; set; } = false;

    /// <summary>
    /// Include current allocations
    /// </summary>
    public bool IncludeAllocations { get; set; } = false;

    /// <summary>
    /// Include pricing information
    /// </summary>
    public bool IncludePricing { get; set; } = true;

    /// <summary>
    /// Format specific options
    /// </summary>
    public Dictionary<string, object> FormatOptions { get; set; } = new();

    /// <summary>
    /// Filter by sections
    /// </summary>
    public List<string> SectionFilter { get; set; } = new();

    /// <summary>
    /// Filter by price categories
    /// </summary>
    public List<string> PriceCategoryFilter { get; set; } = new();

    /// <summary>
    /// Compression options for large exports
    /// </summary>
    public CompressionOptions? Compression { get; set; }
}

/// <summary>
/// Template generation options
/// </summary>
public class SeatMapTemplateOptions
{
    /// <summary>
    /// Include sample data
    /// </summary>
    public bool IncludeSampleData { get; set; } = true;

    /// <summary>
    /// Template size (Small, Medium, Large)
    /// </summary>
    public string TemplateSize { get; set; } = "Medium";

    /// <summary>
    /// Venue type (Theater, Stadium, Arena, etc.)
    /// </summary>
    public string? VenueType { get; set; }

    /// <summary>
    /// Include documentation/help
    /// </summary>
    public bool IncludeDocumentation { get; set; } = true;
}

/// <summary>
/// Import preview result
/// </summary>
public class SeatMapImportPreview
{
    public bool IsValid { get; set; }
    public int TotalSeatsToImport { get; set; }
    public int TotalSeatsToUpdate { get; set; }
    public int TotalSeatsToAdd { get; set; }
    public int TotalSeatsToRemove { get; set; }
    public List<string> Changes { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public SchemaValidationResult ValidationResult { get; set; } = null!;
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// Format information
/// </summary>
public class SeatMapFormatInfo
{
    public SeatMapFormat Format { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> FileExtensions { get; set; } = new();
    public bool SupportsImport { get; set; }
    public bool SupportsExport { get; set; }
    public bool SupportsValidation { get; set; }
    public bool SupportsLayout { get; set; }
    public int MaxFileSize { get; set; }
    public Dictionary<string, object> Capabilities { get; set; } = new();
}

/// <summary>
/// Bulk import item
/// </summary>
public class BulkSeatMapImportItem
{
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public Stream DataStream { get; set; } = null!;
    public SeatMapFormat Format { get; set; }
    public SeatMapImportOptions Options { get; set; } = new();
}

/// <summary>
/// Bulk import result
/// </summary>
public class BulkSeatMapImportResult
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkSeatMapImportItemResult> Results { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
}

/// <summary>
/// Bulk import item result
/// </summary>
public class BulkSeatMapImportItemResult
{
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public SeatMapImportResult? ImportResult { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Bulk operation options
/// </summary>
public class BulkImportOptions
{
    public bool ContinueOnError { get; set; } = true;
    public int MaxConcurrentImports { get; set; } = 3;
    public bool ValidateAllFirst { get; set; } = true;
    public bool CreateBackups { get; set; } = true;
    public string? NotificationEmail { get; set; }
}

/// <summary>
/// Bulk seat attribute update request
/// </summary>
public class BulkSeatAttributeUpdateRequest
{
    public List<Guid> SeatIds { get; set; } = new();
    public Dictionary<string, object> Updates { get; set; } = new();
    public string? Reason { get; set; }
    public bool ValidateChanges { get; set; } = true;
}

/// <summary>
/// Bulk seat update result
/// </summary>
public class BulkSeatUpdateResult
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkSeatUpdateItemResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Bulk seat update item result
/// </summary>
public class BulkSeatUpdateItemResult
{
    public Guid SeatId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ChangedFields { get; set; }
}

/// <summary>
/// Seat map copy options
/// </summary>
public class SeatMapCopyOptions
{
    public bool CopyLayout { get; set; } = true;
    public bool CopyPriceCategories { get; set; } = true;
    public bool CopyAccessibilityInfo { get; set; } = true;
    public bool ReplaceExisting { get; set; } = true;
    public Dictionary<string, string> SectionMappings { get; set; } = new();
    public Dictionary<string, string> PriceCategoryMappings { get; set; } = new();
}

/// <summary>
/// Seat map copy result
/// </summary>
public class SeatMapCopyResult
{
    public bool Success { get; set; }
    public int CopiedSeats { get; set; }
    public int CopiedSections { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? NewChecksum { get; set; }
}

/// <summary>
/// Seat map merge options
/// </summary>
public class SeatMapMergeOptions
{
    public MergeStrategy Strategy { get; set; } = MergeStrategy.Additive;
    public bool PreserveExistingStatuses { get; set; } = true;
    public bool PreserveExistingAllocations { get; set; } = true;
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.UseNew;
}

/// <summary>
/// Seat map merge result
/// </summary>
public class SeatMapMergeResult
{
    public bool Success { get; set; }
    public int AddedSeats { get; set; }
    public int UpdatedSeats { get; set; }
    public int RemovedSeats { get; set; }
    public int ConflictsResolved { get; set; }
    public List<string> Changes { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Seat map versioning result
/// </summary>
public class SeatMapVersioningResult
{
    public bool Success { get; set; }
    public Guid VersionId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionNote { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ArchivedSeats { get; set; }
}

/// <summary>
/// Seat map restore result
/// </summary>
public class SeatMapRestoreResult
{
    public bool Success { get; set; }
    public Guid RestoredVersionId { get; set; }
    public int RestoredSeats { get; set; }
    public DateTime RestoredAt { get; set; }
    public List<string> Changes { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Compression options
/// </summary>
public class CompressionOptions
{
    public string Format { get; set; } = "gzip";
    public int CompressionLevel { get; set; } = 6;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Import modes
/// </summary>
public enum ImportMode
{
    Insert,      // Only add new seats
    Update,      // Only update existing seats
    Upsert       // Add new and update existing
}

/// <summary>
/// Merge strategies
/// </summary>
public enum MergeStrategy
{
    Additive,    // Add new seats, keep existing
    Replacement, // Replace matching seats
    Hybrid       // Smart merge based on rules
}

/// <summary>
/// Conflict resolution strategies
/// </summary>
public enum ConflictResolution
{
    UseExisting, // Keep existing data
    UseNew,      // Use imported data
    Merge,       // Merge both
    Skip,        // Skip conflicting items
    Fail         // Fail on conflicts
}

/// <summary>
/// Seat map import result
/// </summary>
public class SeatMapImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int ImportedSeats { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? Checksum { get; set; }
}

/// <summary>
/// Seat map export result
/// </summary>
public class SeatMapExportResult
{
    public List<SeatMapRowDto> SeatMapData { get; set; } = new();
    public int TotalSeats { get; set; }
    public List<string> Sections { get; set; } = new();
    public string? Checksum { get; set; }
    public DateTime ExportedAt { get; set; }
}

/// <summary>
/// Bulk seat operation request
/// </summary>
public class BulkSeatOperationRequest
{
    public List<Guid> SeatIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // Block, Unblock, Allocate, Deallocate
    public object? OperationData { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Bulk seat operation result
/// </summary>
public class BulkSeatOperationResult
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkSeatOperationItemResult> Results { get; set; } = new();
}

/// <summary>
/// Bulk seat operation item result
/// </summary>
public class BulkSeatOperationItemResult
{
    public Guid SeatId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Seat map row DTO for import/export
/// </summary>
public class SeatMapRowDto
{
    public string Section { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public bool IsAccessible { get; set; }
    public bool HasRestrictedView { get; set; }
    public string? PriceCategory { get; set; }
    public string? Notes { get; set; }
}
