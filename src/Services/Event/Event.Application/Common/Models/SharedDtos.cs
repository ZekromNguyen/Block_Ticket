namespace Event.Application.Common.Models;

/// <summary>
/// Money data transfer object
/// </summary>
public record MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
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
    public MoneyDto? MinPrice { get; init; }
    public MoneyDto? MaxPrice { get; init; }
    public CapacityDto Capacity { get; init; } = null!;
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int SoldCapacity { get; init; }
    public int MinPurchaseQuantity { get; init; }
    public int MaxPurchaseQuantity { get; init; }
    public int MaxPerCustomer { get; init; }
    public bool IsVisible { get; init; }
    public bool IsResaleAllowed { get; init; }
    public bool RequiresApproval { get; init; }
    public bool IsOnSale { get; init; }
    public List<OnSaleWindowDto> OnSaleWindows { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static TicketTypeDto FromEntity(Event.Domain.Entities.TicketType entity)
    {
        return new TicketTypeDto
        {
            Id = entity.Id,
            EventId = entity.EventId,
            Name = entity.Name,
            Code = entity.Code,
            Description = entity.Description,
            InventoryType = entity.InventoryType.ToString(),
            BasePrice = new MoneyDto { Amount = entity.BasePrice.Amount, Currency = entity.BasePrice.Currency },
            ServiceFee = entity.ServiceFee != null ? new MoneyDto { Amount = entity.ServiceFee.Amount, Currency = entity.ServiceFee.Currency } : null,
            TaxAmount = entity.TaxAmount != null ? new MoneyDto { Amount = entity.TaxAmount.Amount, Currency = entity.TaxAmount.Currency } : null,
            Capacity = new CapacityDto { Total = entity.Capacity.Total, Available = entity.Capacity.Available, Sold = entity.Capacity.Sold, Reserved = entity.Capacity.Held },
            MinPurchaseQuantity = entity.MinPurchaseQuantity,
            MaxPurchaseQuantity = entity.MaxPurchaseQuantity,
            MaxPerCustomer = entity.MaxPerCustomer,
            IsVisible = entity.IsVisible,
            IsResaleAllowed = entity.IsResaleAllowed,
            RequiresApproval = entity.RequiresApproval,
            IsOnSale = entity.IsOnSaleNow(),
            OnSaleWindows = entity.OnSaleWindows.Select(w => new OnSaleWindowDto { StartDate = w.StartDate, EndDate = w.EndDate, TimeZone = w.TimeZone.Value }).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}


/// <summary>
/// Ticket type public DTO (for public API)
/// </summary>
public record TicketTypePublicDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto? ServiceFee { get; init; }
    public MoneyDto TotalPrice { get; init; } = null!;
    public MoneyDto CurrentPrice { get; init; } = null!;
    public int AvailableCapacity { get; init; }
    public int AvailableQuantity { get; init; }
    public int MinPurchaseQuantity { get; init; } = 1;
    public int MaxPurchaseQuantity { get; init; } = 10;
    public int MaxPerCustomer { get; init; } = 10;
    public bool IsOnSale { get; init; }
    public DateTime? NextOnSaleDate { get; init; }
    public DateTime? OnSaleStartDate { get; init; }
    public DateTime? OnSaleEndDate { get; init; }
}

/// <summary>
/// On sale window DTO
/// </summary>
public record OnSaleWindowDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Name { get; init; }
    public string? TimeZone { get; init; }
}

/// <summary>
/// Capacity DTO
/// </summary>
public record CapacityDto
{
    public int Total { get; init; }
    public int Available { get; init; }
    public int Sold { get; init; }
    public int Reserved { get; init; }
}

/// <summary>
/// Availability DTO
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
/// Pricing DTO
/// </summary>
public record PricingDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public MoneyDto BasePrice { get; init; } = null!;
    public MoneyDto CurrentPrice { get; init; } = null!;
    public MoneyDto? DiscountAmount { get; init; }
    public string? DiscountCode { get; init; }
    public List<PricingRuleApplicationDto> AppliedRules { get; init; } = new();
}

/// <summary>
/// Pricing rule application DTO
/// </summary>
public record PricingRuleApplicationDto
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public string DiscountType { get; init; } = string.Empty;
}

/// <summary>
/// Event availability DTO
/// </summary>
public record EventAvailabilityDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public List<AvailabilityDto> TicketTypes { get; init; } = new();
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int ReservedCapacity { get; init; }
    public bool HasAvailableTickets { get; init; }
    public bool HasAvailability { get; init; }
    public DateTime LastUpdated { get; init; }
    public string InventoryETag { get; init; } = string.Empty;
}

/// <summary>
/// Seat availability DTO
/// </summary>
public record SeatAvailabilityDto
{
    public Guid SeatId { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Row { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public MoneyDto? Price { get; init; }
}

/// <summary>
/// Inventory snapshot DTO
/// </summary>
public record InventorySnapshotDto
{
    public Guid EventId { get; init; }
    public List<TicketTypeInventoryDto> TicketTypes { get; init; } = new();
    public int TotalCapacity { get; init; }
    public int TotalSold { get; init; }
    public int TotalAvailable { get; init; }
    public DateTime SnapshotTime { get; init; }
    public string ETag { get; init; } = string.Empty;
}



/// <summary>
/// Create ticket type request
/// </summary>
public record CreateTicketTypeRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string InventoryType { get; init; } = "General";
    public MoneyDto BasePrice { get; init; } = null!;
    public int Capacity { get; init; }
    public int MinPurchaseQuantity { get; init; } = 1;
    public int MaxPurchaseQuantity { get; init; } = 10;
    public int MaxPerCustomer { get; init; } = 10;
    public bool IsVisible { get; init; } = true;
    public List<OnSaleWindowDto> OnSaleWindows { get; init; } = new();
    public bool RequiresApproval { get; init; }
}


