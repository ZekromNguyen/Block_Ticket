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
    public string Description { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public string InventoryType { get; init; } = string.Empty;
    public CapacityDto Capacity { get; init; } = null!;
    public bool IsOnSale { get; init; }
    public bool IsVisible { get; init; }
    public DateTime? NextOnSaleDate { get; init; }

    // Additional properties for availability
    public int AvailableQuantity { get; init; }
    public int TotalCapacity { get; init; }
    public bool IsAvailable { get; init; }
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
    public decimal SalesRate { get; init; }
    public MoneyDto TotalRevenue { get; init; } = null!;
    public MoneyDto AveragePrice { get; init; } = null!;
    public DateTime? FirstSale { get; init; }
    public DateTime? LastSale { get; init; }
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

/// <summary>
/// Ticket type pricing history DTO
/// </summary>
public record TicketTypePricingHistoryDto
{
    public Guid Id { get; init; }
    public Guid TicketTypeId { get; init; }
    public MoneyDto PreviousPrice { get; init; } = null!;
    public MoneyDto NewPrice { get; init; } = null!;
    public string? Reason { get; init; }
    public Guid ChangedByUserId { get; init; }
    public string ChangedByUserName { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Ticket type validation result
/// </summary>
public record TicketTypeValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public TicketTypeDto? ValidatedTicketType { get; init; }
}

/// <summary>
/// Bulk ticket type operation request
/// </summary>
public record BulkTicketTypeOperationRequest
{
    public List<Guid> TicketTypeIds { get; init; } = new();
    public string Operation { get; init; } = string.Empty; // UpdatePrice, SetVisibility, SetOnSaleWindows, Delete
    public object? OperationData { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Bulk ticket type operation result
/// </summary>
public record BulkTicketTypeOperationResult
{
    public int TotalRequested { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public List<BulkTicketTypeOperationItemResult> Results { get; init; } = new();
}

/// <summary>
/// Bulk ticket type operation item result
/// </summary>
public record BulkTicketTypeOperationItemResult
{
    public Guid TicketTypeId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TicketTypeDto? UpdatedTicketType { get; init; }
}

/// <summary>
/// Ticket type inventory adjustment request
/// </summary>
public record TicketTypeInventoryAdjustmentRequest
{
    public int QuantityChange { get; init; } // Positive to increase, negative to decrease
    public string Reason { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

/// <summary>
/// Ticket type inventory adjustment result
/// </summary>
public record TicketTypeInventoryAdjustmentResult
{
    public Guid TicketTypeId { get; init; }
    public int PreviousCapacity { get; init; }
    public int NewCapacity { get; init; }
    public int QuantityChanged { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime AdjustedAt { get; init; }
    public Guid AdjustedByUserId { get; init; }
}

/// <summary>
/// Ticket type performance metrics DTO
/// </summary>
public record TicketTypePerformanceDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public decimal ConversionRate { get; init; } // Reservations to purchases
    public TimeSpan AverageReservationDuration { get; init; }
    public int AbandonedReservations { get; init; }
    public int CompletedPurchases { get; init; }
    public decimal RevenuePerTicket { get; init; }
    public int RefundRequests { get; init; }
    public decimal CustomerSatisfactionScore { get; init; }
}

/// <summary>
/// Ticket type recommendation DTO
/// </summary>
public record TicketTypeRecommendationDto
{
    public Guid TicketTypeId { get; init; }
    public string RecommendationType { get; init; } = string.Empty; // PriceAdjustment, CapacityIncrease, PromotionSuggestion
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? SuggestedPriceChange { get; init; }
    public int? SuggestedCapacityChange { get; init; }
    public string? ActionRequired { get; init; }
    public int Priority { get; init; } // 1 = High, 2 = Medium, 3 = Low
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Ticket type comparison DTO
/// </summary>
public record TicketTypeComparisonDto
{
    public List<TicketTypeDto> TicketTypes { get; init; } = new();
    public List<TicketTypeSalesStatsDto> SalesComparison { get; init; } = new();
    public List<TicketTypePerformanceDto> PerformanceComparison { get; init; } = new();
    public TicketTypeRecommendationDto? Recommendation { get; init; }
}

/// <summary>
/// Ticket type export request
/// </summary>
public record TicketTypeExportRequest
{
    public Guid? EventId { get; init; }
    public List<Guid>? TicketTypeIds { get; init; }
    public string Format { get; init; } = "CSV"; // CSV, Excel, JSON
    public bool IncludeSalesData { get; init; } = true;
    public bool IncludePricingHistory { get; init; } = false;
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// Ticket type import request
/// </summary>
public record TicketTypeImportRequest
{
    public Guid EventId { get; init; }
    public string FileContent { get; init; } = string.Empty; // Base64 encoded file content
    public string Format { get; init; } = "CSV"; // CSV, Excel, JSON
    public bool ValidateOnly { get; init; } = false;
    public bool OverwriteExisting { get; init; } = false;
}

/// <summary>
/// Ticket type import result
/// </summary>
public record TicketTypeImportResult
{
    public int TotalRows { get; init; }
    public int SuccessfulImports { get; init; }
    public int FailedImports { get; init; }
    public int SkippedRows { get; init; }
    public List<TicketTypeImportError> Errors { get; init; } = new();
    public List<TicketTypeDto> ImportedTicketTypes { get; init; } = new();
}

/// <summary>
/// Ticket type import error
/// </summary>
public record TicketTypeImportError
{
    public int RowNumber { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string? RowData { get; init; }
}
