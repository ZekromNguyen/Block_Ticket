using Event.Application.Common.Models;
using Event.Domain.Enums;
using MediatR;

namespace Event.Application.Features.PricingRules.Queries.GetEventPricingRules;

/// <summary>
/// Query to get pricing rules for an event
/// </summary>
public record GetEventPricingRulesQuery : IRequest<List<PricingRuleDto>>
{
    public Guid EventId { get; init; }
    public bool IncludeInactive { get; init; } = false;
    public PricingRuleType? Type { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public string? SortBy { get; init; } = "Priority";
    public bool SortDescending { get; init; } = false;
}
