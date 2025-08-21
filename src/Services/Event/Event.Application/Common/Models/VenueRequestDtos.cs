namespace Event.Application.Common.Models;

/// <summary>
/// Request DTO for creating a new venue
/// </summary>
public record CreateVenueRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddressDto Address { get; init; } = null!;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public int Capacity { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }
}

/// <summary>
/// Request DTO for updating an existing venue
/// </summary>
public record UpdateVenueRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public AddressDto? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public int? Capacity { get; init; }
    public string? TimeZone { get; init; }
    public int? TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }
}

/// <summary>
/// Request DTO for searching venues
/// </summary>
public record SearchVenuesRequest
{
    public string? SearchTerm { get; init; }
    public string? Query { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public int? MinCapacity { get; init; }
    public int? MaxCapacity { get; init; }
    public bool? HasSeatMap { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal? RadiusKm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "Name";
    public string? SortDirection { get; init; } = "asc";
}

/// <summary>
/// Request DTO for importing seat map
/// </summary>
public record ImportSeatMapRequest
{
    public List<SeatMapRowDto> SeatMapData { get; init; } = new();
    public string? Metadata { get; init; }
}

/// <summary>
/// Result DTO for seat map import operation
/// </summary>
public record SeatMapImportResult
{
    public bool Success { get; init; }
    public Guid VenueId { get; init; }
    public int ImportedSeats { get; init; }
    public string Version { get; init; } = string.Empty;
    public List<string> Warnings { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Result DTO for seat map export operation
/// </summary>
public record SeatMapExportResult
{
    public Guid VenueId { get; init; }
    public List<SeatMapRowDto> SeatMapData { get; init; } = new();
    public string Version { get; init; } = string.Empty;
    public DateTime ExportedAt { get; init; }
    public string? Metadata { get; init; }
}
