namespace Event.Application.Common.Models;

/// <summary>
/// Venue data transfer object
/// </summary>
public record VenueDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddressDto Address { get; init; } = null!;
    public string TimeZone { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }
    public bool HasSeatMap { get; init; }
    public DateTime? SeatMapLastUpdated { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Venue summary DTO for listings
/// </summary>
public record VenueSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public bool HasSeatMap { get; init; }
}

/// <summary>
/// Venue catalog DTO for public listing
/// </summary>
public record VenueCatalogDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddressDto Address { get; init; } = null!;
    public int TotalCapacity { get; init; }
    public bool HasSeatMap { get; init; }
    public int UpcomingEventsCount { get; init; }
}

/// <summary>
/// Address data transfer object
/// </summary>
public record AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public CoordinatesDto? Coordinates { get; init; }
}

/// <summary>
/// Coordinates data transfer object
/// </summary>
public record CoordinatesDto
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
}

/// <summary>
/// Create venue request
/// </summary>
public record CreateVenueRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddressDto Address { get; init; } = null!;
    public string TimeZone { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }
}

/// <summary>
/// Update venue request
/// </summary>
public record UpdateVenueRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public AddressDto? Address { get; init; }
    public string? TimeZone { get; init; }
    public int? TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }
}

/// <summary>
/// Get venues request
/// </summary>
public record GetVenuesRequest
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public int? MinCapacity { get; init; }
    public int? MaxCapacity { get; init; }
    public bool? HasSeatMap { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Search venues request
/// </summary>
public record SearchVenuesRequest
{
    public string SearchTerm { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public int? MinCapacity { get; init; }
    public int? MaxCapacity { get; init; }
    public bool? HasSeatMap { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? RadiusKm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Get public venues request
/// </summary>
public record GetPublicVenuesRequest
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Seat map data transfer object
/// </summary>
public record SeatMapDto
{
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public int TotalSeats { get; init; }
    public List<SeatDto> Seats { get; init; } = new();
    public List<string> Sections { get; init; } = new();
    public string? Metadata { get; init; }
    public string? Checksum { get; init; }
    public DateTime? LastUpdated { get; init; }
}

// Duplicate ImportSeatMapRequest removed - using the more complete version below

/// <summary>
/// Venue availability DTO
/// </summary>
public record VenueAvailabilityDto
{
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public List<VenueAvailabilityDateDto> AvailableDates { get; init; } = new();
    public List<VenueBookingDto> ExistingBookings { get; init; } = new();
}

/// <summary>
/// Venue availability date DTO
/// </summary>
public record VenueAvailabilityDateDto
{
    public DateTime Date { get; init; }
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
    public List<TimeSlotDto> AvailableTimeSlots { get; init; } = new();
}

/// <summary>
/// Time slot DTO
/// </summary>
public record TimeSlotDto
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool IsAvailable { get; init; }
}

/// <summary>
/// Venue booking DTO
/// </summary>
public record VenueBookingDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
}

// Duplicate SeatDto removed - using the more complete version below

/// <summary>
/// Seat data transfer object
/// </summary>
public record SeatDto
{
    public Guid Id { get; init; }
    public Guid VenueId { get; init; }
    public string Section { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // Available, Blocked, Reserved, Confirmed
    public bool IsAccessible { get; init; }
    public bool HasRestrictedView { get; init; }
    public string? PriceCategory { get; init; }
    public string? Notes { get; init; }
    public Guid? CurrentReservationId { get; init; }
    public DateTime? ReservedUntil { get; init; }
    public Guid? AllocatedToTicketTypeId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Version { get; init; }
}

/// <summary>
/// Seat position data transfer object
/// </summary>
public record SeatPositionDto
{
    public string Section { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
}

/// <summary>
/// Update seat map request
/// </summary>
public record UpdateSeatMapRequest
{
    public List<SeatMapRowDto> SeatMapData { get; init; } = new();
}

/// <summary>
/// Seat map row DTO for import/export
/// </summary>
public record SeatMapRowDto
{
    public string Section { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public string SeatNumber { get; init; } = string.Empty;
    public bool IsAccessible { get; init; }
    public bool HasRestrictedView { get; init; }
    public string? PriceCategory { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Import seat map request
/// </summary>
public record ImportSeatMapRequest
{
    public List<SeatMapRowDto> SeatMapData { get; init; } = new();
    public bool ValidateOnly { get; init; }
    public bool ReplaceExisting { get; init; } = true;
}

/// <summary>
/// Seat map import result
/// </summary>
public record SeatMapImportResult
{
    public bool Success { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public int ImportedSeats { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public string? Checksum { get; init; }
}

/// <summary>
/// Seat map export result
/// </summary>
public record SeatMapExportResult
{
    public List<SeatMapRowDto> SeatMapData { get; init; } = new();
    public int TotalSeats { get; init; }
    public List<string> Sections { get; init; } = new();
    public string? Checksum { get; init; }
    public DateTime ExportedAt { get; init; }
}
