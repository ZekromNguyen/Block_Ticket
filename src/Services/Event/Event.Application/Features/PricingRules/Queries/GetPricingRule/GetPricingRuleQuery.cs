using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.PricingRules.Queries.GetPricingRule;

/// <summary>
/// Query to get a pricing rule by ID
/// </summary>
public record GetPricingRuleQuery(Guid PricingRuleId) : IRequest<PricingRuleDto?>;
