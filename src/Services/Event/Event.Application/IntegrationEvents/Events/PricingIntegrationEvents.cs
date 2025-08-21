using Event.Application.Common.Models;

namespace Event.Application.IntegrationEvents.Events;

/// <summary>
/// Published when a pricing rule is created
/// </summary>
public record PricingRuleCreatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid PricingRuleId { get; init; }
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? DiscountCode { get; init; }
    public decimal? DiscountValue { get; init; }
    public string? DiscountType { get; init; }
    public int Priority { get; init; }

    public PricingRuleCreatedIntegrationEvent()
    {
        EventType = nameof(PricingRuleCreatedIntegrationEvent);
    }
}

/// <summary>
/// Published when a pricing rule is updated
/// </summary>
public record PricingRuleUpdatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid PricingRuleId { get; init; }
    public Guid EventId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public Dictionary<string, object> Changes { get; init; } = new();
    public DateTime UpdatedAt { get; init; }

    public PricingRuleUpdatedIntegrationEvent()
    {
        EventType = nameof(PricingRuleUpdatedIntegrationEvent);
    }
}

/// <summary>
/// Published when a pricing rule is activated or deactivated
/// </summary>
public record PricingRuleStatusChangedIntegrationEvent : BaseIntegrationEvent
{
    public Guid PricingRuleId { get; init; }
    public Guid EventId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string Reason { get; init; } = string.Empty;

    public PricingRuleStatusChangedIntegrationEvent()
    {
        EventType = nameof(PricingRuleStatusChangedIntegrationEvent);
    }
}

/// <summary>
/// Published when a pricing rule expires
/// </summary>
public record PricingRuleExpiredIntegrationEvent : BaseIntegrationEvent
{
    public Guid PricingRuleId { get; init; }
    public Guid EventId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public DateTime ExpiredAt { get; init; }
    public int TotalUses { get; init; }

    public PricingRuleExpiredIntegrationEvent()
    {
        EventType = nameof(PricingRuleExpiredIntegrationEvent);
    }
}

/// <summary>
/// Published when a discount code is used
/// </summary>
public record DiscountCodeUsedIntegrationEvent : BaseIntegrationEvent
{
    public Guid PricingRuleId { get; init; }
    public Guid EventId { get; init; }
    public string DiscountCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid? ReservationId { get; init; }
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto OrderAmount { get; init; } = null!;
    public int RemainingUses { get; init; }

    public DiscountCodeUsedIntegrationEvent()
    {
        EventType = nameof(DiscountCodeUsedIntegrationEvent);
    }
}

/// <summary>
/// Published when dynamic pricing is updated
/// </summary>
public record DynamicPricingUpdatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public List<DynamicPriceUpdateDto> PriceUpdates { get; init; } = new();
    public int DemandLevel { get; init; }
    public string Reason { get; init; } = string.Empty;

    public DynamicPricingUpdatedIntegrationEvent()
    {
        EventType = nameof(DynamicPricingUpdatedIntegrationEvent);
    }
}

/// <summary>
/// Supporting DTOs
/// </summary>
public record DynamicPriceUpdateDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public MoneyDto PreviousPrice { get; init; } = null!;
    public MoneyDto NewPrice { get; init; } = null!;
    public decimal PriceChangePercentage { get; init; }
}
