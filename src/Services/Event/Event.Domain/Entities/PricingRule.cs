using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents a pricing rule for an event
/// </summary>
public class PricingRule : BaseAuditableEntity
{
    // Basic Properties
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PricingRuleType Type { get; private set; }
    public int Priority { get; private set; } // Higher number = higher priority
    public bool IsActive { get; private set; }
    
    // Effective Period
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    
    // Discount Configuration
    public DiscountType? DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public Money? MaxDiscountAmount { get; private set; }
    public Money? MinOrderAmount { get; private set; }
    
    // Quantity-based Rules
    public int? MinQuantity { get; private set; }
    public int? MaxQuantity { get; private set; }
    
    // Discount Code Rules
    public string? DiscountCode { get; private set; }
    public bool? IsSingleUse { get; private set; }
    public int? MaxUses { get; private set; }
    public int CurrentUses { get; private set; }
    
    // Target Constraints
    public List<Guid>? TargetTicketTypeIds { get; private set; }
    public List<string>? TargetCustomerSegments { get; private set; }
    
    // Navigation Properties
    public EventAggregate Event { get; private set; } = null!;

    // For EF Core
    private PricingRule() { }

    public PricingRule(
        Guid eventId,
        string name,
        PricingRuleType type,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null,
        int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new PricingDomainException("Pricing rule name cannot be empty");
        
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new PricingDomainException("Effective end date must be after start date");

        EventId = eventId;
        Name = name.Trim();
        Type = type;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Priority = priority;
        IsActive = true;
        CurrentUses = 0;

        AddDomainEvent(new PricingRuleCreatedDomainEvent(
            Id, EventId, Type.ToString(), EffectiveFrom, EffectiveTo));
    }

    public void UpdateBasicInfo(string name, string? description, int priority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new PricingDomainException("Pricing rule name cannot be empty");

        var changes = new Dictionary<string, object>();
        
        if (Name != name.Trim())
        {
            changes["Name"] = new { Old = Name, New = name.Trim() };
            Name = name.Trim();
        }
        
        if (Description != description?.Trim())
        {
            changes["Description"] = new { Old = Description, New = description?.Trim() };
            Description = description?.Trim();
        }
        
        if (Priority != priority)
        {
            changes["Priority"] = new { Old = Priority, New = priority };
            Priority = priority;
        }

        if (changes.Any())
        {
            AddDomainEvent(new PricingRuleUpdatedDomainEvent(Id, EventId, changes));
        }
    }

    public void SetDiscountConfiguration(
        DiscountType discountType,
        decimal discountValue,
        Money? maxDiscountAmount = null,
        Money? minOrderAmount = null)
    {
        if (discountValue <= 0)
            throw new PricingDomainException("Discount value must be greater than zero");
        
        if (discountType == Enums.DiscountType.Percentage && discountValue > 100)
            throw new PricingDomainException("Percentage discount cannot exceed 100%");

        DiscountType = discountType;
        DiscountValue = discountValue;
        MaxDiscountAmount = maxDiscountAmount;
        MinOrderAmount = minOrderAmount;
    }

    public void SetQuantityConstraints(int? minQuantity, int? maxQuantity)
    {
        if (minQuantity.HasValue && minQuantity.Value <= 0)
            throw new PricingDomainException("Minimum quantity must be greater than zero");
        
        if (maxQuantity.HasValue && minQuantity.HasValue && maxQuantity.Value < minQuantity.Value)
            throw new PricingDomainException("Maximum quantity must be greater than or equal to minimum quantity");

        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
    }

    public void SetDiscountCode(string discountCode, bool isSingleUse, int? maxUses = null)
    {
        if (Type != PricingRuleType.DiscountCode)
            throw new PricingDomainException("Can only set discount code for discount code rules");
        
        if (string.IsNullOrWhiteSpace(discountCode))
            throw new PricingDomainException("Discount code cannot be empty");
        
        if (maxUses.HasValue && maxUses.Value <= 0)
            throw new PricingDomainException("Max uses must be greater than zero");

        DiscountCode = discountCode.Trim().ToUpperInvariant();
        IsSingleUse = isSingleUse;
        MaxUses = maxUses;
    }

