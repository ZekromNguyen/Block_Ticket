namespace Shared.Contracts.Events;

public record TicketRefunded(
    Guid TicketId,
    Guid EventId,
    Guid UserId,
    decimal Amount,
    string Reason,
    DateTime RefundedAt
);

public record TicketTransferred(
    Guid TicketId,
    Guid EventId,
    Guid FromUserId,
    Guid ToUserId,
    decimal Price,
    DateTime TransferredAt
);

public record TicketListedForResale(
    Guid TicketId,
    Guid SellerUserId,
    Guid EventId,
    decimal Price,
    DateTime ListedAt
);

public record ResaleListingCancelled(
    Guid TicketId,
    Guid SellerUserId,
    string Reason,
    DateTime CancelledAt
);

public record WaitingListOfferCreated(
    Guid OfferId,
    Guid UserId,
    Guid EventId,
    Guid TicketTypeId,
    DateTime AvailableUntil
);
