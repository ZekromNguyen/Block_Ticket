using Event.Domain.Enums;

namespace Event.Application.Common.Models;

/// <summary>
/// Reservation data transfer object
/// </summary>
public record ReservationDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public MoneyDto TotalAmount { get; init; } = null!;
    public string? DiscountCode { get; init; }
    public MoneyDto? DiscountAmount { get; init; }
    public List<ReservationItemDto> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Reservation item data transfer object
/// </summary>
public record ReservationItemDto
{
    public Guid Id { get; init; }
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public Guid? SeatId { get; init; }
    public SeatPositionDto? SeatPosition { get; init; }
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
    public MoneyDto TotalPrice => new() { Amount = UnitPrice.Amount * Quantity, Currency = UnitPrice.Currency };
}

/// <summary>
/// Money data transfer object
/// </summary>
public record MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
}

/// <summary>
/// Create reservation request
/// </summary>
public record CreateReservationRequest
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public List<ReservationItemRequestDto> Items { get; init; } = new();
    public string? DiscountCode { get; init; }
    public TimeSpan? CustomTTL { get; init; }
}

/// <summary>
/// Reservation item request DTO
/// </summary>
public record ReservationItemRequestDto
{
    public Guid TicketTypeId { get; init; }
    public List<Guid>? SeatIds { get; init; } // For reserved seating
    public int Quantity { get; init; } // For general admission
}

/// <summary>
/// Get reservations request
/// </summary>
public record GetReservationsRequest
{
    public ReservationStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Reservation validation result
/// </summary>
public record ReservationValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public MoneyDto? EstimatedTotal { get; init; }
    public DateTime? EstimatedExpiration { get; init; }
}

/// <summary>
/// Ticket type data transfer object
/// </summary>
public record TicketTypeDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string InventoryType { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto? TaxAmount { get; init; }
    public CapacityDto Capacity { get; init; } = null!;
    public int MinPurchaseQuantity { get; init; }
    public int MaxPurchaseQuantity { get; init; }
    public int MaxPerCustomer { get; init; }
    public bool IsVisible { get; init; }
    public bool IsResaleAllowed { get; init; }
    public bool RequiresApproval { get; init; }
    public List<OnSaleWindowDto> OnSaleWindows { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Ticket type public DTO (for catalog)
/// </summary>
public record TicketTypePublicDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto TotalPrice { get; init; } = null!;
    public int AvailableQuantity { get; init; }
    public int MinPurchaseQuantity { get; init; }
    public int MaxPurchaseQuantity { get; init; }
    public int MaxPerCustomer { get; init; }
    public bool IsOnSale { get; init; }
    public DateTime? OnSaleStartDate { get; init; }
    public DateTime? OnSaleEndDate { get; init; }
}

/// <summary>
/// Capacity data transfer object
/// </summary>
public record CapacityDto
{
    public int Total { get; init; }
    public int Available { get; init; }
    public int Reserved => Total - Available;
}

/// <summary>
/// On-sale window data transfer object
/// </summary>
public record OnSaleWindowDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
}

// Note: Reservation-related DTOs have been moved to Ticketing Service
// as reservations are part of the buyer purchase workflow, not promoter setup

/// <summary>
/// Create ticket type request
/// </summary>
public record CreateTicketTypeRequest
{
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string InventoryType { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto? TaxAmount { get; init; }
    public int TotalCapacity { get; init; }
    public int MinPurchaseQuantity { get; init; } = 1;
    public int MaxPurchaseQuantity { get; init; } = 10;
    public int MaxPerCustomer { get; init; } = 10;
    public bool IsVisible { get; init; } = true;
    public bool IsResaleAllowed { get; init; } = true;
    public bool RequiresApproval { get; init; }
    public List<OnSaleWindowDto> OnSaleWindows { get; init; } = new();
}

// UpdateTicketTypeRequest moved to TicketTypeDtos.cs to avoid duplication

/// <summary>
/// Availability data transfer object
/// </summary>
public record AvailabilityDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int ReservedCapacity { get; init; }
    public bool IsOnSale { get; init; }
    public DateTime? NextOnSaleDate { get; init; }
    public DateTime? OnSaleEndDate { get; init; }
}

/// <summary>
/// Event availability data transfer object
/// </summary>
public record EventAvailabilityDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int ReservedCapacity { get; init; }
    public bool HasAvailability { get; init; }
    public List<AvailabilityDto> TicketTypes { get; init; } = new();
    public string InventoryETag { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Seat availability data transfer object
/// </summary>
public record SeatAvailabilityDto
{
    public Guid EventId { get; init; }
    public Guid VenueId { get; init; }
    public List<SeatDto> AvailableSeats { get; init; } = new();
    public List<SeatDto> ReservedSeats { get; init; } = new();
    public List<SeatDto> BlockedSeats { get; init; } = new();
    public Dictionary<string, int> AvailabilityBySection { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Inventory snapshot data transfer object
/// </summary>
public record InventorySnapshotDto
{
    public Guid EventId { get; init; }
    public Dictionary<Guid, int> TicketTypeAvailability { get; init; } = new();
    public string ETag { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Pricing data transfer object
/// </summary>
public record PricingDto
{
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto? TaxAmount { get; init; }
    public MoneyDto? DiscountAmount { get; init; }
    public MoneyDto TotalPrice { get; init; } = null!;
    public List<PricingRuleApplicationDto> AppliedRules { get; init; } = new();
    public string? DiscountCode { get; init; }
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// Pricing rule application DTO
/// </summary>
public record PricingRuleApplicationDto
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
}
