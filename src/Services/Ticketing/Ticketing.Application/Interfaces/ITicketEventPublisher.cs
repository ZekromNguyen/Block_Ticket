using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces;

public interface ITicketEventPublisher
{
    Task PublishTicketPurchasedAsync(Ticket ticket, CancellationToken cancellationToken);

    Task PublishMintTicketAsync(Ticket ticket, string userWalletAddress, CancellationToken cancellationToken);

    Task PublishTicketRefundedAsync(Ticket ticket, decimal amount, string reason, CancellationToken cancellationToken);

    Task PublishTicketTransferredAsync(Ticket ticket, Guid fromUserId, Guid toUserId, decimal price, CancellationToken cancellationToken);

    Task PublishTicketListedForResaleAsync(Ticket ticket, decimal price, CancellationToken cancellationToken);

    Task PublishResaleListingCancelledAsync(Ticket ticket, string reason, CancellationToken cancellationToken);

    Task PublishWaitingListOfferAsync(WaitingListEntry entry, CancellationToken cancellationToken);

    Task PublishBurnTicketAsync(Ticket ticket, string userWalletAddress, string reason, CancellationToken cancellationToken);

    Task PublishRetryMintAsync(Ticket ticket, string userWalletAddress, string requestedBy, string reason, CancellationToken cancellationToken);

    Task PublishReservationReleasedAsync(Reservation reservation, CancellationToken cancellationToken);

    Task PublishTicketsRestockedAsync(Ticket ticket, string reason, CancellationToken cancellationToken);
}
