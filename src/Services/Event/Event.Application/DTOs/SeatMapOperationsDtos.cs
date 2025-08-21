using System;
using System.Collections.Generic;
using System.IO;

namespace Event.Application.DTOs
{
    // Schema validation DTOs
    public class SchemaValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Seat map import/export DTOs
    public class SeatMapImportOptions
    {
        public bool ValidateSchema { get; set; } = true;
        public bool DryRun { get; set; } = false;
        public bool ReplaceExisting { get; set; } = false;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SeatMapImportResult
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ImportedSeats { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SeatMapExportOptions
    {
        public string Format { get; set; } = "CSV";
        public bool IncludeMetadata { get; set; } = true;
        public bool CompressOutput { get; set; } = false;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SeatMapExportResult
    {
        public bool Success { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Bulk operations DTOs
    public class BulkSeatOperationResult
    {
        public int Successful { get; set; }
        public int Failed { get; set; }
        public int Total => Successful + Failed;
        public List<string> Errors { get; set; } = new();
        public List<BulkSeatOperationItemResult> Results { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkSeatOperationItemResult
    {
        public string SeatId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkSeatUpdateRequest
    {
        public List<SeatUpdateItem> Updates { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class SeatUpdateItem
    {
        public string SeatId { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class BulkSeatUpdateResult
    {
        public int TotalRequested { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<BulkSeatOperationItemResult> Results { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkSeatAttributeUpdateRequest
    {
        public List<SeatAttributeUpdate> Updates { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class SeatAttributeUpdate
    {
        public string SeatId { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    // Copy operations DTOs
    public class SeatMapCopyOptions
    {
        public bool ReplaceExisting { get; set; } = false;
        public Dictionary<string, string> SectionMappings { get; set; } = new();
        public Dictionary<string, string> PriceCategoryMappings { get; set; } = new();
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class SeatMapCopyResult
    {
        public bool Success { get; set; }
        public int CopiedSeats { get; set; }
        public int CopiedSections { get; set; }
        public string NewChecksum { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Merge operations DTOs
    public enum MergeStrategy
    {
        Add,
        Replace,
        Merge
    }

    public enum ConflictResolution
    {
        UseExisting,
        UseNew,
        Merge,
        Skip,
        Fail
    }

    public class SeatMapMergeOptions
    {
        public MergeStrategy Strategy { get; set; } = MergeStrategy.Merge;
        public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.UseExisting;
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class SeatMapMergeResult
    {
        public bool Success { get; set; }
        public int AddedSeats { get; set; }
        public int UpdatedSeats { get; set; }
        public int RemovedSeats { get; set; }
        public int Changes => AddedSeats + UpdatedSeats + RemovedSeats;
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Versioning DTOs
    public class SeatMapVersioningResult
    {
        public bool Success { get; set; }
        public string VersionId { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public string VersionNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ArchivedSeats { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SeatMapRestoreResult
    {
        public bool Success { get; set; }
        public string RestoredVersionId { get; set; } = string.Empty;
        public int RestoredSeats { get; set; }
        public DateTime RestoredAt { get; set; }
        public int Changes { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Import preview DTOs
    public class SeatMapImportPreview
    {
        public bool IsValid { get; set; }
        public int TotalSeatsToImport { get; set; }
        public int TotalSeatsToAdd { get; set; }
        public int TotalSeatsToUpdate { get; set; }
        public int TotalSeatsToRemove { get; set; }
        public SchemaValidationResultDto ValidationResult { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int Changes { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Format info DTOs
    public class SeatMapFormatInfo
    {
        public string Format { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> FileExtensions { get; set; } = new();
        public bool SupportsValidation { get; set; }
        public bool SupportsLayout { get; set; }
        public long MaxFileSize { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Bulk import DTOs
    public class BulkSeatMapImportResult
    {
        public int TotalRequested { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<BulkSeatMapImportItemResult> Results { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkSeatMapImportItemResult
    {
        public string VenueName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkSeatMapImportItem
    {
        public string VenueName { get; set; } = string.Empty;
        public string Format { get; set; } = "CSV";
        public Stream DataStream { get; set; } = Stream.Null;
        public SeatMapImportOptions Options { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkImportOptions
    {
        public bool ValidateAllFirst { get; set; } = true;
        public bool ContinueOnError { get; set; } = false;
        public int MaxConcurrentImports { get; set; } = 5;
        public Dictionary<string, object> Options { get; set; } = new();
    }

    // Template DTOs
    public class SeatMapTemplateOptions
    {
        public string TemplateSize { get; set; } = "Standard";
        public bool IncludeSampleData { get; set; } = false;
        public Dictionary<string, object> Options { get; set; } = new();
    }

    // Missing DTOs for SeatMapBulkOperationsService
    public class BulkSeatOperationRequest
    {
        public string Operation { get; set; } = string.Empty; // block, unblock, allocate, etc.
        public List<Guid> SeatIds { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool ValidateOnly { get; set; } = false;
    }

    // Schema DTOs that were missing (using different names to avoid conflicts with Domain entities)
    public class SeatMapSchemaDto
    {
        public Guid VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public List<SeatMapSectionDto> Sections { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SeatMapSectionDto
    {
        public string Name { get; set; } = string.Empty;
        public List<SeatMapRowDto> Rows { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class SeatMapRowDto
    {
        public string Name { get; set; } = string.Empty;
        public List<SeatMapSeatDto> Seats { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class SeatMapSeatDto
    {
        public string Number { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
