using Event.Application.Common.Interfaces;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Commands.DeletePricingRule;

/// <summary>
/// Handler for deleting pricing rules
/// </summary>
public class DeletePricingRuleCommandHandler : IRequestHandler<DeletePricingRuleCommand, bool>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePricingRuleCommandHandler> _logger;

    public DeletePricingRuleCommandHandler(
        IPricingRuleRepository pricingRuleRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeletePricingRuleCommandHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeletePricingRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting pricing rule {PricingRuleId}", request.PricingRuleId);

        // Get existing pricing rule
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(request.PricingRuleId, cancellationToken);
        if (pricingRule == null)
        {
            _logger.LogWarning("Pricing rule {PricingRuleId} not found for deletion", request.PricingRuleId);
            return false;
        }

        // Validate version for optimistic concurrency control
        if (pricingRule.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyException($"Pricing rule has been modified by another user. Expected version: {request.ExpectedVersion}, Current version: {pricingRule.Version}");
        }

        // Check if pricing rule is currently in use
        if (pricingRule.CurrentUses > 0)
        {
            throw new PricingDomainException($"Cannot delete pricing rule '{pricingRule.Name}' because it has been used {pricingRule.CurrentUses} times. Consider deactivating it instead.");
        }

        // Soft delete the pricing rule
        pricingRule.Delete();

        // Save changes
        _pricingRuleRepository.Update(pricingRule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted pricing rule {PricingRuleId}", request.PricingRuleId);

        return true;
    }
}
