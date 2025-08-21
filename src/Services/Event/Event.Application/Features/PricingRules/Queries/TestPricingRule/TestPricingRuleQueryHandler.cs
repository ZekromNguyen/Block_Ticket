using Event.Application.Common.Models;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.PricingRules.Queries.TestPricingRule;

/// <summary>
/// Handler for testing pricing rules
/// </summary>
public class TestPricingRuleQueryHandler : IRequestHandler<TestPricingRuleQuery, PricingTestResultDto>
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ILogger<TestPricingRuleQueryHandler> _logger;

    public TestPricingRuleQueryHandler(
        IPricingRuleRepository pricingRuleRepository,
        ILogger<TestPricingRuleQueryHandler> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _logger = logger;
    }

    public async Task<PricingTestResultDto> Handle(TestPricingRuleQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing pricing rule {PricingRuleId}", request.PricingRuleId);

        // Get the pricing rule
        var pricingRule = await _pricingRuleRepository.GetByIdAsync(request.PricingRuleId, cancellationToken);
        if (pricingRule == null)
        {
            throw new PricingDomainException($"Pricing rule with ID {request.PricingRuleId} not found");
        }

        // Calculate original amounts
        var originalAmount = CalculateOriginalAmount(request.OrderItems);
        var totalQuantity = request.OrderItems.Sum(item => item.Quantity);

        // Test if rule is applicable
        var testDate = request.TestDate ?? DateTime.UtcNow;
        var isApplicable = IsRuleApplicable(pricingRule, request, originalAmount, totalQuantity, testDate, out var reasonNotApplicable);

        var discountAmount = Money.Zero(originalAmount.Currency);
        var itemResults = new List<TestOrderItemResultDto>();

        if (isApplicable)
        {
            // Calculate discount
            discountAmount = pricingRule.CalculateDiscount(originalAmount, totalQuantity);

            // Calculate per-item results
            itemResults = CalculateItemResults(request.OrderItems, pricingRule, discountAmount, originalAmount);
        }
        else
        {
            // No discount applied
            itemResults = request.OrderItems.Select(item => new TestOrderItemResultDto
            {
                TicketTypeId = item.TicketTypeId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = new MoneyDto { Amount = item.UnitPrice.Amount * item.Quantity, Currency = item.UnitPrice.Currency },
                DiscountAmount = new MoneyDto { Amount = 0, Currency = item.UnitPrice.Currency },
                FinalLineTotal = new MoneyDto { Amount = item.UnitPrice.Amount * item.Quantity, Currency = item.UnitPrice.Currency },
                RuleApplied = false
            }).ToList();
        }

        var finalAmount = originalAmount - discountAmount;

        return new PricingTestResultDto
        {
            IsApplicable = isApplicable,
            ReasonNotApplicable = reasonNotApplicable,
            OriginalAmount = new MoneyDto { Amount = originalAmount.Amount, Currency = originalAmount.Currency },
            DiscountAmount = new MoneyDto { Amount = discountAmount.Amount, Currency = discountAmount.Currency },
            FinalAmount = new MoneyDto { Amount = finalAmount.Amount, Currency = finalAmount.Currency },
            ItemResults = itemResults,
            PricingRule = MapPricingRuleToDto(pricingRule)
        };
    }

    private static Money CalculateOriginalAmount(List<TestOrderItemDto> orderItems)
    {
        if (!orderItems.Any())
            return Money.Zero("USD");

        var currency = orderItems.First().UnitPrice.Currency;
        var total = orderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);
        
        return new Money(total, currency);
    }

    private static bool IsRuleApplicable(
        Domain.Entities.PricingRule pricingRule, 
        TestPricingRuleQuery request, 
        Money originalAmount, 
        int totalQuantity, 
        DateTime testDate,
        out string? reasonNotApplicable)
    {
        reasonNotApplicable = null;

        // Check if rule is active and effective
        if (!pricingRule.IsActive)
        {
            reasonNotApplicable = "Pricing rule is not active";
            return false;
        }

        if (!pricingRule.IsEffectiveAt(testDate))
        {
            reasonNotApplicable = $"Pricing rule is not effective at {testDate:yyyy-MM-dd HH:mm}";
            return false;
        }

        // Check if rule can be used (usage limits)
        if (!pricingRule.CanBeUsed())
        {
            reasonNotApplicable = "Pricing rule has reached its usage limit";
            return false;
        }

        // Check discount code if required
        if (pricingRule.Type == Domain.Enums.PricingRuleType.DiscountCode)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                reasonNotApplicable = "Discount code is required";
                return false;
            }

            if (!string.Equals(pricingRule.DiscountCode, request.DiscountCode, StringComparison.OrdinalIgnoreCase))
            {
                reasonNotApplicable = "Invalid discount code";
                return false;
            }
        }

        // Check minimum order amount
        if (pricingRule.MinOrderAmount != null && originalAmount < pricingRule.MinOrderAmount)
        {
            reasonNotApplicable = $"Order amount {originalAmount} is below minimum required {pricingRule.MinOrderAmount}";
            return false;
        }

        // Check quantity constraints
        if (pricingRule.MinQuantity.HasValue && totalQuantity < pricingRule.MinQuantity.Value)
        {
            reasonNotApplicable = $"Total quantity {totalQuantity} is below minimum required {pricingRule.MinQuantity.Value}";
            return false;
        }

        if (pricingRule.MaxQuantity.HasValue && totalQuantity > pricingRule.MaxQuantity.Value)
        {
            reasonNotApplicable = $"Total quantity {totalQuantity} exceeds maximum allowed {pricingRule.MaxQuantity.Value}";
            return false;
        }

        // Check ticket type targeting
        if (pricingRule.TargetTicketTypeIds?.Any() == true)
        {
            var hasTargetedTickets = request.OrderItems.Any(item => 
                pricingRule.TargetTicketTypeIds.Contains(item.TicketTypeId));
            
            if (!hasTargetedTickets)
            {
                reasonNotApplicable = "No targeted ticket types in order";
                return false;
            }
        }

        // Check customer segment targeting
        if (pricingRule.TargetCustomerSegments?.Any() == true)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerSegment) ||
                !pricingRule.TargetCustomerSegments.Contains(request.CustomerSegment.ToLowerInvariant()))
            {
                reasonNotApplicable = "Customer segment does not match rule targeting";
                return false;
            }
        }

        return true;
    }

    private static List<TestOrderItemResultDto> CalculateItemResults(
        List<TestOrderItemDto> orderItems, 
        Domain.Entities.PricingRule pricingRule, 
        Money totalDiscount, 
        Money originalAmount)
    {
        var results = new List<TestOrderItemResultDto>();

        foreach (var item in orderItems)
        {
            var lineTotal = new Money(item.UnitPrice.Amount * item.Quantity, item.UnitPrice.Currency);
            var ruleApplied = false;
            var itemDiscount = Money.Zero(item.UnitPrice.Currency);

            // Check if this item is targeted by the rule
            if (pricingRule.TargetTicketTypeIds?.Any() != true || 
                pricingRule.TargetTicketTypeIds.Contains(item.TicketTypeId))
            {
                // Proportionally distribute the discount
                var discountRatio = lineTotal.Amount / originalAmount.Amount;
                itemDiscount = new Money(totalDiscount.Amount * discountRatio, item.UnitPrice.Currency);
                ruleApplied = true;
            }

            var finalLineTotal = lineTotal - itemDiscount;

            results.Add(new TestOrderItemResultDto
            {
                TicketTypeId = item.TicketTypeId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = new MoneyDto { Amount = lineTotal.Amount, Currency = lineTotal.Currency },
                DiscountAmount = new MoneyDto { Amount = itemDiscount.Amount, Currency = itemDiscount.Currency },
                FinalLineTotal = new MoneyDto { Amount = finalLineTotal.Amount, Currency = finalLineTotal.Currency },
                RuleApplied = ruleApplied
            });
        }

        return results;
    }

    private static PricingRuleDto MapPricingRuleToDto(Domain.Entities.PricingRule pricingRule)
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
