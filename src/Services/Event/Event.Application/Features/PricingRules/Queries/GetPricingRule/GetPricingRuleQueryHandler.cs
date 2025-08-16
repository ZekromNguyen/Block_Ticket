using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Queries.GetPricingRule;

/// <summary>
/// Handler for getting a pricing rule by ID
/// </summary>
public class GetPricingRuleQueryHandler : IRequestHandler<GetPricingRuleQuery, PricingRuleDto?>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ILogger<GetPricingRuleQueryHandler> _logger;

    public GetPricingRuleQueryHandler(
        IPricingRuleRepository pricingRuleRepository,
        ILogger<GetPricingRuleQueryHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _logger = logger;
    }

    public async Task<PricingRuleDto?> Handle(GetPricingRuleQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pricing rule {PricingRuleId}", request.PricingRuleId);

        var pricingRule = await _pricingRuleRepository.GetByIdAsync(request.PricingRuleId, cancellationToken);
        
        if (pricingRule == null)
        {
            _logger.LogWarning("Pricing rule {PricingRuleId} not found", request.PricingRuleId);
            return null;
        }

        return MapToDto(pricingRule);
    }

    private static PricingRuleDto MapToDto(Domain.Entities.PricingRule pricingRule)
    {
        return new PricingRuleDto
        {
            Id = pricingRule.Id,
            EventId = pricingRule.EventId,
            Name = pricingRule.Name,
            Description = pricingRule.Description,
            Type = pricingRule.Type.ToString(),
            Priority = pricingRule.Priority,
            IsActive = pricingRule.IsActive,
            EffectiveFrom = pricingRule.EffectiveFrom,
            EffectiveTo = pricingRule.EffectiveTo,
            DiscountType = pricingRule.DiscountType?.ToString(),
            DiscountValue = pricingRule.DiscountValue,
            MaxDiscountAmount = pricingRule.MaxDiscountAmount != null 
                ? new MoneyDto { Amount = pricingRule.MaxDiscountAmount.Amount, Currency = pricingRule.MaxDiscountAmount.Currency }
                : null,
            MinOrderAmount = pricingRule.MinOrderAmount != null
                ? new MoneyDto { Amount = pricingRule.MinOrderAmount.Amount, Currency = pricingRule.MinOrderAmount.Currency }
                : null,
            MinQuantity = pricingRule.MinQuantity,
            MaxQuantity = pricingRule.MaxQuantity,
            DiscountCode = pricingRule.DiscountCode,
            IsSingleUse = pricingRule.IsSingleUse,
            MaxUses = pricingRule.MaxUses,
            CurrentUses = pricingRule.CurrentUses,
            TargetTicketTypeIds = pricingRule.TargetTicketTypeIds?.ToList(),
            TargetCustomerSegments = pricingRule.TargetCustomerSegments?.ToList(),
            CreatedAt = pricingRule.CreatedAt,
            UpdatedAt = pricingRule.UpdatedAt
        };
    }
}
