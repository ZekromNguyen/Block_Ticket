using MediatR;

namespace Event.Application.Features.PricingRules.Commands.DeletePricingRule;

/// <summary>
/// Command to delete a pricing rule
/// </summary>
public record DeletePricingRuleCommand(Guid PricingRuleId, int ExpectedVersion) : IRequest<bool>;
