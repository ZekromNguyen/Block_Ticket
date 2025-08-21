using Event.Application.Common.Models;

namespace Event.Application.IntegrationEvents.Events;

/// <summary>
/// Base integration event
/// </summary>
public abstract record BaseIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
}

/// <summary>
/// Published when a new event is created
/// </summary>
public record EventCreatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public DateTime EventDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> Categories { get; init; } = new();
    public List<TicketTypeDto> TicketTypes { get; init; } = new();
    public VenueInfoDto Venue { get; init; } = null!;

    public EventCreatedIntegrationEvent()
    {
        EventType = nameof(EventCreatedIntegrationEvent);
    }
}

/// <summary>
/// Published when an event is published (made available to public)
/// </summary>
public record EventPublishedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public DateTime EventDate { get; init; }
    public DateTime PublishedAt { get; init; }
    public List<TicketTypeDto> AvailableTicketTypes { get; init; } = new();
    public VenueInfoDto Venue { get; init; } = null!;

    public EventPublishedIntegrationEvent()
    {
        EventType = nameof(EventPublishedIntegrationEvent);
    }
}

/// <summary>
/// Published when an event is cancelled
/// </summary>
public record EventCancelledIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public DateTime EventDate { get; init; }
    public DateTime CancelledAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<Guid> AffectedReservations { get; init; } = new();

    public EventCancelledIntegrationEvent()
    {
        EventType = nameof(EventCancelledIntegrationEvent);
    }
}

/// <summary>
/// Published when an event is updated
/// </summary>
public record EventUpdatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Dictionary<string, object> Changes { get; init; } = new();
    public DateTime UpdatedAt { get; init; }

    public EventUpdatedIntegrationEvent()
    {
        EventType = nameof(EventUpdatedIntegrationEvent);
    }
}

/// <summary>
/// Published when event inventory changes
/// </summary>
public record InventoryChangedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid? TicketTypeId { get; init; }
    public string? TicketTypeName { get; init; }
    public int PreviousQuantity { get; init; }
    public int NewQuantity { get; init; }
    public int QuantityChange { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty; // "Reserved", "Released", "Sold", "Restocked"

    public InventoryChangedIntegrationEvent()
    {
        EventType = nameof(InventoryChangedIntegrationEvent);
    }
}

/// <summary>
/// Published when tickets are restocked (returned/refunded)
/// </summary>
public record TicketsRestockedIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public List<RestockedTicketDto> RestockedTickets { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }

    public TicketsRestockedIntegrationEvent()
    {
        EventType = nameof(TicketsRestockedIntegrationEvent);
    }
}

/// <summary>
/// Published when an event sells out
/// </summary>
public record EventSoldOutIntegrationEvent : BaseIntegrationEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public DateTime SoldOutAt { get; init; }
    public List<Guid> SoldOutTicketTypes { get; init; } = new();

    public EventSoldOutIntegrationEvent()
    {
        EventType = nameof(EventSoldOutIntegrationEvent);
    }
}

/// <summary>
/// Supporting DTOs
/// </summary>
public record VenueInfoDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
}

public record RestockedTicketDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public List<Guid>? SeatIds { get; init; }
}
