using Event.Domain.Models;

namespace Event.Application.Common.Models;

// SeatMap Import/Export DTOs
public record SeatMapImportOptions
{
    public bool OverwriteExisting { get; init; } = false;
    public bool ValidateBeforeImport { get; init; } = true;
    public string? ImportedBy { get; init; }
    public Dictionary<string, object>? CustomOptions { get; init; }
}

public record SeatMapExportOptions
{
    public bool IncludeMetadata { get; init; } = true;
    public string? ExportFormat { get; init; }
    public bool CompressOutput { get; init; } = false;
    public Dictionary<string, object>? CustomOptions { get; init; }
}

public record SeatMapImportPreview
{
    public int TotalSeats { get; init; }
    public int NewSeats { get; init; }
    public int UpdatedSeats { get; init; }
    public int ConflictingSeats { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object>? PreviewData { get; init; }
}

public record SeatMapFormatInfo
{
    public string FormatName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> SupportedFileExtensions { get; init; } = new();
    public bool SupportsImport { get; init; }
    public bool SupportsExport { get; init; }
    public Dictionary<string, object>? Capabilities { get; init; }
}

public record SeatMapTemplateOptions
{
    public string TemplateName { get; init; } = string.Empty;
    public bool IncludeExamples { get; init; } = true;
    public string? Language { get; init; }
    public Dictionary<string, object>? CustomFields { get; init; }
}

public record SchemaValidationResultDto
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public string? Version { get; init; }
    public Dictionary<string, object>? ValidationDetails { get; init; }
}

// Bulk Operations DTOs
public record BulkSeatMapImportItem
{
    public Guid VenueId { get; init; }
    public string DataSource { get; init; } = string.Empty;
    public SeatMapFormat Format { get; init; }
    public SeatMapImportOptions Options { get; init; } = new();
    public Dictionary<string, object>? Metadata { get; init; }
}

public record BulkSeatMapImportResult
{
    public int TotalItems { get; init; }
    public int SuccessfulImports { get; init; }
    public int FailedImports { get; init; }
    public List<BulkSeatMapImportItemResult> Results { get; init; } = new();
    public TimeSpan ProcessingTime { get; init; }
    public Dictionary<string, object>? Summary { get; init; }
}

public record BulkSeatMapImportItemResult
{
    public Guid VenueId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public SeatMapImportResult? ImportResult { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

public record BulkImportOptions
{
    public int MaxConcurrentOperations { get; init; } = 5;
    public bool StopOnFirstError { get; init; } = false;
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object>? GlobalOptions { get; init; }
}

public record BulkSeatAttributeUpdateRequest
{
    public List<Guid> SeatIds { get; init; } = new();
    public Dictionary<string, object> AttributeUpdates { get; init; } = new();
    public bool OverwriteExisting { get; init; } = false;
}

public record BulkSeatUpdateResult
{
    public int TotalSeats { get; init; }
    public int UpdatedSeats { get; init; }
    public int SkippedSeats { get; init; }
    public List<string> Errors { get; init; } = new();
    public Dictionary<Guid, Dictionary<string, object>> UpdatedAttributes { get; init; } = new();
}

// SeatMap Copy/Merge DTOs
public record SeatMapCopyOptions
{
    public bool CopyAttributes { get; init; } = true;
    public bool CopyPricing { get; init; } = false;
    public bool CopyAvailability { get; init; } = false;
    public string? MappingStrategy { get; init; }
    public Dictionary<string, object>? CustomMappings { get; init; }
}

public record SeatMapCopyResult
{
    public Guid SourceVenueId { get; init; }
    public Guid TargetVenueId { get; init; }
    public int CopiedSeats { get; init; }
    public int SkippedSeats { get; init; }
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object>? CopyDetails { get; init; }
}

public record SeatMapMergeOptions
{
    public ConflictResolution ConflictResolution { get; init; } = ConflictResolution.KeepExisting;
    public bool CreateBackup { get; init; } = true;
    public List<string>? AttributesToMerge { get; init; }
    public Dictionary<string, object>? MergeRules { get; init; }
}

public record SeatMapMergeResult
{
    public int MergedSeats { get; init; }
    public int ConflictingSeats { get; init; }
    public int NewSeats { get; init; }
    public List<string> Conflicts { get; init; } = new();
    public Guid? BackupVersionId { get; init; }
    public Dictionary<string, object>? MergeDetails { get; init; }
}

// Versioning DTOs
public record SeatMapVersioningResult
{
    public Guid VersionId { get; init; }
    public string VersionNote { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid VenueId { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public Dictionary<string, object>? VersionMetadata { get; init; }
}

public record SeatMapRestoreResult
{
    public Guid VenueId { get; init; }
    public Guid RestoredVersionId { get; init; }
    public DateTime RestoredAt { get; init; }
    public string RestoredBy { get; init; } = string.Empty;
    public int RestoredSeats { get; init; }
    public Guid? BackupVersionId { get; init; }
}

// Enums
public enum ConflictResolution
{
    KeepExisting,
    OverwriteWithNew,
    Merge,
    SkipConflicts,
    PromptUser
}
