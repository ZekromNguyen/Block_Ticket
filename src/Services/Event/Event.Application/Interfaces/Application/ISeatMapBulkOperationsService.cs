using Event.Domain.Models;
using Event.Application.Common.Models;

namespace Event.Application.Interfaces.Application;

/// <summary>
/// Service for bulk operations on seat maps
/// </summary>
public interface ISeatMapBulkOperationsService
{
    /// <summary>
    /// Bulk update seat statuses
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="updates">Status updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkUpdateSeatStatusAsync(
        Guid venueId,
        IEnumerable<SeatStatusUpdate> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update seat types
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="updates">Type updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkUpdateSeatTypeAsync(
        Guid venueId,
        IEnumerable<SeatTypeUpdate> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update seat pricing
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="updates">Pricing updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkUpdateSeatPricingAsync(
        Guid venueId,
        IEnumerable<SeatPricingUpdate> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk create seats
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="seats">Seats to create</param>
    /// <param name="options">Creation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkCreateSeatsAsync(
        Guid venueId,
        IEnumerable<SeatCreateRequest> seats,
        SeatBulkCreateOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete seats
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="seatIds">Seat identifiers to delete</param>
    /// <param name="options">Deletion options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkDeleteSeatsAsync(
        Guid venueId,
        IEnumerable<Guid> seatIds,
        SeatBulkDeleteOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk move seats (change positions)
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="moves">Seat moves</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkMoveSeatsAsync(
        Guid venueId,
        IEnumerable<SeatMoveRequest> moves,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk copy seats to another section/row
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="copyRequests">Copy requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> BulkCopySeatsAsync(
        Guid venueId,
        IEnumerable<SeatCopyRequest> copyRequests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate seats based on pattern
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="pattern">Generation pattern</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> GenerateSeatsFromPatternAsync(
        Guid venueId,
        SeatGenerationPattern pattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate bulk operation before execution
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="operation">Operation to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<SeatMapBulkValidationResult> ValidateBulkOperationAsync(
        Guid venueId,
        SeatMapBulkOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operation progress
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation progress</returns>
    Task<SeatMapBulkOperationProgress?> GetOperationProgressAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel running operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform bulk seat operation
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="request">Bulk operation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<SeatMapBulkOperationResult> PerformBulkSeatOperationAsync(
        Guid venueId,
        BulkSeatOperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update seat attributes
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk update result</returns>
    Task<BulkSeatUpdateResult> BulkUpdateSeatAttributesAsync(
        Guid venueId,
        BulkSeatAttributeUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy seat map from another venue
    /// </summary>
    /// <param name="sourceVenueId">Source venue identifier</param>
    /// <param name="targetVenueId">Target venue identifier</param>
    /// <param name="options">Copy options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Copy result</returns>
    Task<SeatMapCopyResult> CopySeatMapAsync(
        Guid sourceVenueId,
        Guid targetVenueId,
        SeatMapCopyOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create seat map version
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="versionNote">Version note</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Versioning result</returns>
    Task<SeatMapVersioningResult> CreateSeatMapVersionAsync(
        Guid venueId,
        string versionNote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore seat map version
    /// </summary>
    /// <param name="venueId">Venue identifier</param>
    /// <param name="versionId">Version identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Restore result</returns>
    Task<SeatMapRestoreResult> RestoreSeatMapVersionAsync(
        Guid venueId,
        Guid versionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seat status update request
/// </summary>
public class SeatStatusUpdate
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public Guid SeatId { get; set; }

    /// <summary>
    /// New status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional reason for status change
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Seat type update request
/// </summary>
public class SeatTypeUpdate
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public Guid SeatId { get; set; }

    /// <summary>
    /// New seat type
    /// </summary>
    public string SeatType { get; set; } = string.Empty;

    /// <summary>
    /// Optional accessibility features
    /// </summary>
    public List<string>? AccessibilityFeatures { get; set; }
}

/// <summary>
/// Seat pricing update request
/// </summary>
public class SeatPricingUpdate
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public Guid SeatId { get; set; }

    /// <summary>
    /// Price tier
    /// </summary>
    public string PriceTier { get; set; } = string.Empty;

    /// <summary>
    /// Base price
    /// </summary>
    public decimal? BasePrice { get; set; }

    /// <summary>
    /// Premium multiplier
    /// </summary>
    public decimal? PremiumMultiplier { get; set; }
}

/// <summary>
/// Seat create request
/// </summary>
public class SeatCreateRequest
{
    /// <summary>
    /// Section identifier
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Row identifier
    /// </summary>
    public string Row { get; set; } = string.Empty;

    /// <summary>
    /// Seat number
    /// </summary>
    public string SeatNumber { get; set; } = string.Empty;

    /// <summary>
    /// Seat type
    /// </summary>
    public string SeatType { get; set; } = "Standard";

    /// <summary>
    /// Position coordinates
    /// </summary>
    public SeatPosition Position { get; set; } = new();

    /// <summary>
    /// Price tier
    /// </summary>
    public string? PriceTier { get; set; }

    /// <summary>
    /// Accessibility features
    /// </summary>
    public List<string>? AccessibilityFeatures { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Seat move request
/// </summary>
public class SeatMoveRequest
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public Guid SeatId { get; set; }

    /// <summary>
    /// New position
    /// </summary>
    public SeatPosition NewPosition { get; set; } = new();

    /// <summary>
    /// New section (optional)
    /// </summary>
    public string? NewSection { get; set; }

    /// <summary>
    /// New row (optional)
    /// </summary>
    public string? NewRow { get; set; }

    /// <summary>
    /// New seat number (optional)
    /// </summary>
    public string? NewSeatNumber { get; set; }
}

/// <summary>
/// Seat copy request
/// </summary>
public class SeatCopyRequest
{
    /// <summary>
    /// Source seat identifier
    /// </summary>
    public Guid SourceSeatId { get; set; }

    /// <summary>
    /// Target section
    /// </summary>
    public string TargetSection { get; set; } = string.Empty;

    /// <summary>
    /// Target row
    /// </summary>
    public string TargetRow { get; set; } = string.Empty;

    /// <summary>
    /// Target seat numbers
    /// </summary>
    public List<string> TargetSeatNumbers { get; set; } = new();

    /// <summary>
    /// Whether to copy pricing information
    /// </summary>
    public bool CopyPricing { get; set; } = true;

    /// <summary>
    /// Whether to copy accessibility features
    /// </summary>
    public bool CopyAccessibility { get; set; } = true;
}

/// <summary>
/// Seat generation pattern
/// </summary>
public class SeatGenerationPattern
{
    /// <summary>
    /// Section identifier
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Row pattern
    /// </summary>
    public RowPattern RowPattern { get; set; } = new();

    /// <summary>
    /// Seat numbering pattern
    /// </summary>
    public SeatNumberingPattern SeatPattern { get; set; } = new();

    /// <summary>
    /// Default seat type
    /// </summary>
    public string DefaultSeatType { get; set; } = "Standard";

    /// <summary>
    /// Layout type
    /// </summary>
    public string LayoutType { get; set; } = "Linear";

    /// <summary>
    /// Spacing between seats
    /// </summary>
    public double SeatSpacing { get; set; } = 1.0;

    /// <summary>
    /// Row spacing
    /// </summary>
    public double RowSpacing { get; set; } = 1.5;
}

/// <summary>
/// Row generation pattern
/// </summary>
public class RowPattern
{
    /// <summary>
    /// Starting row identifier
    /// </summary>
    public string StartRow { get; set; } = "A";

    /// <summary>
    /// Number of rows
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Row naming type (Alphabetic, Numeric)
    /// </summary>
    public string NamingType { get; set; } = "Alphabetic";

    /// <summary>
    /// Row increment
    /// </summary>
    public int Increment { get; set; } = 1;
}

/// <summary>
/// Seat numbering pattern
/// </summary>
public class SeatNumberingPattern
{
    /// <summary>
    /// Starting seat number
    /// </summary>
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// Number of seats per row
    /// </summary>
    public int SeatsPerRow { get; set; }

    /// <summary>
    /// Numbering direction (LeftToRight, RightToLeft, CenterOut)
    /// </summary>
    public string Direction { get; set; } = "LeftToRight";

    /// <summary>
    /// Number increment
    /// </summary>
    public int Increment { get; set; } = 1;

    /// <summary>
    /// Whether to use odd/even numbering
    /// </summary>
    public bool OddEvenNumbering { get; set; } = false;
}

/// <summary>
/// Seat position coordinates
/// </summary>
public class SeatPosition
{
    /// <summary>
    /// X coordinate
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z coordinate (elevation)
    /// </summary>
    public double Z { get; set; }

    /// <summary>
    /// Rotation angle
    /// </summary>
    public double Rotation { get; set; }
}

/// <summary>
/// Bulk create options
/// </summary>
public class SeatBulkCreateOptions
{
    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to skip duplicates
    /// </summary>
    public bool SkipDuplicates { get; set; } = true;

    /// <summary>
    /// Whether to validate positions
    /// </summary>
    public bool ValidatePositions { get; set; } = true;

    /// <summary>
    /// Whether to auto-assign price tiers
    /// </summary>
    public bool AutoAssignPriceTiers { get; set; } = false;
}

/// <summary>
/// Bulk delete options
/// </summary>
public class SeatBulkDeleteOptions
{
    /// <summary>
    /// Whether to force delete (ignore constraints)
    /// </summary>
    public bool ForceDelete { get; set; } = false;

    /// <summary>
    /// Whether to cascade delete related data
    /// </summary>
    public bool CascadeDelete { get; set; } = true;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}

/// <summary>
/// Bulk operation result
/// </summary>
public class SeatMapBulkOperationResult
{
    /// <summary>
    /// Operation identifier
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of items processed
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Processing time
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Errors encountered
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Detailed results per item
    /// </summary>
    public List<SeatOperationResult> Results { get; set; } = new();
}

/// <summary>
/// Individual seat operation result
/// </summary>
public class SeatOperationResult
{
    /// <summary>
    /// Seat identifier
    /// </summary>
    public Guid? SeatId { get; set; }

    /// <summary>
    /// Seat reference (section-row-number)
    /// </summary>
    public string SeatReference { get; set; } = string.Empty;

    /// <summary>
    /// Whether this operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Operation type performed
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
}

/// <summary>
/// Bulk operation validation result
/// </summary>
public class SeatMapBulkValidationResult
{
    /// <summary>
    /// Whether the operation is valid
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
    /// Estimated processing time
    /// </summary>
    public TimeSpan? EstimatedProcessingTime { get; set; }

    /// <summary>
    /// Number of items to process
    /// </summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Bulk operation progress
/// </summary>
public class SeatMapBulkOperationProgress
{
    /// <summary>
    /// Operation identifier
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Items processed
    /// </summary>
    public int ProcessedItems { get; set; }

    /// <summary>
    /// Total items to process
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Elapsed time
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Current operation message
    /// </summary>
    public string? CurrentMessage { get; set; }

    /// <summary>
    /// Whether the operation can be cancelled
    /// </summary>
    public bool CanCancel { get; set; } = true;
}

/// <summary>
/// Bulk operation definition
/// </summary>
public class SeatMapBulkOperation
{
    /// <summary>
    /// Operation type
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Target venue
    /// </summary>
    public Guid VenueId { get; set; }

    /// <summary>
    /// Operation data (JSON)
    /// </summary>
    public string OperationData { get; set; } = string.Empty;

    /// <summary>
    /// Batch size
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to continue on errors
    /// </summary>
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Bulk seat operation request
/// </summary>
public class BulkSeatOperationRequest
{
    /// <summary>
    /// Operation type
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Seat identifiers to operate on
    /// </summary>
    public List<Guid> SeatIds { get; set; } = new();

    /// <summary>
    /// Operation data
    /// </summary>
    public Dictionary<string, object> OperationData { get; set; } = new();

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to continue on errors
    /// </summary>
    public bool ContinueOnError { get; set; } = true;
}


