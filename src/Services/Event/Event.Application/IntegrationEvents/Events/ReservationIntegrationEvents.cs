using Event.Application.Common.Models;

namespace Event.Application.IntegrationEvents.Events;

/// <summary>
/// Published when a reservation is created
/// </summary>
public record ReservationCreatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTime EventDate { get; init; }
    public DateTime ExpiresAt { get; init; }
    public List<ReservationItemDto> Items { get; init; } = new();
    public MoneyDto TotalAmount { get; init; } = null!;
    public List<Guid> SeatIds { get; init; } = new();

    public ReservationCreatedIntegrationEvent()
    {
        EventType = nameof(ReservationCreatedIntegrationEvent);
    }
}

/// <summary>
/// Published when a reservation is confirmed (payment successful)
/// </summary>
public record ReservationConfirmedIntegrationEvent : BaseIntegrationEvent
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTime EventDate { get; init; }
    public DateTime ConfirmedAt { get; init; }
    public List<ReservationItemDto> Items { get; init; } = new();
    public MoneyDto TotalAmount { get; init; } = null!;
    public List<Guid> SeatIds { get; init; } = new();
    public string? PaymentReference { get; init; }

    public ReservationConfirmedIntegrationEvent()
    {
        EventType = nameof(ReservationConfirmedIntegrationEvent);
    }
}

/// <summary>
/// Published when a reservation is cancelled
/// </summary>
public record ReservationCancelledIntegrationEvent : BaseIntegrationEvent
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CancelledAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<Guid> ReleasedSeatIds { get; init; } = new();
    public bool WasConfirmed { get; init; }

    public ReservationCancelledIntegrationEvent()
    {
        EventType = nameof(ReservationCancelledIntegrationEvent);
    }
}

/// <summary>
/// Published when a reservation expires
/// </summary>
public record ReservationExpiredIntegrationEvent : BaseIntegrationEvent
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime ExpiredAt { get; init; }
    public List<Guid> ReleasedSeatIds { get; init; } = new();
    public MoneyDto LostAmount { get; init; } = null!;

    public ReservationExpiredIntegrationEvent()
    {
        EventType = nameof(ReservationExpiredIntegrationEvent);
    }
}

/// <summary>
/// Published when a reservation is extended
/// </summary>
public record ReservationExtendedIntegrationEvent : BaseIntegrationEvent
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime PreviousExpiryTime { get; init; }
    public DateTime NewExpiryTime { get; init; }
    public TimeSpan ExtensionDuration { get; init; }
    public int ExtensionCount { get; init; }

    public ReservationExtendedIntegrationEvent()
    {
        EventType = nameof(ReservationExtendedIntegrationEvent);
    }
}

/// <summary>
/// Supporting DTOs
/// </summary>
public record ReservationItemDto
{
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
    public MoneyDto TotalPrice { get; init; } = null!;
    public List<Guid>? SeatIds { get; init; }
}
