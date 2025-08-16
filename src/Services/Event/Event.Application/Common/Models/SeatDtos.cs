namespace Event.Application.Common.Models;

/// <summary>
/// Update seat request
/// </summary>
public record UpdateSeatRequest
{
    public bool? IsAccessible { get; init; }
    public bool? HasRestrictedView { get; init; }
    public string? PriceCategory { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Get seats request
/// </summary>
public record GetSeatsRequest
{
    public Guid? VenueId { get; init; }
    public string? Section { get; init; }
    public string? Row { get; init; }
    public string? Status { get; init; }
    public string? PriceCategory { get; init; }
    public bool? IsAccessible { get; init; }
    public bool? HasRestrictedView { get; init; }
    public Guid? AllocatedToTicketTypeId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Seat summary DTO
/// </summary>
public record SeatSummaryDto
{
    public Guid Id { get; init; }
    public string Section { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsAccessible { get; init; }
    public bool HasRestrictedView { get; init; }
    public string? PriceCategory { get; init; }
}

/// <summary>
/// Bulk seat operation request
/// </summary>
public record BulkSeatOperationRequest
{
    public List<Guid> SeatIds { get; init; } = new();
    public string Operation { get; init; } = string.Empty; // Block, Unblock, Allocate, Deallocate
    public object? OperationData { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Bulk seat operation result
/// </summary>
public record BulkSeatOperationResult
{
    public int TotalRequested { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public List<BulkSeatOperationItemResult> Results { get; init; } = new();
}

/// <summary>
/// Bulk seat operation item result
/// </summary>
public record BulkSeatOperationItemResult
{
    public Guid SeatId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Venue seat map DTO
/// </summary>
public record VenueSeatMapDto
{
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public int ReservedSeats { get; init; }
    public int BlockedSeats { get; init; }
    public List<SectionDto> Sections { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Section DTO for seat maps
/// </summary>
public record SectionDto
{
    public string Name { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public List<RowDto> Rows { get; init; } = new();
}

/// <summary>
/// Row DTO for seat maps
/// </summary>
public record RowDto
{
    public string Name { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public List<SeatSummaryDto> Seats { get; init; } = new();
}

/// <summary>
/// Seat filter DTO
/// </summary>
public record SeatFilterDto
{
    public List<string> Sections { get; init; } = new();
    public List<string> Rows { get; init; } = new();
    public List<string> Statuses { get; init; } = new();
    public List<string> PriceCategories { get; init; } = new();
    public bool? IsAccessible { get; init; }
    public bool? HasRestrictedView { get; init; }
}

/// <summary>
/// Seat allocation result DTO
/// </summary>
public record SeatAllocationResult
{
    public bool Success { get; init; }
    public List<Guid> AllocatedSeatIds { get; init; } = new();
    public List<Guid> FailedSeatIds { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public string? AllocationId { get; init; }
}

/// <summary>
/// Seat reservation result DTO
/// </summary>
public record SeatReservationResult
{
    public bool Success { get; init; }
    public Guid? ReservationId { get; init; }
    public List<Guid> ReservedSeatIds { get; init; } = new();
    public DateTime? ReservedUntil { get; init; }
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Seat statistics DTO
/// </summary>
public record SeatStatisticsDto
{
    public Guid VenueId { get; init; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public int ReservedSeats { get; init; }
    public int ConfirmedSeats { get; init; }
    public int BlockedSeats { get; init; }
    public Dictionary<string, int> SeatsBySection { get; init; } = new();
    public Dictionary<string, int> SeatsByStatus { get; init; } = new();
    public Dictionary<string, int> SeatsByPriceCategory { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}
