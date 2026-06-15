using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces;

public interface ITicketEventPublisher
{
    Task PublishTicketPurchasedAsync(Ticket ticket, CancellationToken cancellationToken);

    Task PublishMintTicketAsync(Ticket ticket, string userWalletAddress, CancellationToken cancellationToken);
}
