using Event.Application.Common.Models;
using Event.Domain.Enums;
using MediatR;

namespace Event.Application.Features.PricingRules.Commands.UpdatePricingRule;

/// <summary>
/// Command to update an existing pricing rule
/// </summary>
public record UpdatePricingRuleCommand : IRequest<PricingRuleDto>
{
    public Guid PricingRuleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
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
    public int ExpectedVersion { get; init; }

    public static UpdatePricingRuleCommand FromRequest(Guid pricingRuleId, UpdatePricingRuleRequest request, int expectedVersion)
    {
        return new UpdatePricingRuleCommand
        {
            PricingRuleId = pricingRuleId,
            Name = request.Name,
            Description = request.Description,
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
            IsActive = request.IsActive,
            ExpectedVersion = expectedVersion
        };
    }
}

/// <summary>
/// Update pricing rule request DTO
/// </summary>
public record UpdatePricingRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
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