    public void SetTargetTicketTypes(List<Guid> ticketTypeIds)
    {
        TargetTicketTypeIds = ticketTypeIds?.Any() == true ? ticketTypeIds : null;
    }

    public void SetTargetCustomerSegments(List<string> segments)
    {
        TargetCustomerSegments = segments?.Any() == true ? 
            segments.Select(s => s.Trim().ToLowerInvariant()).ToList() : null;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void IncrementUsage()
    {
        if (!CanBeUsed())
            throw new PricingDomainException("Pricing rule cannot be used");

        CurrentUses++;
    }

    public bool IsEffectiveNow()
    {
        var now = DateTime.UtcNow;
        return IsActive && 
               now >= EffectiveFrom && 
               (!EffectiveTo.HasValue || now <= EffectiveTo.Value);
    }

    public bool IsEffectiveAt(DateTime dateTime)
    {
        return IsActive && 
               dateTime >= EffectiveFrom && 
               (!EffectiveTo.HasValue || dateTime <= EffectiveTo.Value);
    }

    public bool CanBeUsed()
    {
        if (!IsEffectiveNow())
            return false;
        
        if (Type == PricingRuleType.DiscountCode)
        {
            if (IsSingleUse == true && CurrentUses > 0)
                return false;
            
            if (MaxUses.HasValue && CurrentUses >= MaxUses.Value)
                return false;
        }

        return true;
    }

    public bool AppliesTo(Guid? ticketTypeId, int quantity, string? customerSegment = null)
    {
        // Check ticket type targeting
        if (TargetTicketTypeIds?.Any() == true && ticketTypeId.HasValue)
        {
            if (!TargetTicketTypeIds.Contains(ticketTypeId.Value))
                return false;
        }

        // Check quantity constraints
        if (MinQuantity.HasValue && quantity < MinQuantity.Value)
            return false;
        
        if (MaxQuantity.HasValue && quantity > MaxQuantity.Value)
            return false;

        // Check customer segment targeting
        if (TargetCustomerSegments?.Any() == true && !string.IsNullOrWhiteSpace(customerSegment))
        {
            if (!TargetCustomerSegments.Contains(customerSegment.ToLowerInvariant()))
                return false;
        }

        return true;
    }

    public Money CalculateDiscount(Money orderAmount, int quantity)
    {
        if (!CanBeUsed() || !DiscountType.HasValue || !DiscountValue.HasValue)
            return Money.Zero(orderAmount.Currency);

        // Check minimum order amount
        if (MinOrderAmount != null && orderAmount < MinOrderAmount)
            return Money.Zero(orderAmount.Currency);

        Money discount = DiscountType.Value switch
        {
            Enums.DiscountType.FixedAmount => new Money(DiscountValue.Value, orderAmount.Currency),
            Enums.DiscountType.Percentage => orderAmount * (DiscountValue.Value / 100m),
            _ => Money.Zero(orderAmount.Currency)
        };

        // Apply maximum discount limit
        if (MaxDiscountAmount != null && discount > MaxDiscountAmount)
            discount = MaxDiscountAmount;

        // Ensure discount doesn't exceed order amount
        if (discount > orderAmount)
            discount = orderAmount;

        return discount;
    }

    public bool HasUsageLimit()
    {
        return Type == PricingRuleType.DiscountCode && 
               (IsSingleUse == true || MaxUses.HasValue);
    }

    public int? GetRemainingUses()
    {
        if (Type != PricingRuleType.DiscountCode)
            return null;
        
        if (IsSingleUse == true)
            return CurrentUses > 0 ? 0 : 1;
        
        if (MaxUses.HasValue)
            return Math.Max(0, MaxUses.Value - CurrentUses);
        
        return null; // Unlimited
    }
}
