using MediatR;
using Shared.Common.Models;

namespace Event.Domain.Events;

/// <summary>
/// Domain event raised when an event is created
/// </summary>
public record EventCreatedDomainEvent(
    Guid EventId,
    string EventName,
    Guid PromoterId,
    DateTime EventDate,
    Guid VenueId) : IDomainEvent, INotification
{
    // Alias for backward compatibility
    public string Title => EventName;
}

/// <summary>
/// Domain event raised when an event is updated
/// </summary>
public record EventUpdatedDomainEvent(
    Guid EventId,
    string EventName,
    Dictionary<string, object> Changes) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an event is published
/// </summary>
public record EventPublishedDomainEvent(
    Guid EventId,
    string EventName,
    DateTime PublishedAt,
    DateTime EventDate) : IDomainEvent, INotification
{
    // Alias for backward compatibility
    public string Title => EventName;
}

/// <summary>
/// Domain event raised when an event is cancelled
/// </summary>
public record EventCancelledDomainEvent(
    Guid EventId,
    string EventName,
    DateTime CancelledAt,
    string Reason) : IDomainEvent, INotification
{
    // Alias for backward compatibility
    public string Title => EventName;
}

/// <summary>
/// Domain event raised when an event goes on sale
/// </summary>
public record EventOnSaleDomainEvent(
    Guid EventId,
    string EventName,
    DateTime OnSaleDate) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an event is sold out
/// </summary>
public record EventSoldOutDomainEvent(
    Guid EventId,
    string EventName,
    DateTime SoldOutAt) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a venue is created
/// </summary>
public record VenueCreatedDomainEvent(
    Guid VenueId,
    string VenueName,
    string Address,
    int Capacity) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a venue is updated
/// </summary>
public record VenueUpdatedDomainEvent(
    Guid VenueId,
    string VenueName,
    Dictionary<string, object> Changes) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a seat map is imported
/// </summary>
public record SeatMapImportedDomainEvent(
    Guid VenueId,
    string VenueName,
    int TotalSeats,
    DateTime ImportedAt) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when inventory changes
/// </summary>
public record InventoryChangedDomainEvent(
    Guid EventId,
    Guid? TicketTypeId,
    int PreviousQuantity,
    int NewQuantity,
    string Reason) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a reservation is created
/// </summary>
public record ReservationCreatedDomainEvent(
    Guid ReservationId,
    Guid EventId,
    Guid UserId,
    List<Guid> SeatIds,
    DateTime ExpiresAt) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a reservation expires
/// </summary>
public record ReservationExpiredDomainEvent(
    Guid ReservationId,
    Guid EventId,
    List<Guid> SeatIds,
    DateTime ExpiredAt) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a reservation is confirmed
/// </summary>
public record ReservationConfirmedDomainEvent(
    Guid ReservationId,
    Guid EventId,
    Guid UserId,
    List<Guid> SeatIds,
    decimal TotalAmount) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a reservation is cancelled
/// </summary>
public record ReservationCancelledDomainEvent(
    Guid ReservationId,
    Guid EventId,
    Guid UserId,
    List<Guid> SeatIds,
    string Reason) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a pricing rule is created
/// </summary>
public record PricingRuleCreatedDomainEvent(
    Guid PricingRuleId,
    Guid EventId,
    string RuleType,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a pricing rule is updated
/// </summary>
public record PricingRuleUpdatedDomainEvent(
    Guid PricingRuleId,
    Guid EventId,
    Dictionary<string, object> Changes) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when tickets are restocked
/// </summary>
public record TicketsRestockedDomainEvent(
    Guid EventId,
    Guid? TicketTypeId,
    int Quantity,
    string Reason) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a return window opens
/// </summary>
public record ReturnWindowOpenedDomainEvent(
    Guid EventId,
    DateTime WindowOpensAt,
    DateTime WindowClosesAt) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a hold expires
/// </summary>
public record HoldExpiredDomainEvent(
    Guid EventId,
    Guid AllocationId,
    int ReleasedQuantity,
    DateTime ExpiredAt) : IDomainEvent, INotification;
