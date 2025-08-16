using Event.Application.Common.Models;
using Event.Domain.Enums;
using MediatR;

namespace Event.Application.Features.PricingRules.Commands.CreatePricingRule;

/// <summary>
/// Command to create a new pricing rule
/// </summary>
public record CreatePricingRuleCommand : IRequest<PricingRuleDto>
{
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PricingRuleType Type { get; init; }
    public int Priority { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public DiscountType? DiscountType { get; init; }
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

    public static CreatePricingRuleCommand FromRequest(Guid eventId, CreatePricingRuleRequest request)
    {
        return new CreatePricingRuleCommand
        {
            EventId = eventId,
            Name = request.Name,
            Description = request.Description,
            Type = Enum.Parse<PricingRuleType>(request.Type),
            Priority = request.Priority,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            DiscountType = request.DiscountType != null ? Enum.Parse<DiscountType>(request.DiscountType) : null,
            DiscountValue = request.DiscountValue,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MinOrderAmount = request.MinOrderAmount,
            MinQuantity = request.MinQuantity,
            MaxQuantity = request.MaxQuantity,
            DiscountCode = request.DiscountCode,
            IsSingleUse = request.IsSingleUse,
            MaxUses = request.MaxUses,
            TargetTicketTypeIds = request.TargetTicketTypeIds,
            TargetCustomerSegments = request.TargetCustomerSegments,
            IsActive = request.IsActive
        };
    }
}
