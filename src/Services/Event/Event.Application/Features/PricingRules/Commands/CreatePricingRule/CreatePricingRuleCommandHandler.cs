using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Commands.CreatePricingRule;

/// <summary>
/// Handler for creating pricing rules
/// </summary>
public class CreatePricingRuleCommandHandler : IRequestHandler<CreatePricingRuleCommand, PricingRuleDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ILogger<CreatePricingRuleCommandHandler> _logger;

    public CreatePricingRuleCommandHandler(
        IEventRepository eventRepository,
        IPricingRuleRepository pricingRuleRepository,
        ILogger<CreatePricingRuleCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _pricingRuleRepository = pricingRuleRepository;
        _logger = logger;
    }

    public async Task<PricingRuleDto> Handle(CreatePricingRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating pricing rule '{Name}' for event {EventId}", request.Name, request.EventId);

        // Validate event exists
        var eventExists = await _eventRepository.ExistsAsync(request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new InvalidOperationException($"Event with ID '{request.EventId}' not found");
        }

        // Validate discount code uniqueness if provided
        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            var codeExists = await _pricingRuleRepository.DiscountCodeExistsAsync(
                request.EventId, request.DiscountCode, cancellationToken);
            if (codeExists)
            {
                throw new InvalidOperationException($"Discount code '{request.DiscountCode}' already exists for this event");
            }
        }

        // Create the pricing rule
        var pricingRule = CreatePricingRuleEntity(request);

        // Save to repository
        var createdRule = await _pricingRuleRepository.AddAsync(pricingRule, cancellationToken);
        await _pricingRuleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created pricing rule {RuleId} for event {EventId}", 
            createdRule.Id, request.EventId);

        // Convert to DTO
        return MapToDto(createdRule);
    }

    private PricingRule CreatePricingRuleEntity(CreatePricingRuleCommand request)
    {
        var pricingRule = new PricingRule(
            request.EventId,
            request.Name,
            request.Type,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Priority);

        // Configure optional properties
        if (request.Description != null)
            pricingRule.UpdateBasicInfo(pricingRule.Name, request.Description, pricingRule.Priority);

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

        if (request.MinQuantity.HasValue || request.MaxQuantity.HasValue)
        {
            pricingRule.SetQuantityConstraints(request.MinQuantity, request.MaxQuantity);
        }

        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            pricingRule.SetDiscountCode(
                request.DiscountCode,
                request.IsSingleUse ?? false,
                request.MaxUses);
        }

        if (request.TargetTicketTypeIds?.Any() == true)
        {
            pricingRule.SetTargetTicketTypes(request.TargetTicketTypeIds);
        }

        if (request.TargetCustomerSegments?.Any() == true)
        {
            pricingRule.SetTargetCustomerSegments(request.TargetCustomerSegments);
        }

        if (!request.IsActive)
        {
            pricingRule.Deactivate();
        }

        return pricingRule;
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
