using Event.Application.Common.Models;

namespace Event.Application.IntegrationEvents.Events;

/// <summary>
/// Integration events consumed from other services
/// </summary>

/// <summary>
/// Consumed from Ticketing Service when payment is authorized
/// </summary>
public record OrderPaymentAuthorizedIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public MoneyDto AuthorizedAmount { get; init; } = null!;
    public string PaymentReference { get; init; } = string.Empty;
    public DateTime AuthorizedAt { get; init; }

    public OrderPaymentAuthorizedIntegrationEvent()
    {
        EventType = nameof(OrderPaymentAuthorizedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when payment is completed
/// </summary>
public record OrderPaymentCompletedIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public MoneyDto PaidAmount { get; init; } = null!;
    public string PaymentReference { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }

    public OrderPaymentCompletedIntegrationEvent()
    {
        EventType = nameof(OrderPaymentCompletedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when payment fails
/// </summary>
public record OrderPaymentFailedIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string FailureReason { get; init; } = string.Empty;
    public string PaymentReference { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }

    public OrderPaymentFailedIntegrationEvent()
    {
        EventType = nameof(OrderPaymentFailedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when an order is cancelled
/// </summary>
public record OrderCancelledIntegrationEvent : BaseIntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid? ReservationId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string CancellationReason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
    public List<Guid> TicketsToRestock { get; init; } = new();

    public OrderCancelledIntegrationEvent()
    {
        EventType = nameof(OrderCancelledIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when a refund is requested
/// </summary>
public record RefundRequestedIntegrationEvent : BaseIntegrationEvent
{
    public Guid RefundId { get; init; }
    public Guid OrderId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public MoneyDto RefundAmount { get; init; } = null!;
    public string RefundReason { get; init; } = string.Empty;
    public List<RefundTicketDto> TicketsToRefund { get; init; } = new();
    public DateTime RequestedAt { get; init; }

    public RefundRequestedIntegrationEvent()
    {
        EventType = nameof(RefundRequestedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when a refund is processed
/// </summary>
public record RefundProcessedIntegrationEvent : BaseIntegrationEvent
{
    public Guid RefundId { get; init; }
    public Guid OrderId { get; init; }
    public Guid EventId { get; init; }
    public MoneyDto RefundedAmount { get; init; } = null!;
    public List<RefundTicketDto> RefundedTickets { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
    public bool ShouldRestockTickets { get; init; } = true;

    public RefundProcessedIntegrationEvent()
    {
        EventType = nameof(RefundProcessedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service for resale events
/// </summary>
public record TicketResaleListedIntegrationEvent : BaseIntegrationEvent
{
    public Guid ResaleId { get; init; }
    public Guid OriginalOrderId { get; init; }
    public Guid EventId { get; init; }
    public Guid SellerId { get; init; }
    public List<ResaleTicketDto> TicketsForSale { get; init; } = new();
    public MoneyDto ResalePrice { get; init; } = null!;
    public DateTime ListedAt { get; init; }

    public TicketResaleListedIntegrationEvent()
    {
        EventType = nameof(TicketResaleListedIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Ticketing Service when resale tickets are sold
/// </summary>
public record TicketResaleSoldIntegrationEvent : BaseIntegrationEvent
{
    public Guid ResaleId { get; init; }
    public Guid EventId { get; init; }
    public Guid SellerId { get; init; }
    public Guid BuyerId { get; init; }
    public List<ResaleTicketDto> SoldTickets { get; init; } = new();
    public MoneyDto SalePrice { get; init; } = null!;
    public DateTime SoldAt { get; init; }

    public TicketResaleSoldIntegrationEvent()
    {
        EventType = nameof(TicketResaleSoldIntegrationEvent);
    }
}

/// <summary>
/// Consumed from Identity Service when user preferences change
/// </summary>
public record UserPreferencesUpdatedIntegrationEvent : BaseIntegrationEvent
{
    public Guid UserId { get; init; }
    public List<string> PreferredCategories { get; init; } = new();
    public string? PreferredLocation { get; init; }
    public MoneyDto? PreferredPriceRange { get; init; }
    public Dictionary<string, object> CustomPreferences { get; init; } = new();

    public UserPreferencesUpdatedIntegrationEvent()
    {
        EventType = nameof(UserPreferencesUpdatedIntegrationEvent);
    }
}

/// <summary>
/// Supporting DTOs
/// </summary>
public record RefundTicketDto
{
    public Guid TicketId { get; init; }
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public Guid? SeatId { get; init; }
    public string? SeatNumber { get; init; }
    public MoneyDto RefundAmount { get; init; } = null!;
}

public record ResaleTicketDto
{
    public Guid TicketId { get; init; }
    public Guid TicketTypeId { get; init; }
    public string TicketTypeName { get; init; } = string.Empty;
    public Guid? SeatId { get; init; }
    public string? SeatNumber { get; init; }
    public MoneyDto OriginalPrice { get; init; } = null!;
    public MoneyDto ResalePrice { get; init; } = null!;
}
