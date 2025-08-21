using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Queries.GetEventPricingRules;

/// <summary>
/// Handler for getting pricing rules for an event
/// </summary>
public class GetEventPricingRulesQueryHandler : IRequestHandler<GetEventPricingRulesQuery, List<PricingRuleDto>>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ILogger<GetEventPricingRulesQueryHandler> _logger;

    public GetEventPricingRulesQueryHandler(
        IPricingRuleRepository pricingRuleRepository,
        ILogger<GetEventPricingRulesQueryHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _logger = logger;
    }

    public async Task<List<PricingRuleDto>> Handle(GetEventPricingRulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pricing rules for event {EventId}, Type: {Type}, IncludeInactive: {IncludeInactive}", 
            request.EventId, request.Type, request.IncludeInactive);

        IEnumerable<PricingRule> pricingRules;

        if (request.EffectiveDate.HasValue)
        {
            // Get rules effective at a specific date
            pricingRules = await _pricingRuleRepository.GetActiveRulesForEventAsync(
                request.EventId, request.EffectiveDate.Value, cancellationToken);
        }
        else
        {
            // Get all rules for the event
            pricingRules = await _pricingRuleRepository.GetByEventIdAsync(request.EventId, cancellationToken);
        }

        // Apply filters
        if (!request.IncludeInactive)
        {
            pricingRules = pricingRules.Where(r => r.IsActive);
        }

        if (request.Type.HasValue)
        {
            pricingRules = pricingRules.Where(r => r.Type == request.Type.Value);
        }

        // Apply sorting
        pricingRules = ApplySorting(pricingRules, request.SortBy, request.SortDescending);

        var result = pricingRules.Select(MapToDto).ToList();

        _logger.LogInformation("Found {Count} pricing rules for event {EventId}", result.Count, request.EventId);

        return result;
    }

    private static IEnumerable<PricingRule> ApplySorting(IEnumerable<PricingRule> rules, string? sortBy, bool descending)
    {
        var query = sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? rules.OrderByDescending(r => r.Name) : rules.OrderBy(r => r.Name),
            "type" => descending ? rules.OrderByDescending(r => r.Type) : rules.OrderBy(r => r.Type),
            "effectivefrom" => descending ? rules.OrderByDescending(r => r.EffectiveFrom) : rules.OrderBy(r => r.EffectiveFrom),
            "createdat" => descending ? rules.OrderByDescending(r => r.CreatedAt) : rules.OrderBy(r => r.CreatedAt),
            "priority" or _ => descending ? rules.OrderByDescending(r => r.Priority) : rules.OrderBy(r => r.Priority)
        };

        return query;
    }

    private static PricingRuleDto MapToDto(PricingRule pricingRule)
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
