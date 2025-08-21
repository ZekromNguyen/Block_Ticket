using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Commands.UpdatePricingRule;

/// <summary>
/// Handler for updating pricing rules
/// </summary>
public class UpdatePricingRuleCommandHandler : IRequestHandler<UpdatePricingRuleCommand, PricingRuleDto>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePricingRuleCommandHandler> _logger;

    public UpdatePricingRuleCommandHandler(
        IPricingRuleRepository pricingRuleRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePricingRuleCommandHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PricingRuleDto> Handle(UpdatePricingRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating pricing rule {PricingRuleId}", request.PricingRuleId);

        // Get existing pricing rule
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(request.PricingRuleId, cancellationToken);
        if (pricingRule == null)
        {
            throw new PricingDomainException($"Pricing rule with ID {request.PricingRuleId} not found");
        }

        // Validate version for optimistic concurrency control
        if (pricingRule.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyException($"Pricing rule has been modified by another user. Expected version: {request.ExpectedVersion}, Current version: {pricingRule.Version}");
        }

        // Validate discount code uniqueness if changed
        if (!string.IsNullOrWhiteSpace(request.DiscountCode) && 
            request.DiscountCode != pricingRule.DiscountCode)
        {
            var existingRule = await _pricingRuleRepository.GetByDiscountCodeAsync(
                pricingRule.EventId, request.DiscountCode, cancellationToken);
            
            if (existingRule != null && existingRule.Id != pricingRule.Id)
            {
                throw new PricingDomainException($"Discount code '{request.DiscountCode}' already exists for this event");
            }
        }

        // Apply updates
        ApplyUpdates(pricingRule, request);

        // Save changes
        _pricingRuleRepository.Update(pricingRule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated pricing rule {PricingRuleId}", request.PricingRuleId);

        return MapToDto(pricingRule);
    }

    private void ApplyUpdates(PricingRule pricingRule, UpdatePricingRuleCommand request)
    {
        // Update basic info
        pricingRule.UpdateBasicInfo(request.Name, request.Description, request.Priority);

        // Update effective dates
        pricingRule.UpdateEffectiveDates(request.EffectiveFrom, request.EffectiveTo);

        // Update discount configuration
        if (request.DiscountType.HasValue && request.DiscountValue.HasValue)
        {
            var maxDiscountAmount = request.MaxDiscountAmount != null 
                ? new Money(request.MaxDiscountAmount.Amount, request.MaxDiscountAmount.Currency)
                : null;
            
            var minOrderAmount = request.MinOrderAmount != null
                ? new Money(request.MinOrderAmount.Amount, request.MinOrderAmount.Currency)
                : null;

            pricingRule.SetDiscountConfiguration(
                request.DiscountType.Value,
                request.DiscountValue.Value,
                maxDiscountAmount,
                minOrderAmount);
        }

        // Update quantity constraints
        pricingRule.SetQuantityConstraints(request.MinQuantity, request.MaxQuantity);

        // Update discount code if applicable
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            pricingRule.SetDiscountCode(
                request.DiscountCode,
                request.IsSingleUse ?? false,
                request.MaxUses);
        }

        // Update target constraints
        pricingRule.SetTargetTicketTypes(request.TargetTicketTypeIds);
        pricingRule.SetTargetCustomerSegments(request.TargetCustomerSegments);

        // Update active status
        if (request.IsActive && !pricingRule.IsActive)
        {
            pricingRule.Activate();
        }
        else if (!request.IsActive && pricingRule.IsActive)
        {
            pricingRule.Deactivate();
        }
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
