using Event.Domain.Models;
using Event.Application.Common.Models;
using System.IO;

namespace Event.Application.Interfaces.Infrastructure;

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
        Event.Domain.Models.SeatMapSchema seatMapSchema,
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
    Task<SchemaValidationResultDto> ValidateSeatMapSchemaAsync(
        Stream dataStream,
        SeatMapFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate seat map schema object
    /// </summary>
    Task<SchemaValidationResultDto> ValidateSeatMapSchemaAsync(
        Event.Domain.Models.SeatMapSchema seatMapSchema,
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
        Event.Domain.Models.SeatMapSchema newSeatMap,
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
