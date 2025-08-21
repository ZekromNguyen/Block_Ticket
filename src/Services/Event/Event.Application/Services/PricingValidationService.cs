using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Event.Application.Services;

/// <summary>
/// Service for validating pricing rules and detecting conflicts
/// </summary>
public interface IPricingValidationService
{
    Task ValidatePricingRuleAsync(PricingRule pricingRule, CancellationToken cancellationToken = default);
    Task<List<PricingRuleConflict>> DetectConflictsAsync(Guid eventId, PricingRule newRule, CancellationToken cancellationToken = default);
    Task<bool> ValidateDiscountCodeUniquenessAsync(Guid eventId, string discountCode, Guid? excludeRuleId = null, CancellationToken cancellationToken = default);
    Task<List<PricingRule>> GetOverlappingRulesAsync(Guid eventId, PricingRuleType type, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeRuleId = null, CancellationToken cancellationToken = default);
}

public class PricingValidationService : IPricingValidationService
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<PricingValidationService> _logger;

    public PricingValidationService(
        IPricingRuleRepository pricingRuleRepository,
        IEventRepository eventRepository,
        ILogger<PricingValidationService> logger)
    {
        _pricingRuleRepository = pricingRuleRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task ValidatePricingRuleAsync(PricingRule pricingRule, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating pricing rule {PricingRuleId} for event {EventId}", pricingRule.Id, pricingRule.EventId);

        // Validate event exists
        var eventExists = await _eventRepository.ExistsAsync(pricingRule.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new EventDomainException($"Event with ID {pricingRule.EventId} not found");
        }

        // Validate discount code uniqueness
        if (pricingRule.Type == PricingRuleType.DiscountCode && !string.IsNullOrWhiteSpace(pricingRule.DiscountCode))
        {
            var isUnique = await ValidateDiscountCodeUniquenessAsync(
                pricingRule.EventId, 
                pricingRule.DiscountCode, 
                pricingRule.Id, 
                cancellationToken);

            if (!isUnique)
            {
                throw new PricingDomainException($"Discount code '{pricingRule.DiscountCode}' already exists for this event");
            }
        }

        // Validate business rules
        ValidateBusinessRules(pricingRule);

        // Detect conflicts
        var conflicts = await DetectConflictsAsync(pricingRule.EventId, pricingRule, cancellationToken);
        if (conflicts.Any())
        {
            var conflictMessages = conflicts.Select(c => c.Description);
            throw new PricingDomainException($"Pricing rule conflicts detected: {string.Join(", ", conflictMessages)}");
        }
    }

    public async Task<List<PricingRuleConflict>> DetectConflictsAsync(Guid eventId, PricingRule newRule, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting conflicts for pricing rule {PricingRuleId} in event {EventId}", newRule.Id, eventId);

        var conflicts = new List<PricingRuleConflict>();

        // Get overlapping rules
        var overlappingRules = await GetOverlappingRulesAsync(
            eventId, 
            newRule.Type, 
            newRule.EffectiveFrom, 
            newRule.EffectiveTo, 
            newRule.Id, 
            cancellationToken);

        foreach (var existingRule in overlappingRules)
        {
            // Check for priority conflicts
            if (existingRule.Priority == newRule.Priority)
            {
                conflicts.Add(new PricingRuleConflict
                {
                    ConflictType = PricingRuleConflictType.PriorityConflict,
                    ConflictingRuleId = existingRule.Id,
                    ConflictingRuleName = existingRule.Name,
                    Description = $"Priority conflict with rule '{existingRule.Name}' (both have priority {newRule.Priority})"
                });
            }

            // Check for discount code conflicts (already handled in validation, but included for completeness)
            if (newRule.Type == PricingRuleType.DiscountCode && 
                existingRule.Type == PricingRuleType.DiscountCode &&
                string.Equals(newRule.DiscountCode, existingRule.DiscountCode, StringComparison.OrdinalIgnoreCase))
            {
                conflicts.Add(new PricingRuleConflict
                {
                    ConflictType = PricingRuleConflictType.DiscountCodeConflict,
                    ConflictingRuleId = existingRule.Id,
                    ConflictingRuleName = existingRule.Name,
                    Description = $"Discount code '{newRule.DiscountCode}' conflicts with rule '{existingRule.Name}'"
                });
            }

            // Check for targeting conflicts (same ticket types, customer segments)
            if (HasTargetingConflict(newRule, existingRule))
            {
                conflicts.Add(new PricingRuleConflict
                {
                    ConflictType = PricingRuleConflictType.TargetingConflict,
                    ConflictingRuleId = existingRule.Id,
                    ConflictingRuleName = existingRule.Name,
                    Description = $"Targeting conflict with rule '{existingRule.Name}' (overlapping ticket types or customer segments)"
                });
            }
        }

        return conflicts;
    }

    public async Task<bool> ValidateDiscountCodeUniquenessAsync(Guid eventId, string discountCode, Guid? excludeRuleId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return true;

        var existingRule = await _pricingRuleRepository.GetByDiscountCodeAsync(eventId, discountCode, cancellationToken);
        
        // If no existing rule found, it's unique
        if (existingRule == null)
            return true;

        // If existing rule is the one we're excluding (updating), it's still unique
        if (excludeRuleId.HasValue && existingRule.Id == excludeRuleId.Value)
            return true;

        return false;
    }

    public async Task<List<PricingRule>> GetOverlappingRulesAsync(Guid eventId, PricingRuleType type, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeRuleId = null, CancellationToken cancellationToken = default)
    {
        var allRules = await _pricingRuleRepository.GetByEventIdAsync(eventId, cancellationToken);

        var overlappingRules = allRules.Where(rule =>
        {
            // Exclude the rule we're checking against
            if (excludeRuleId.HasValue && rule.Id == excludeRuleId.Value)
                return false;

            // Only check rules of the same type
            if (rule.Type != type)
                return false;

            // Only check active rules
            if (!rule.IsActive)
                return false;

            // Check for date range overlap
            return DoDateRangesOverlap(
                effectiveFrom, effectiveTo,
                rule.EffectiveFrom, rule.EffectiveTo);
        }).ToList();

        return overlappingRules;
    }

    private static void ValidateBusinessRules(PricingRule pricingRule)
    {
        // Validate discount configuration
        if (pricingRule.DiscountType.HasValue && pricingRule.DiscountValue.HasValue)
        {
            if (pricingRule.DiscountType == DiscountType.Percentage && pricingRule.DiscountValue > 100)
            {
                throw new PricingDomainException("Percentage discount cannot exceed 100%");
            }

            if (pricingRule.DiscountValue <= 0)
            {
                throw new PricingDomainException("Discount value must be greater than zero");
            }
        }

        // Validate quantity constraints
        if (pricingRule.MinQuantity.HasValue && pricingRule.MaxQuantity.HasValue)
        {
            if (pricingRule.MinQuantity > pricingRule.MaxQuantity)
            {
                throw new PricingDomainException("Minimum quantity cannot be greater than maximum quantity");
            }
        }

        // Validate discount code requirements
        if (pricingRule.Type == PricingRuleType.DiscountCode)
        {
            if (string.IsNullOrWhiteSpace(pricingRule.DiscountCode))
            {
                throw new PricingDomainException("Discount code is required for discount code rules");
            }
        }

        // Validate effective dates
        if (pricingRule.EffectiveTo.HasValue && pricingRule.EffectiveTo <= pricingRule.EffectiveFrom)
        {
            throw new PricingDomainException("Effective end date must be after start date");
        }
    }

    private static bool HasTargetingConflict(PricingRule rule1, PricingRule rule2)
    {
        // Check ticket type targeting overlap
        if (rule1.TargetTicketTypeIds?.Any() == true && rule2.TargetTicketTypeIds?.Any() == true)
        {
            var hasOverlap = rule1.TargetTicketTypeIds.Intersect(rule2.TargetTicketTypeIds).Any();
            if (hasOverlap)
                return true;
        }

        // Check customer segment targeting overlap
        if (rule1.TargetCustomerSegments?.Any() == true && rule2.TargetCustomerSegments?.Any() == true)
        {
            var hasOverlap = rule1.TargetCustomerSegments.Intersect(rule2.TargetCustomerSegments, StringComparer.OrdinalIgnoreCase).Any();
            if (hasOverlap)
                return true;
        }

        // If one rule has no targeting (applies to all) and the other has targeting, there's potential conflict
        if ((rule1.TargetTicketTypeIds?.Any() != true && rule1.TargetCustomerSegments?.Any() != true) ||
            (rule2.TargetTicketTypeIds?.Any() != true && rule2.TargetCustomerSegments?.Any() != true))
        {
            return true;
        }

        return false;
    }

    private static bool DoDateRangesOverlap(DateTime start1, DateTime? end1, DateTime start2, DateTime? end2)
    {
        // If either range is open-ended (no end date), check if they overlap
        if (!end1.HasValue && !end2.HasValue)
        {
            // Both are open-ended, they overlap if one starts before or at the same time as the other
            return true;
        }

        if (!end1.HasValue)
        {
            // First range is open-ended, overlaps if it starts before the second range ends
            return start1 <= (end2 ?? DateTime.MaxValue);
        }

        if (!end2.HasValue)
        {
            // Second range is open-ended, overlaps if it starts before the first range ends
            return start2 <= end1.Value;
        }

        // Both ranges have end dates, check for overlap
        return start1 <= end2.Value && start2 <= end1.Value;
    }
}

/// <summary>
/// Represents a conflict between pricing rules
/// </summary>
public class PricingRuleConflict
{
    public PricingRuleConflictType ConflictType { get; set; }
    public Guid ConflictingRuleId { get; set; }
    public string ConflictingRuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Types of pricing rule conflicts
/// </summary>
public enum PricingRuleConflictType
{
    PriorityConflict,
    DiscountCodeConflict,
    TargetingConflict,
    DateRangeConflict
}
