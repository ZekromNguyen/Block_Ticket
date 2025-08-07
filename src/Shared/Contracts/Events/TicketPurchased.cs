namespace Shared.Contracts.Events;

public record TicketPurchased(
    Guid TicketId,
    Guid EventId,
    Guid UserId,
    decimal Price,
    DateTime PurchasedAt
);
