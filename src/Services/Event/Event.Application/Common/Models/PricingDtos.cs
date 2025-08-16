using Event.Domain.Enums;

namespace Event.Application.Common.Models;

/// <summary>
/// Pricing rule data transfer object
/// </summary>
public record PricingRuleDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? DiscountType { get; init; }
    public decimal? DiscountValue { get; init; }
    public MoneyDto? MaxDiscountAmount { get; init; }
    public MoneyDto? MinOrderAmount { get; init; }
    public int? MinQuantity { get; init; }
    public int? MaxQuantity { get; init; }
    public string? DiscountCode { get; init; }
    public bool? IsSingleUse { get; init; }
    public int? MaxUses { get; init; }
    public int CurrentUses { get; init; }
    public List<Guid>? TargetTicketTypeIds { get; init; }
    public List<string>? TargetCustomerSegments { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Create pricing rule request
/// </summary>
public record CreatePricingRuleRequest
{
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
    public int Priority { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? DiscountType { get; init; }
    public decimal? DiscountValue { get; init; }
    public MoneyDto? MaxDiscountAmount { get; init; }
    public MoneyDto? MinOrderAmount { get; init; }
    public int? MinQuantity { get; init; }
    public int? MaxQuantity { get; init; }
    public string? DiscountCode { get; init; }
    public bool? IsSingleUse { get; init; }
    public int? MaxUses { get; init; }
    public List<Guid>? TargetTicketTypeIds { get; init; }
    public List<string>? TargetCustomerSegments { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Update pricing rule request
/// </summary>
public record UpdatePricingRuleRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? DiscountType { get; init; }
    public decimal? DiscountValue { get; init; }
    public MoneyDto? MaxDiscountAmount { get; init; }
    public MoneyDto? MinOrderAmount { get; init; }
    public int? MinQuantity { get; init; }
    public int? MaxQuantity { get; init; }
    public int? MaxUses { get; init; }
    public List<Guid>? TargetTicketTypeIds { get; init; }
    public List<string>? TargetCustomerSegments { get; init; }
}

/// <summary>
/// Calculate pricing request
/// </summary>
public record CalculatePricingRequest
{
    public Guid EventId { get; init; }
    public List<PricingCalculationItemDto> Items { get; init; } = new();
    public string? DiscountCode { get; init; }
    public Guid? UserId { get; init; }
    public List<string>? CustomerSegments { get; init; }
}

/// <summary>
/// Pricing calculation item DTO
/// </summary>
public record PricingCalculationItemDto
{
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Pricing calculation result
/// </summary>
public record PricingCalculationResult
{
    public bool Success { get; init; }
    public List<PricingDto> ItemPricing { get; init; } = new();
    public MoneyDto SubTotal { get; init; } = null!;
    public MoneyDto TotalDiscount { get; init; } = null!;
    public MoneyDto TotalTax { get; init; } = null!;
    public MoneyDto TotalFees { get; init; } = null!;
    public MoneyDto GrandTotal { get; init; } = null!;
    public List<PricingRuleApplicationDto> AppliedRules { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public DateTime CalculatedAt { get; init; }
}

// Allocation DTOs moved to AllocationDtos.cs to avoid duplication

/// <summary>
/// Allocation availability DTO
/// </summary>
public record AllocationAvailabilityDto
{
    public Guid AllocationId { get; init; }
    public string AllocationName { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public int AllocatedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public bool IsAvailable { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public DateTime? AvailableUntil { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Event sales report DTO
/// </summary>
public record EventSalesReportDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public int TotalCapacity { get; init; }
    public int SoldTickets { get; init; }
    public int ReservedTickets { get; init; }
    public int AvailableTickets { get; init; }
    public decimal SalesPercentage { get; init; }
    public MoneyDto TotalRevenue { get; init; } = null!;
    public MoneyDto AverageTicketPrice { get; init; } = null!;
    public List<TicketTypeSalesDto> TicketTypeSales { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Ticket type sales DTO
/// </summary>
public record TicketTypeSalesDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int SoldQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public MoneyDto Revenue { get; init; } = null!;
    public MoneyDto AveragePrice { get; init; } = null!;
}

/// <summary>
/// Venue utilization report DTO
/// </summary>
public record VenueUtilizationReportDto
{
    public Guid VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalEvents { get; init; }
    public int TotalCapacity { get; init; }
    public int TotalSoldTickets { get; init; }
    public decimal AverageUtilization { get; init; }
    public MoneyDto TotalRevenue { get; init; } = null!;
    public List<EventUtilizationDto> EventUtilization { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Event utilization DTO
/// </summary>
public record EventUtilizationDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public int Capacity { get; init; }
    public int SoldTickets { get; init; }
    public decimal Utilization { get; init; }
    public MoneyDto Revenue { get; init; } = null!;
}

/// <summary>
/// Promoter performance report DTO
/// </summary>
public record PromoterPerformanceReportDto
{
    public Guid PromoterId { get; init; }
    public string PromoterName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalEvents { get; init; }
    public int TotalTicketsSold { get; init; }
    public MoneyDto TotalRevenue { get; init; } = null!;
    public MoneyDto AverageEventRevenue { get; init; } = null!;
    public decimal AverageUtilization { get; init; }
    public List<EventPerformanceDto> EventPerformance { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Event performance DTO
/// </summary>
public record EventPerformanceDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public int TicketsSold { get; init; }
    public MoneyDto Revenue { get; init; } = null!;
    public decimal Utilization { get; init; }
}

/// <summary>
/// Inventory report DTO
/// </summary>
public record InventoryReportDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int ReservedCapacity { get; init; }
    public int SoldCapacity { get; init; }
    public List<TicketTypeInventoryDto> TicketTypeInventory { get; init; } = new();
    public List<AllocationInventoryDto> AllocationInventory { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Ticket type inventory DTO
/// </summary>
public record TicketTypeInventoryDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int ReservedCapacity { get; init; }
    public int SoldCapacity { get; init; }
}

/// <summary>
/// Allocation inventory DTO
/// </summary>
public record AllocationInventoryDto
{
    public Guid AllocationId { get; init; }
    public string AllocationName { get; init; } = string.Empty;
    public string AllocationType { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public int AllocatedQuantity { get; init; }
    public int RemainingQuantity { get; init; }
}

/// <summary>
/// Reservation metrics DTO
/// </summary>
public record ReservationMetricsDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int TotalReservations { get; init; }
    public int ActiveReservations { get; init; }
    public int ConfirmedReservations { get; init; }
    public int ExpiredReservations { get; init; }
    public int CancelledReservations { get; init; }
    public decimal ConversionRate { get; init; }
    public TimeSpan AverageReservationDuration { get; init; }
    public DateTime GeneratedAt { get; init; }
}
