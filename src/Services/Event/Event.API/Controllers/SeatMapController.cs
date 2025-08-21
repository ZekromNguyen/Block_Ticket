using Event.Application.Interfaces.Application;
using Event.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using SeatMapFormat = Event.Application.Interfaces.Application.SeatMapFormat;
using SeatMapImportResult = Event.Application.Interfaces.Application.SeatMapImportResult;
using SeatMapSchema = Event.Application.Interfaces.Application.SeatMapSchema;
using SeatMapImportPreview = Event.Application.Interfaces.Application.SeatMapImportPreview;
using SeatMapFormatInfo = Event.Application.Interfaces.Application.SeatMapFormatInfo;
using SchemaValidationResult = Event.Application.Interfaces.Application.SchemaValidationResult;
using SeatMapImportOptions = Event.Application.Interfaces.Application.SeatMapImportOptions;
using SeatMapExportOptions = Event.Application.Interfaces.Application.SeatMapExportOptions;
using SeatMapTemplateOptions = Event.Application.Interfaces.Application.SeatMapTemplateOptions;

namespace Event.API.Controllers;

/// <summary>
/// Controller for seat map import/export and bulk operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/venues/{venueId:guid}/seatmap")]
[ApiVersion("1.0")]
public class SeatMapController : ControllerBase
{
    private readonly ISeatMapImportExportService _importExportService;
    private readonly ISeatMapBulkOperationsService _bulkOperationsService;
    private readonly ILogger<SeatMapController> _logger;

    public SeatMapController(
        ISeatMapImportExportService importExportService,
        ISeatMapBulkOperationsService bulkOperationsService,
        ILogger<SeatMapController> logger)
    {
        _importExportService = importExportService;
        _bulkOperationsService = bulkOperationsService;
        _logger = logger;
    }

