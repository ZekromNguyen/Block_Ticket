using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents a type of ticket for an event
/// </summary>
public class TicketType : BaseAuditableEntity
{
    private readonly List<DateTimeRange> _onSaleWindows = new();

    // Basic Properties
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public Money? ServiceFee { get; private set; }
    public Money? TaxAmount { get; private set; }
    
    // Inventory
    public InventoryType InventoryType { get; private set; }
    public Capacity Capacity { get; private set; } = null!;
    
    // Purchase Constraints
    public int MinPurchaseQuantity { get; private set; } = 1;
    public int MaxPurchaseQuantity { get; private set; } = 10;
    public int MaxPerCustomer { get; private set; } = 10;
    
    // Visibility and Rules
    public bool IsVisible { get; private set; } = true;
    public bool IsResaleAllowed { get; private set; } = true;
    public bool RequiresApproval { get; private set; } = false;
    
    // On-Sale Windows
    public IReadOnlyCollection<DateTimeRange> OnSaleWindows => _onSaleWindows.AsReadOnly();
    
    // Navigation
    public EventAggregate Event { get; private set; } = null!;

    // For EF Core
    private TicketType() { }

    public TicketType(
        Guid eventId,
        string name,
        string code,
        Money basePrice,
        InventoryType inventoryType,
        int totalCapacity,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InventoryDomainException("Ticket type name cannot be empty");
        
        if (string.IsNullOrWhiteSpace(code))
            throw new InventoryDomainException("Ticket type code cannot be empty");
        
        if (totalCapacity <= 0)
            throw new InventoryDomainException("Ticket type capacity must be greater than zero");

        EventId = eventId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = description?.Trim();
        BasePrice = basePrice;
        InventoryType = inventoryType;
        Capacity = new Capacity(totalCapacity);
    }

    public void UpdateBasicInfo(string name, string? description, Money basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InventoryDomainException("Ticket type name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim();
        BasePrice = basePrice;
    }

    public void SetFees(Money? serviceFee, Money? taxAmount)
    {
        // Validate currency matches if fees are provided
        if (serviceFee != null && serviceFee.Currency != BasePrice.Currency)
            throw new InventoryDomainException("Service fee currency must match base price currency");
        
        if (taxAmount != null && taxAmount.Currency != BasePrice.Currency)
            throw new InventoryDomainException("Tax amount currency must match base price currency");

        ServiceFee = serviceFee;
        TaxAmount = taxAmount;
    }

    public void SetPurchaseConstraints(int minQuantity, int maxQuantity, int maxPerCustomer)
    {
        if (minQuantity <= 0)
            throw new InventoryDomainException("Minimum purchase quantity must be greater than zero");
        
        if (maxQuantity < minQuantity)
            throw new InventoryDomainException("Maximum purchase quantity must be greater than or equal to minimum");
        
        if (maxPerCustomer < maxQuantity)
            throw new InventoryDomainException("Max per customer must be greater than or equal to max purchase quantity");

        MinPurchaseQuantity = minQuantity;
        MaxPurchaseQuantity = maxQuantity;
        MaxPerCustomer = maxPerCustomer;
    }

    public void SetVisibility(bool isVisible)
    {
        IsVisible = isVisible;
    }

    public void SetResalePolicy(bool isResaleAllowed)
    {
        IsResaleAllowed = isResaleAllowed;
    }

    public void SetApprovalRequirement(bool requiresApproval)
    {
        RequiresApproval = requiresApproval;
    }

    public void AddOnSaleWindow(DateTime startDate, DateTime endDate, TimeZoneId timeZone)
    {
        var window = new DateTimeRange(startDate, endDate, timeZone);
        
        // Check for overlapping windows
        if (_onSaleWindows.Any(w => w.Overlaps(window)))
            throw new InventoryDomainException("On-sale windows cannot overlap");

        _onSaleWindows.Add(window);
    }

    public void RemoveOnSaleWindow(DateTime startDate, DateTime endDate, TimeZoneId timeZone)
    {
        var window = new DateTimeRange(startDate, endDate, timeZone);
        var existingWindow = _onSaleWindows.FirstOrDefault(w => 
            w.StartDate == window.StartDate && 
            w.EndDate == window.EndDate && 
            w.TimeZone.Value == window.TimeZone.Value);
        
        if (existingWindow != null)
        {
            _onSaleWindows.Remove(existingWindow);
        }
    }

    public void ClearOnSaleWindows()
    {
        _onSaleWindows.Clear();
    }

    public void ReserveCapacity(int quantity)
    {
        if (!Capacity.CanReserve(quantity))
            throw new CapacityExceededException(quantity, Capacity.Available);

        Capacity = Capacity.Reserve(quantity);
    }

    public void ReleaseCapacity(int quantity)
    {
        Capacity = Capacity.Release(quantity);
    }

    public void AdjustCapacity(int newTotalCapacity)
    {
        if (newTotalCapacity < Capacity.Reserved)
            throw new InventoryDomainException($"Cannot reduce capacity below reserved amount ({Capacity.Reserved})");

        var availableChange = newTotalCapacity - Capacity.Total;
        Capacity = new Capacity(newTotalCapacity);
        
        if (availableChange > 0)
        {
            Capacity = Capacity.Release(availableChange);
        }
    }

    public Money GetTotalPrice()
    {
        var total = BasePrice;
        
        if (ServiceFee != null)
            total += ServiceFee;
        
        if (TaxAmount != null)
            total += TaxAmount;
        
        return total;
    }

    public bool IsOnSaleNow()
    {
        var now = DateTime.UtcNow;
        return IsVisible && _onSaleWindows.Any(w => w.Contains(now));
    }

    public bool IsAvailable(int requestedQuantity = 1)
    {
        return IsVisible && 
               IsOnSaleNow() && 
               Capacity.CanReserve(requestedQuantity) &&
               requestedQuantity >= MinPurchaseQuantity &&
               requestedQuantity <= MaxPurchaseQuantity;
    }

    public DateTimeRange? GetCurrentOnSaleWindow()
    {
        var now = DateTime.UtcNow;
        return _onSaleWindows.FirstOrDefault(w => w.Contains(now));
    }

    public DateTimeRange? GetNextOnSaleWindow()
    {
        var now = DateTime.UtcNow;
        return _onSaleWindows
            .Where(w => w.StartDate > now)
            .OrderBy(w => w.StartDate)
            .FirstOrDefault();
    }

    public bool ValidatePurchaseQuantity(int quantity, int customerPreviousPurchases = 0)
    {
        return quantity >= MinPurchaseQuantity &&
               quantity <= MaxPurchaseQuantity &&
               (customerPreviousPurchases + quantity) <= MaxPerCustomer;
    }
}
