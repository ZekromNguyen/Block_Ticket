using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents a temporary reservation of seats/tickets
/// </summary>
public class Reservation : BaseAuditableEntity
{
    private readonly List<ReservationItem> _items = new();

    // Basic Properties
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string ReservationNumber { get; private set; } = string.Empty;
    public ReservationStatus Status { get; private set; }
    
    // Timing
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    
    // Pricing
    public Money TotalAmount { get; private set; } = null!;
    public string? DiscountCode { get; private set; }
    public Money? DiscountAmount { get; private set; }
    
    // Metadata
    public string? CustomerNotes { get; private set; }
    public string? InternalNotes { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Navigation Properties
    public IReadOnlyCollection<ReservationItem> Items => _items.AsReadOnly();
    public EventAggregate Event { get; private set; } = null!;

    // For EF Core
    private Reservation() { }

    public Reservation(
        Guid eventId,
        Guid userId,
        DateTime expiresAt,
        string currency = "USD")
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ReservationDomainException("Reservation expiration must be in the future");

        EventId = eventId;
        UserId = userId;
        ExpiresAt = expiresAt;
        Status = ReservationStatus.Active;
        ReservationNumber = GenerateReservationNumber();
        TotalAmount = Money.Zero(currency);

        AddDomainEvent(new ReservationCreatedDomainEvent(
            Id, EventId, UserId, new List<Guid>(), ExpiresAt));
    }

    public void AddItem(Guid ticketTypeId, Guid? seatId, Money unitPrice, int quantity = 1)
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot modify non-active reservations");
        
        if (IsExpired())
            throw new ReservationDomainException("Cannot modify expired reservations");

        // Check if item already exists
        var existingItem = _items.FirstOrDefault(i => 
            i.TicketTypeId == ticketTypeId && i.SeatId == seatId);
        
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = new ReservationItem(ticketTypeId, seatId, unitPrice, quantity);
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public void RemoveItem(Guid ticketTypeId, Guid? seatId)
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot modify non-active reservations");

        var item = _items.FirstOrDefault(i => 
            i.TicketTypeId == ticketTypeId && i.SeatId == seatId);
        
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    public void UpdateItemQuantity(Guid ticketTypeId, Guid? seatId, int newQuantity)
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot modify non-active reservations");
        
        if (newQuantity <= 0)
        {
            RemoveItem(ticketTypeId, seatId);
            return;
        }

        var item = _items.FirstOrDefault(i => 
            i.TicketTypeId == ticketTypeId && i.SeatId == seatId);
        
        if (item != null)
        {
            item.UpdateQuantity(newQuantity);
            RecalculateTotal();
        }
    }

    public void ApplyDiscount(string discountCode, Money discountAmount)
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot modify non-active reservations");
        
        if (discountAmount.Currency != TotalAmount.Currency)
            throw new ReservationDomainException("Discount currency must match reservation currency");

        DiscountCode = discountCode;
        DiscountAmount = discountAmount;
        RecalculateTotal();
    }

    public void RemoveDiscount()
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot modify non-active reservations");

        DiscountCode = null;
        DiscountAmount = null;
        RecalculateTotal();
    }

    public void ExtendExpiration(TimeSpan additionalTime)
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Cannot extend non-active reservations");

        if (IsExpired())
            throw new ReservationDomainException("Cannot extend expired reservations");

        ExpiresAt = ExpiresAt.Add(additionalTime);
    }

    /// <summary>
    /// Alias for ExtendExpiration for backward compatibility
    /// </summary>
    public void ExtendExpiry(TimeSpan additionalTime)
    {
        ExtendExpiration(additionalTime);
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Active)
            throw new ReservationDomainException("Can only confirm active reservations");

        if (IsExpired())
            throw new ReservationDomainException("Cannot confirm expired reservations");

        if (!_items.Any())
            throw new ReservationDomainException("Cannot confirm empty reservations");

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;

        var seatIds = _items.Where(i => i.SeatId.HasValue)
                           .Select(i => i.SeatId!.Value)
                           .ToList();

        AddDomainEvent(new ReservationConfirmedDomainEvent(
            Id, EventId, UserId, seatIds, TotalAmount.Amount));
    }

    public void Confirm(string paymentReference)
    {
        Confirm(); // Call the parameterless version
        // Store payment reference if needed
        // PaymentReference = paymentReference; // Add this property if needed
    }

    public void Cancel(string reason)
    {
        if (Status == ReservationStatus.Cancelled)
            return; // Already cancelled

        if (Status == ReservationStatus.Confirmed)
            throw new ReservationDomainException("Cannot cancel confirmed reservations");

        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        var seatIds = _items.Where(i => i.SeatId.HasValue)
                           .Select(i => i.SeatId!.Value)
                           .ToList();

        AddDomainEvent(new ReservationCancelledDomainEvent(Id, EventId, UserId, seatIds, reason));
    }

    public void Expire()
    {
        if (Status != ReservationStatus.Active)
            return; // Already processed

        Status = ReservationStatus.Expired;
        CancellationReason = "Reservation expired";

        var seatIds = _items.Where(i => i.SeatId.HasValue)
                           .Select(i => i.SeatId!.Value)
                           .ToList();

        AddDomainEvent(new ReservationExpiredDomainEvent(Id, EventId, seatIds, DateTime.UtcNow));
    }

    public void SetCustomerNotes(string? notes)
    {
        CustomerNotes = notes?.Trim();
    }

    public void SetInternalNotes(string? notes)
    {
        InternalNotes = notes?.Trim();
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    public TimeSpan GetRemainingTime()
    {
        if (Status != ReservationStatus.Active)
            return TimeSpan.Zero;
        
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public int GetTotalQuantity()
    {
        return _items.Sum(i => i.Quantity);
    }

    public List<Guid> GetSeatIds()
    {
        return _items.Where(i => i.SeatId.HasValue)
                    .Select(i => i.SeatId!.Value)
                    .ToList();
    }

    public Money GetSubtotal()
    {
        return _items.Aggregate(Money.Zero(TotalAmount.Currency), 
            (sum, item) => sum + (item.UnitPrice * item.Quantity));
    }

    public Money GetFinalTotal()
    {
        var subtotal = GetSubtotal();
        return DiscountAmount != null ? subtotal - DiscountAmount : subtotal;
    }

    private void RecalculateTotal()
    {
        TotalAmount = GetFinalTotal();
    }

    private static string GenerateReservationNumber()
    {
        // Generate a unique reservation number
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random().Next(1000, 9999);
        return $"RES{timestamp}{random}";
    }
}

/// <summary>
/// Represents an item within a reservation
/// </summary>
public class ReservationItem : BaseEntity
{
    public Guid TicketTypeId { get; private set; }
    public Guid? SeatId { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public int Quantity { get; private set; }

    // For EF Core
    private ReservationItem() { }

    public ReservationItem(Guid ticketTypeId, Guid? seatId, Money unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new ReservationDomainException("Quantity must be greater than zero");

        TicketTypeId = ticketTypeId;
        SeatId = seatId;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ReservationDomainException("Quantity must be greater than zero");

        Quantity = newQuantity;
    }

    public Money GetTotalPrice()
    {
        return UnitPrice * Quantity;
    }
}

/// <summary>
/// Represents the status of a reservation
/// </summary>
public enum ReservationStatus
{
    Active = 0,
    Confirmed = 1,
    Expired = 2,
    Cancelled = 3
}