    /// <summary>
    /// Import seat map from file
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="file">Seat map file</param>
    /// <param name="format">File format (Json, Csv, Excel, Xml)</param>
    /// <param name="validateSchema">Whether to validate schema before import</param>
    /// <param name="replaceExisting">Whether to replace existing seat map</param>
    /// <param name="dryRun">Whether to perform dry run validation only</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(SeatMapImportResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<SeatMapImportResult>> ImportSeatMap(
        [FromRoute] Guid venueId,
        [FromForm] IFormFile file,
        [FromQuery] SeatMapFormat format = SeatMapFormat.Json,
        [FromQuery] bool validateSchema = true,
        [FromQuery] bool replaceExisting = true,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing seat map for venue {VenueId} from file {FileName} in format {Format}",
            venueId, file.FileName, format);

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required and cannot be empty");
        }

        // Validate file size (50MB max)
        if (file.Length > 50 * 1024 * 1024)
        {
            return BadRequest("File size cannot exceed 50MB");
        }

        try
        {
            var options = new SeatMapImportOptions
            {
                ValidateSchema = validateSchema,
                ReplaceExisting = replaceExisting,
                DryRun = dryRun,
                BatchSize = 1000
            };

            using var stream = file.OpenReadStream();
            var result = await _importExportService.ImportSeatMapAsync(venueId, stream, format, options, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing seat map for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to import seat map");
        }
    }

    /// <summary>
    /// Import seat map from JSON schema
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="seatMapSchema">Seat map schema</param>
    /// <param name="validateSchema">Whether to validate schema before import</param>
    /// <param name="replaceExisting">Whether to replace existing seat map</param>
    /// <param name="dryRun">Whether to perform dry run validation only</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result</returns>
    [HttpPost("import/schema")]
    [ProducesResponseType(typeof(SeatMapImportResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SeatMapImportResult>> ImportSeatMapFromSchema(
        [FromRoute] Guid venueId,
        [FromBody] SeatMapSchema seatMapSchema,
        [FromQuery] bool validateSchema = true,
        [FromQuery] bool replaceExisting = true,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing seat map from schema for venue {VenueId}", venueId);

        try
        {
            var options = new SeatMapImportOptions
            {
                ValidateSchema = validateSchema,
                ReplaceExisting = replaceExisting,
                DryRun = dryRun,
                BatchSize = 1000
            };

            var result = await _importExportService.ImportSeatMapFromSchemaAsync(venueId, seatMapSchema, options, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing seat map from schema for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to import seat map from schema");
        }
    }

    /// <summary>
    /// Export seat map to file
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="format">Export format</param>
    /// <param name="includeLayout">Include layout information</param>
    /// <param name="includeStatuses">Include current seat statuses</param>
    /// <param name="includeAllocations">Include current allocations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> ExportSeatMap(
        [FromRoute] Guid venueId,
        [FromQuery] SeatMapFormat format = SeatMapFormat.Json,
        [FromQuery] bool includeLayout = true,
        [FromQuery] bool includeStatuses = false,
        [FromQuery] bool includeAllocations = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting seat map for venue {VenueId} in format {Format}", venueId, format);

        try
        {
            var options = new SeatMapExportOptions
            {
                IncludeLayout = includeLayout,
                IncludeStatuses = includeStatuses,
                IncludeAllocations = includeAllocations
            };

            var stream = await _importExportService.ExportSeatMapToStreamAsync(venueId, format, options, cancellationToken);

            var fileName = $"seatmap_{venueId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var contentType = format switch
            {
                SeatMapFormat.Json => "application/json",
                SeatMapFormat.Csv => "text/csv",
                SeatMapFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                SeatMapFormat.Xml => "application/xml",
                _ => "application/octet-stream"
            };

            var fileExtension = format switch
            {
                SeatMapFormat.Json => ".json",
                SeatMapFormat.Csv => ".csv",
                SeatMapFormat.Excel => ".xlsx",
                SeatMapFormat.Xml => ".xml",
                _ => ".dat"
            };

            return File(stream, contentType, fileName + fileExtension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting seat map for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to export seat map");
        }
    }

    /// <summary>
    /// Preview import results without applying changes
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="file">Seat map file</param>
    /// <param name="format">File format</param>
    /// <param name="replaceExisting">Whether import would replace existing seat map</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import preview</returns>
    [HttpPost("import/preview")]
    [ProducesResponseType(typeof(SeatMapImportPreview), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SeatMapImportPreview>> PreviewImport(
        [FromRoute] Guid venueId,
        [FromForm] IFormFile file,
        [FromQuery] SeatMapFormat format = SeatMapFormat.Json,
        [FromQuery] bool replaceExisting = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Previewing seat map import for venue {VenueId}", venueId);

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required and cannot be empty");
        }

        try
        {
            var options = new SeatMapImportOptions
            {
                ReplaceExisting = replaceExisting,
                ValidateSchema = true
            };

            using var stream = file.OpenReadStream();
            var preview = await _importExportService.PreviewImportAsync(venueId, stream, format, options, cancellationToken);

            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing import for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to preview import");
        }
    }

    /// <summary>
    /// Validate seat map schema
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="file">Seat map file</param>
    /// <param name="format">File format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(SchemaValidationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SchemaValidationResult>> ValidateSchema(
        [FromRoute] Guid venueId,
        [FromForm] IFormFile file,
        [FromQuery] SeatMapFormat format = SeatMapFormat.Json,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating seat map schema for venue {VenueId}", venueId);

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required and cannot be empty");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importExportService.ValidateSeatMapSchemaAsync(stream, format, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating schema for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to validate schema");
        }
    }

    /// <summary>
    /// Validate seat map schema from JSON
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="seatMapSchema">Seat map schema</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate/schema")]
    [ProducesResponseType(typeof(SchemaValidationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SchemaValidationResult>> ValidateSchemaFromJson(
        [FromRoute] Guid venueId,
        [FromBody] SeatMapSchema seatMapSchema,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating seat map schema from JSON for venue {VenueId}", venueId);

        try
        {
            var result = await _importExportService.ValidateSeatMapSchemaAsync(seatMapSchema, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating schema from JSON for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to validate schema");
        }
    }

    /// <summary>
    /// Get supported import/export formats
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Supported formats</returns>
    [HttpGet("formats")]
    [ProducesResponseType(typeof(List<SeatMapFormatInfo>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<SeatMapFormatInfo>>> GetSupportedFormats(
        [FromRoute] Guid venueId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var formats = await _importExportService.GetSupportedFormatsAsync(cancellationToken);
            return Ok(formats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported formats");
            return StatusCode(500, "Failed to get supported formats");
        }
    }

    /// <summary>
    /// Generate seat map template
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="format">Template format</param>
    /// <param name="templateSize">Template size (Small, Medium, Large)</param>
    /// <param name="venueType">Venue type (Theater, Stadium, Arena)</param>
    /// <param name="includeSampleData">Include sample data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template file</returns>
    [HttpGet("template")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GenerateTemplate(
        [FromRoute] Guid venueId,
        [FromQuery] SeatMapFormat format = SeatMapFormat.Json,
        [FromQuery] string templateSize = "Medium",
        [FromQuery] string? venueType = null,
        [FromQuery] bool includeSampleData = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating seat map template in format {Format} for venue {VenueId}", format, venueId);

        try
        {
            var options = new SeatMapTemplateOptions
            {
                VenueType = venueType,
                IncludeExampleData = includeSampleData,
                IncludeDocumentation = true
            };

            var stream = await _importExportService.GenerateTemplateAsync(format, options, cancellationToken);

            var fileName = $"seatmap_template_{templateSize.ToLower()}";
            var contentType = format switch
            {
                SeatMapFormat.Json => "application/json",
                SeatMapFormat.Csv => "text/csv",
                SeatMapFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                SeatMapFormat.Xml => "application/xml",
                _ => "application/octet-stream"
            };

            var fileExtension = format switch
            {
                SeatMapFormat.Json => ".json",
                SeatMapFormat.Csv => ".csv",
                SeatMapFormat.Excel => ".xlsx",
                SeatMapFormat.Xml => ".xml",
                _ => ".dat"
            };

            return File(stream, contentType, fileName + fileExtension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template in format {Format}", format);
            return StatusCode(500, "Failed to generate template");
        }
    }

    /// <summary>
    /// Perform bulk seat operations
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="request">Bulk operation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("bulk-operations")]
    [ProducesResponseType(typeof(BulkSeatOperationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<BulkSeatOperationResult>> PerformBulkSeatOperation(
        [FromRoute] Guid venueId,
        [FromBody] BulkSeatOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing bulk seat operation {Operation} for {SeatCount} seats in venue {VenueId}",
            request.Operation, request.SeatIds.Count, venueId);

        try
        {
            // Map API DTO to Application DTO
            var applicationRequest = new Event.Application.Interfaces.Application.BulkSeatOperationRequest
            {
                Operation = request.Operation,
                SeatIds = request.SeatIds,
                OperationData = request.OperationData != null ? 
                    new Dictionary<string, object> { { "data", request.OperationData } } : 
                    new Dictionary<string, object>(),
                BatchSize = 1000,
                ContinueOnError = true
            };
            
            var result = await _bulkOperationsService.PerformBulkSeatOperationAsync(venueId, applicationRequest, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk seat operation for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to perform bulk operation");
        }
    }

    /// <summary>
    /// Bulk update seat attributes
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk update result</returns>
    [HttpPut("bulk-update")]
    [ProducesResponseType(typeof(BulkSeatUpdateResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<BulkSeatUpdateResult>> BulkUpdateSeatAttributes(
        [FromRoute] Guid venueId,
        [FromBody] BulkSeatAttributeUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk updating attributes for {SeatCount} seats in venue {VenueId}",
            request.SeatIds.Count, venueId);

        try
        {
            var result = await _bulkOperationsService.BulkUpdateSeatAttributesAsync(venueId, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating seat attributes for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to bulk update seat attributes");
        }
    }

    /// <summary>
    /// Copy seat map from another venue
    /// </summary>
    /// <param name="venueId">Target venue identifier</param>
    /// <param name="request">Copy request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Copy result</returns>
    [HttpPost("copy")]
    [ProducesResponseType(typeof(SeatMapCopyResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SeatMapCopyResult>> CopySeatMap(
        [FromRoute] Guid venueId,
        [FromBody] CopySeatMapRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Copying seat map from venue {SourceVenueId} to venue {TargetVenueId}",
            request.SourceVenueId, venueId);

        try
        {
            var result = await _bulkOperationsService.CopySeatMapAsync(
                request.SourceVenueId, venueId, request.Options, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying seat map to venue {VenueId}", venueId);
            return StatusCode(500, "Failed to copy seat map");
        }
    }

    /// <summary>
    /// Create seat map version
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="request">Version request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Versioning result</returns>
    [HttpPost("versions")]
    [ProducesResponseType(typeof(SeatMapVersioningResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<SeatMapVersioningResult>> CreateSeatMapVersion(
        [FromRoute] Guid venueId,
        [FromBody] CreateSeatMapVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating seat map version for venue {VenueId}", venueId);

        try
        {
            var result = await _bulkOperationsService.CreateSeatMapVersionAsync(
                venueId, request.VersionNote, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seat map version for venue {VenueId}", venueId);
            return StatusCode(500, "Failed to create seat map version");
        }
    }

    /// <summary>
    /// Restore seat map version
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Restore result</returns>
    [HttpPost("versions/{versionId:guid}/restore")]
    [ProducesResponseType(typeof(SeatMapRestoreResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<SeatMapRestoreResult>> RestoreSeatMapVersion(
        [FromRoute] Guid venueId,
        [FromRoute] Guid versionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring seat map version {VersionId} for venue {VenueId}", versionId, venueId);

        try
        {
            var result = await _bulkOperationsService.RestoreSeatMapVersionAsync(venueId, versionId, cancellationToken);

            // Check if we have valid restoration results
            if (result.RestoredSeats == 0)
            {
                return BadRequest("No seats were restored");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring seat map version {VersionId} for venue {VenueId}", versionId, venueId);
            return StatusCode(500, "Failed to restore seat map version");
        }
    }
}

#region Request Models

/// <summary>
/// Copy seat map request
/// </summary>
public record CopySeatMapRequest
{
    [Required]
    public Guid SourceVenueId { get; init; }

    public SeatMapCopyOptions Options { get; init; } = new();
}

/// <summary>
/// Create seat map version request
/// </summary>
public record CreateSeatMapVersionRequest
{
    [Required]
    [MaxLength(500)]
    public string VersionNote { get; init; } = string.Empty;
}

#endregion
