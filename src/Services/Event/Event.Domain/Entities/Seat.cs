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
    
    // Current Allocation
    public Guid? CurrentReservationId { get; private set; }
    public DateTime? ReservedUntil { get; private set; }
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

    public void Hold(Guid reservationId, DateTime holdUntil)
    {
        if (Status != SeatStatus.Available)
            throw new SeatNotAvailableException(Id, Position.GetDisplayName());
        
        if (holdUntil <= DateTime.UtcNow)
            throw new ReservationDomainException("Hold expiration must be in the future");

        Status = SeatStatus.Held;
        CurrentReservationId = reservationId;
        ReservedUntil = holdUntil;
    }

    public void Reserve(Guid reservationId, DateTime reserveUntil)
    {
        if (Status != SeatStatus.Available && Status != SeatStatus.Held)
            throw new SeatNotAvailableException(Id, Position.GetDisplayName());
        
        // If currently held, verify it's the same reservation
        if (Status == SeatStatus.Held && CurrentReservationId != reservationId)
            throw new ReservationDomainException("Seat is held by a different reservation");

        Status = SeatStatus.Reserved;
        CurrentReservationId = reservationId;
        ReservedUntil = reserveUntil;
    }

    public void Confirm(Guid reservationId)
    {
        if (Status != SeatStatus.Reserved)
            throw new ReservationDomainException("Can only confirm reserved seats");
        
        if (CurrentReservationId != reservationId)
            throw new ReservationDomainException("Seat is reserved by a different reservation");

        Status = SeatStatus.Confirmed;
        ReservedUntil = null; // No longer has expiration
    }

    public void Release()
    {
        if (Status == SeatStatus.Available || Status == SeatStatus.Blocked)
            return; // Already in desired state

        Status = SeatStatus.Available;
        CurrentReservationId = null;
        ReservedUntil = null;
        AllocatedToTicketTypeId = null;
    }

    public void Block()
    {
        if (Status == SeatStatus.Confirmed)
            throw new ReservationDomainException("Cannot block confirmed seats");

        Status = SeatStatus.Blocked;
        CurrentReservationId = null;
        ReservedUntil = null;
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

    public bool IsExpired()
    {
        return ReservedUntil.HasValue && ReservedUntil.Value <= DateTime.UtcNow;
    }

    public void CheckAndHandleExpiration()
    {
        if (IsExpired())
        {
            var previousStatus = Status;
            Release();
            
            // Could add domain event here for expired reservation
            if (previousStatus == SeatStatus.Held)
            {
                Status = SeatStatus.Expired;
            }
        }
    }

    public bool IsAvailableForReservation(Guid? ticketTypeId = null)
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

    public bool CanBeHeld()
    {
        return Status == SeatStatus.Available;
    }

    public bool CanBeReserved()
    {
        return Status == SeatStatus.Available || Status == SeatStatus.Held;
    }

    public bool CanBeConfirmed()
    {
        return Status == SeatStatus.Reserved && !IsExpired();
    }

    public bool CanBeReleased()
    {
        return Status != SeatStatus.Available && Status != SeatStatus.Blocked;
    }

    public TimeSpan? GetRemainingHoldTime()
    {
        if (!ReservedUntil.HasValue || Status == SeatStatus.Confirmed)
            return null;
        
        var remaining = ReservedUntil.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public string GetStatusDescription()
    {
        return Status switch
        {
            SeatStatus.Available => "Available",
            SeatStatus.Held => $"Held until {ReservedUntil:HH:mm}",
            SeatStatus.Reserved => $"Reserved until {ReservedUntil:HH:mm}",
            SeatStatus.Confirmed => "Sold",
            SeatStatus.Released => "Available",
            SeatStatus.Expired => "Available",
            SeatStatus.Blocked => "Unavailable",
            _ => "Unknown"
        };
    }
}
