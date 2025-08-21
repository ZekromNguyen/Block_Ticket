using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Comprehensive pricing engine service with rule evaluation, conflict resolution, and dynamic pricing
/// </summary>
public class PricingEngineService : IPricingEngineService
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PricingEngineService> _logger;

    public PricingEngineService(
        IPricingRuleRepository pricingRuleRepository,
        ICacheService cacheService,
        IConfiguration configuration,
        ILogger<PricingEngineService> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _cacheService = cacheService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<decimal> CalculateDynamicPriceAsync(Guid eventId, Guid ticketTypeId, int demandLevel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating dynamic price for event {EventId}, ticket type {TicketTypeId}, demand level {DemandLevel}", 
            eventId, ticketTypeId, demandLevel);

        // Check if dynamic pricing is enabled
        var isDynamicPricingEnabled = await IsDynamicPricingEnabledAsync(eventId, cancellationToken);
        if (!isDynamicPricingEnabled)
        {
            _logger.LogDebug("Dynamic pricing is disabled for event {EventId}", eventId);
            return 0; // Return 0 to indicate no dynamic pricing adjustment
        }

        // Get base multiplier from configuration
        var baseMultiplier = _configuration.GetValue<decimal>("EventService:DynamicPricing:BaseMultiplier", 1.0m);
        var maxMultiplier = _configuration.GetValue<decimal>("EventService:DynamicPricing:MaxMultiplier", 2.0m);
        var minMultiplier = _configuration.GetValue<decimal>("EventService:DynamicPricing:MinMultiplier", 0.8m);

        // Calculate demand-based multiplier
        var demandMultiplier = CalculateDemandMultiplier(demandLevel, baseMultiplier, maxMultiplier, minMultiplier);

        _logger.LogDebug("Dynamic pricing multiplier for event {EventId}: {Multiplier}", eventId, demandMultiplier);

        return demandMultiplier;
    }

    public async Task<bool> IsDynamicPricingEnabledAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // Check global configuration
        var globallyEnabled = _configuration.GetValue<bool>("EventService:EnableDynamicPricing", false);
        if (!globallyEnabled)
        {
            return false;
        }

        // Check cache first
        var cacheKey = $"dynamic-pricing-enabled:{eventId}";
        var cachedResult = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedResult) && bool.TryParse(cachedResult, out var cachedValue))
        {
            return cachedValue;
        }

        // For now, return the global setting
        // In a full implementation, this would check event-specific settings
        var result = globallyEnabled;

        // Cache the result for 5 minutes
        await _cacheService.SetAsync(cacheKey, result.ToString(), TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }

    public async Task<PricingCalculationResult> CalculatePricingAsync(PricingCalculationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating pricing for event {EventId} with {ItemCount} items", 
            request.EventId, request.OrderItems.Count);

        // Get applicable pricing rules
        var applicableRules = await GetApplicablePricingRulesAsync(request, cancellationToken);

        // Calculate base amounts
        var baseAmount = CalculateBaseAmount(request.OrderItems);
        var totalQuantity = request.OrderItems.Sum(item => item.Quantity);

        // Apply pricing rules with conflict resolution
        var ruleResults = await ApplyPricingRulesAsync(applicableRules, request, baseAmount, totalQuantity, cancellationToken);

        // Calculate final amounts
        var totalDiscount = ruleResults.Sum(r => r.DiscountAmount.Amount);
        var finalAmount = baseAmount.Amount - totalDiscount;

        return new PricingCalculationResult
        {
            BaseAmount = new MoneyDto { Amount = baseAmount.Amount, Currency = baseAmount.Currency },
            TotalDiscount = new MoneyDto { Amount = totalDiscount, Currency = baseAmount.Currency },
            FinalAmount = new MoneyDto { Amount = finalAmount, Currency = baseAmount.Currency },
            AppliedRules = ruleResults,
            Currency = baseAmount.Currency
        };
    }

    private async Task<List<PricingRule>> GetApplicablePricingRulesAsync(PricingCalculationRequest request, CancellationToken cancellationToken)
    {
        // Get all active rules for the event at the effective date
        var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow;
        var allRules = await _pricingRuleRepository.GetActiveRulesForEventAsync(request.EventId, effectiveDate, cancellationToken);

        var applicableRules = new List<PricingRule>();

        foreach (var rule in allRules)
        {
            if (IsRuleApplicable(rule, request))
            {
                applicableRules.Add(rule);
            }
        }

        // Sort by priority (higher priority first)
        return applicableRules.OrderByDescending(r => r.Priority).ToList();
    }

    private bool IsRuleApplicable(PricingRule rule, PricingCalculationRequest request)
    {
        // Check if rule can be used (usage limits, etc.)
        if (!rule.CanBeUsed())
        {
            return false;
        }

        // Check discount code if required
        if (rule.Type == PricingRuleType.DiscountCode)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountCode) ||
                !string.Equals(rule.DiscountCode, request.DiscountCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check customer segment targeting
        if (rule.TargetCustomerSegments?.Any() == true)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerSegment) ||
                !rule.TargetCustomerSegments.Contains(request.CustomerSegment.ToLowerInvariant()))
            {
                return false;
            }
        }

        // Check ticket type targeting
        if (rule.TargetTicketTypeIds?.Any() == true)
        {
            var hasTargetedTickets = request.OrderItems.Any(item =>
                rule.TargetTicketTypeIds.Contains(item.TicketTypeId));

            if (!hasTargetedTickets)
            {
                return false;
            }
        }

        // Check quantity constraints
        var totalQuantity = request.OrderItems.Sum(item => item.Quantity);
        if (rule.MinQuantity.HasValue && totalQuantity < rule.MinQuantity.Value)
        {
            return false;
        }

        if (rule.MaxQuantity.HasValue && totalQuantity > rule.MaxQuantity.Value)
        {
            return false;
        }

        return true;
    }

    private async Task<List<AppliedPricingRuleResult>> ApplyPricingRulesAsync(
        List<PricingRule> rules, 
        PricingCalculationRequest request, 
        Money baseAmount, 
        int totalQuantity, 
        CancellationToken cancellationToken)
    {
        var results = new List<AppliedPricingRuleResult>();
        var remainingAmount = baseAmount;

        foreach (var rule in rules)
        {
            // Check minimum order amount constraint
            if (rule.MinOrderAmount != null && remainingAmount < rule.MinOrderAmount)
            {
                continue;
            }

            // Calculate discount for this rule
            var discount = rule.CalculateDiscount(remainingAmount, totalQuantity);
            
            if (discount.Amount > 0)
            {
                results.Add(new AppliedPricingRuleResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    RuleType = rule.Type.ToString(),
                    DiscountAmount = new MoneyDto { Amount = discount.Amount, Currency = discount.Currency },
                    Priority = rule.Priority
                });

                // Update remaining amount for stackable rules
                // For now, we'll apply rules sequentially (stackable)
                // In a more complex system, you might have different stacking strategies
                remainingAmount = remainingAmount - discount;

                // Prevent negative amounts
                if (remainingAmount.Amount < 0)
                {
                    remainingAmount = Money.Zero(baseAmount.Currency);
                    break;
                }
            }
        }

        return results;
    }

    private static Money CalculateBaseAmount(List<OrderItemDto> orderItems)
    {
        if (!orderItems.Any())
            return Money.Zero("USD");

        var currency = orderItems.First().UnitPrice.Currency;
        var total = orderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);

        return new Money(total, currency);
    }

    private static decimal CalculateDemandMultiplier(int demandLevel, decimal baseMultiplier, decimal maxMultiplier, decimal minMultiplier)
    {
        // Simple demand-based pricing algorithm
        // In a real system, this would be more sophisticated
        var multiplier = demandLevel switch
        {
            <= 20 => minMultiplier,           // Low demand
            <= 40 => baseMultiplier * 0.9m,  // Below average
            <= 60 => baseMultiplier,          // Average
            <= 80 => baseMultiplier * 1.2m,  // Above average
            _ => maxMultiplier                // High demand
        };

        return Math.Max(minMultiplier, Math.Min(maxMultiplier, multiplier));
    }
}

/// <summary>
/// Pricing calculation request
/// </summary>
public class PricingCalculationRequest
{
    public Guid EventId { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public string? CustomerSegment { get; set; }
    public string? DiscountCode { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

/// <summary>
/// Order item DTO for pricing calculations
/// </summary>
public class OrderItemDto
{
    public Guid TicketTypeId { get; set; }
    public int Quantity { get; set; }
    public MoneyDto UnitPrice { get; set; } = null!;
}

/// <summary>
/// Pricing calculation result
/// </summary>
public class PricingCalculationResult
{
    public MoneyDto BaseAmount { get; set; } = null!;
    public MoneyDto TotalDiscount { get; set; } = null!;
    public MoneyDto FinalAmount { get; set; } = null!;
    public List<AppliedPricingRuleResult> AppliedRules { get; set; } = new();
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Applied pricing rule result
/// </summary>
public class AppliedPricingRuleResult
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public MoneyDto DiscountAmount { get; set; } = null!;
    public int Priority { get; set; }
}
