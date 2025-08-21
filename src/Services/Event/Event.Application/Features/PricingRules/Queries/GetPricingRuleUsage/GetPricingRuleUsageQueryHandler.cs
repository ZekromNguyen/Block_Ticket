using Event.Application.Common.Models;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Queries.GetPricingRuleUsage;

/// <summary>
/// Handler for getting pricing rule usage statistics
/// </summary>
public class GetPricingRuleUsageQueryHandler : IRequestHandler<GetPricingRuleUsageQuery, PricingRuleUsageDto>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ILogger<GetPricingRuleUsageQueryHandler> _logger;

    public GetPricingRuleUsageQueryHandler(
        IPricingRuleRepository pricingRuleRepository,
        ILogger<GetPricingRuleUsageQueryHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _logger = logger;
    }

    public async Task<PricingRuleUsageDto> Handle(GetPricingRuleUsageQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting usage statistics for pricing rule {PricingRuleId}", request.PricingRuleId);

        // Get the pricing rule
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(request.PricingRuleId, cancellationToken);
        if (pricingRule == null)
        {
            throw new PricingDomainException($"Pricing rule with ID {request.PricingRuleId} not found");
        }

        // For now, return basic usage information from the pricing rule entity
        // In a full implementation, you would query a separate usage tracking table
        var totalDiscountAmount = Money.Zero("USD"); // This would come from usage records
        var averageDiscountAmount = Money.Zero("USD");
        
        if (pricingRule.CurrentUses > 0)
        {
            // These calculations would be based on actual usage records
            // For now, we'll provide placeholder values
            totalDiscountAmount = new Money(pricingRule.CurrentUses * 10m, "USD"); // Placeholder
            averageDiscountAmount = new Money(10m, "USD"); // Placeholder
        }

        var remainingUses = pricingRule.GetRemainingUses();
        var totalPages = (int)Math.Ceiling((double)pricingRule.CurrentUses / request.PageSize);

        return new PricingRuleUsageDto
        {
            PricingRuleId = pricingRule.Id,
            RuleName = pricingRule.Name,
            TotalUses = pricingRule.CurrentUses,
            MaxUses = pricingRule.MaxUses,
            RemainingUses = remainingUses,
            TotalDiscountAmount = new MoneyDto 
            { 
                Amount = totalDiscountAmount.Amount, 
                Currency = totalDiscountAmount.Currency 
            },
            AverageDiscountAmount = new MoneyDto 
            { 
                Amount = averageDiscountAmount.Amount, 
                Currency = averageDiscountAmount.Currency 
            },
            FirstUsed = pricingRule.CurrentUses > 0 ? pricingRule.CreatedAt : null, // Placeholder
            LastUsed = pricingRule.CurrentUses > 0 ? pricingRule.UpdatedAt : null, // Placeholder
            RecentUsages = new List<PricingRuleUsageDetailDto>(), // Would be populated from usage records
            TotalUsageCount = pricingRule.CurrentUses,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = Math.Max(1, totalPages)
        };
    }
}
