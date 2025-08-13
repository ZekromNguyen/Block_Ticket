using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents an allocation of tickets/seats for specific purposes
/// </summary>
public class Allocation : BaseAuditableEntity
{
    private readonly List<Guid> _allocatedSeatIds = new();

    // Basic Properties
    public Guid EventId { get; private set; }
    public Guid? TicketTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AllocationType Type { get; private set; }
    
    // Capacity
    public int TotalQuantity { get; private set; }
    public int AllocatedQuantity { get; private set; }
    public int UsedQuantity { get; private set; }
    
    // Access Control
    public string? AccessCode { get; private set; }
    public List<string>? AllowedUserIds { get; private set; }
    public List<string>? AllowedEmailDomains { get; private set; }
    
    // Timing
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableUntil { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    
    // Status
    public bool IsActive { get; private set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    
    // Navigation Properties
    public IReadOnlyCollection<Guid> AllocatedSeatIds => _allocatedSeatIds.AsReadOnly();
    public EventAggregate Event { get; private set; } = null!;
    public TicketType? TicketType { get; private set; }

    // For EF Core
    private Allocation() { }

    public Allocation(
        Guid eventId,
        string name,
        AllocationType type,
        int totalQuantity,
        Guid? ticketTypeId = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InventoryDomainException("Allocation name cannot be empty");
        
        if (totalQuantity <= 0)
            throw new InventoryDomainException("Allocation quantity must be greater than zero");

        EventId = eventId;
        TicketTypeId = ticketTypeId;
        Name = name.Trim();
        Description = description?.Trim();
        Type = type;
        TotalQuantity = totalQuantity;
        AllocatedQuantity = 0;
        UsedQuantity = 0;
        IsActive = true;
    }

    public void UpdateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InventoryDomainException("Allocation name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void SetAccessCode(string accessCode)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
            throw new InventoryDomainException("Access code cannot be empty");

        AccessCode = accessCode.Trim().ToUpperInvariant();
    }

    public void RemoveAccessCode()
    {
        AccessCode = null;
    }

    public void SetAllowedUsers(List<string> userIds)
    {
        AllowedUserIds = userIds?.Any() == true ? userIds : null;
    }

    public void SetAllowedEmailDomains(List<string> domains)
    {
        AllowedEmailDomains = domains?.Any() == true ? 
            domains.Select(d => d.Trim().ToLowerInvariant()).ToList() : null;
    }

    public void SetAvailabilityWindow(DateTime? availableFrom, DateTime? availableUntil)
    {
        if (availableFrom.HasValue && availableUntil.HasValue && availableUntil.Value <= availableFrom.Value)
            throw new InventoryDomainException("Available until date must be after available from date");

        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
    }

    public void SetExpiration(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new InventoryDomainException("Expiration date must be in the future");

        ExpiresAt = expiresAt;
    }

    public void RemoveExpiration()
    {
        ExpiresAt = null;
    }

    public void AllocateSeats(List<Guid> seatIds)
    {
        if (!IsActive)
            throw new InventoryDomainException("Cannot allocate seats to inactive allocation");
        
        if (IsExpired)
            throw new InventoryDomainException("Cannot allocate seats to expired allocation");

        var newAllocations = seatIds.Where(id => !_allocatedSeatIds.Contains(id)).ToList();
        
        if (AllocatedQuantity + newAllocations.Count > TotalQuantity)
            throw new CapacityExceededException(newAllocations.Count, TotalQuantity - AllocatedQuantity);

        foreach (var seatId in newAllocations)
        {
            _allocatedSeatIds.Add(seatId);
        }

        AllocatedQuantity = _allocatedSeatIds.Count;
    }

    public void DeallocateSeats(List<Guid> seatIds)
    {
        foreach (var seatId in seatIds)
        {
            _allocatedSeatIds.Remove(seatId);
        }

        AllocatedQuantity = _allocatedSeatIds.Count;
    }

    public void ClearSeatAllocations()
    {
        _allocatedSeatIds.Clear();
        AllocatedQuantity = 0;
    }

    public void UseQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Usage quantity must be greater than zero");
        
        if (UsedQuantity + quantity > AllocatedQuantity)
            throw new InventoryDomainException("Cannot use more than allocated quantity");

        UsedQuantity += quantity;
    }

    public void ReleaseUsedQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Release quantity must be greater than zero");
        
        UsedQuantity = Math.Max(0, UsedQuantity - quantity);
    }

    public void AdjustTotalQuantity(int newTotalQuantity)
    {
        if (newTotalQuantity < UsedQuantity)
            throw new InventoryDomainException($"Cannot reduce total quantity below used quantity ({UsedQuantity})");
        
        if (newTotalQuantity < AllocatedQuantity)
        {
            // Need to deallocate some seats
            var excessSeats = AllocatedQuantity - newTotalQuantity;
            var seatsToRemove = _allocatedSeatIds.Take(excessSeats).ToList();
            DeallocateSeats(seatsToRemove);
        }

        TotalQuantity = newTotalQuantity;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Expire()
    {
        if (!IsExpired)
        {
            ExpiresAt = DateTime.UtcNow;
            AddDomainEvent(new HoldExpiredDomainEvent(EventId, Id, GetAvailableQuantity(), DateTime.UtcNow));
        }
    }

    public bool IsAvailableNow()
    {
        if (!IsActive || IsExpired)
            return false;

        var now = DateTime.UtcNow;
        
        if (AvailableFrom.HasValue && now < AvailableFrom.Value)
            return false;
        
        if (AvailableUntil.HasValue && now > AvailableUntil.Value)
            return false;

        return true;
    }

    public bool CanAccess(string? accessCode, string? userId, string? userEmail)
    {
        if (!IsAvailableNow())
            return false;

        // Check access code
        if (!string.IsNullOrWhiteSpace(AccessCode))
        {
            if (string.IsNullOrWhiteSpace(accessCode) || 
                !AccessCode.Equals(accessCode.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Check allowed users
        if (AllowedUserIds?.Any() == true && !string.IsNullOrWhiteSpace(userId))
        {
            if (!AllowedUserIds.Contains(userId))
                return false;
        }

        // Check allowed email domains
        if (AllowedEmailDomains?.Any() == true && !string.IsNullOrWhiteSpace(userEmail))
        {
            var emailDomain = userEmail.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(emailDomain) || !AllowedEmailDomains.Contains(emailDomain))
                return false;
        }

        return true;
    }

    public int GetAvailableQuantity()
    {
        return AllocatedQuantity - UsedQuantity;
    }

    public int GetRemainingCapacity()
    {
        return TotalQuantity - AllocatedQuantity;
    }

    public decimal GetUtilizationPercentage()
    {
        return TotalQuantity == 0 ? 0 : (decimal)UsedQuantity / TotalQuantity * 100;
    }

    public bool HasAvailableQuantity(int requestedQuantity = 1)
    {
        return IsAvailableNow() && GetAvailableQuantity() >= requestedQuantity;
    }

    public bool IsSeatAllocated(Guid seatId)
    {
        return _allocatedSeatIds.Contains(seatId);
    }

    public TimeSpan? GetRemainingTime()
    {
        if (!ExpiresAt.HasValue)
            return null;
        
        var remaining = ExpiresAt.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
