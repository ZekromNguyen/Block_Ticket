namespace Event.Application.Common.Models;

/// <summary>
/// Update ticket type request
/// </summary>
public record UpdateTicketTypeRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public MoneyDto? BasePrice { get; init; }
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto? TaxAmount { get; init; }
    public int? MinPurchaseQuantity { get; init; }
    public int? MaxPurchaseQuantity { get; init; }
    public int? MaxPerCustomer { get; init; }
    public bool? IsVisible { get; init; }
    public bool? IsResaleAllowed { get; init; }
    public bool? RequiresApproval { get; init; }
    public List<OnSaleWindowDto>? OnSaleWindows { get; init; }
}

/// <summary>
/// Get ticket types request
/// </summary>
public record GetTicketTypesRequest
{
    public Guid? EventId { get; init; }
    public string? InventoryType { get; init; }
    public bool? IsVisible { get; init; }
    public bool? IsOnSale { get; init; }
    public bool IncludeInactive { get; init; } = false;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Ticket type summary DTO
/// </summary>
public record TicketTypeSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public string InventoryType { get; init; } = string.Empty;
    public CapacityDto Capacity { get; init; } = null!;
    public bool IsOnSale { get; init; }
    public bool IsVisible { get; init; }
    public DateTime? NextOnSaleDate { get; init; }
}

/// <summary>
/// Ticket type sales statistics DTO
/// </summary>
public record TicketTypeSalesStatsDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public int SoldQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public MoneyDto TotalRevenue { get; init; } = null!;
    public MoneyDto AveragePrice { get; init; } = null!;
    public decimal SalesPercentage { get; init; }
    public DateTime? FirstSale { get; init; }
    public DateTime? LastSale { get; init; }
}

/// <summary>
/// Ticket type performance DTO
/// </summary>
public record TicketTypePerformanceDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public int SoldQuantity { get; init; }
    public MoneyDto Revenue { get; init; } = null!;
    public decimal ConversionRate { get; init; }
    public TimeSpan AverageSaleTime { get; init; }
    public List<DailySalesDto> DailySales { get; init; } = new();
}

/// <summary>
/// Daily sales DTO
/// </summary>
public record DailySalesDto
{
    public DateTime Date { get; init; }
    public int QuantitySold { get; init; }
    public MoneyDto Revenue { get; init; } = null!;
}