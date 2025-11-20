using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents an individual seat in a venue
/// </summary>
public class Seat : BaseEntity
{
    // Basic Properties
    public Guid VenueId { get; private set; }
    public SeatPosition Position { get; private set; } = null!;
    public SeatStatus Status { get; private set; }
    
    // Seat Attributes
    public bool IsAccessible { get; private set; }
    public bool HasRestrictedView { get; private set; }
    public string? PriceCategory { get; private set; }
    public string? Notes { get; private set; }
    
    // Current Allocation (Event Service only manages ticket type allocations)
    public Guid? AllocatedToTicketTypeId { get; private set; }
    
    // Navigation Properties
    public Venue Venue { get; private set; } = null!;

    // For EF Core
    private Seat() { }

    public Seat(
        Guid venueId,
        SeatPosition position,
        bool isAccessible = false,
        bool hasRestrictedView = false,
        string? priceCategory = null)
    {
        VenueId = venueId;
        Position = position;
        Status = SeatStatus.Available;
        IsAccessible = isAccessible;
        HasRestrictedView = hasRestrictedView;
        PriceCategory = priceCategory?.Trim();
    }

    public void UpdateAttributes(bool isAccessible, bool hasRestrictedView, string? priceCategory, string? notes)
    {
        IsAccessible = isAccessible;
        HasRestrictedView = hasRestrictedView;
        PriceCategory = priceCategory?.Trim();
        Notes = notes?.Trim();
    }

    public void Hold()
    {
        if (Status != SeatStatus.Available)
            throw new SeatNotAvailableException(Id, Position.GetDisplayName());
        Status = SeatStatus.Held;
    }

    public void Sell()
    {
        if (Status != SeatStatus.Held)
            throw new EventDomainException($"Cannot sell a seat that is not held. Current status: {Status}");
        Status = SeatStatus.Sold;
    }

    public void Release()
    {
        if (Status != SeatStatus.Held)
            return; // Can only release a held seat

        Status = SeatStatus.Available;
    }

    public void Block()
    {
        Status = SeatStatus.Blocked;
        AllocatedToTicketTypeId = null;
    }

    public void Unblock()
    {
        if (Status == SeatStatus.Blocked)
        {
            Status = SeatStatus.Available;
        }
    }

    public void AllocateToTicketType(Guid ticketTypeId)
    {
        if (Status != SeatStatus.Available)
            throw new SeatNotAvailableException(Id, Position.GetDisplayName());

        AllocatedToTicketTypeId = ticketTypeId;
    }

    public void RemoveTicketTypeAllocation()
    {
        if (Status == SeatStatus.Available)
        {
            AllocatedToTicketTypeId = null;
        }
    }

    public bool IsAvailableForAllocation(Guid? ticketTypeId = null)
    {
        if (Status != SeatStatus.Available)
            return false;

        // If seat is allocated to a specific ticket type, check if it matches
        if (AllocatedToTicketTypeId.HasValue && ticketTypeId.HasValue)
        {
            return AllocatedToTicketTypeId.Value == ticketTypeId.Value;
        }

        // If no allocation or no ticket type specified, it's available
        return true;
    }

    public bool CanBeAllocated()
    {
        return Status == SeatStatus.Available;
    }

    public string GetStatusDescription()
    {
        return Status switch
        {
            SeatStatus.Available => "Available",
            SeatStatus.Blocked => "Unavailable",
            SeatStatus.Held => "Held",
            SeatStatus.Sold => "Sold",
            _ => "Unknown"
        };
    }
}
