using MassTransit;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Messaging;

public sealed class TicketEventPublisher : ITicketEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TicketEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishTicketPurchasedAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(
            new TicketPurchased(ticket.Id, ticket.EventId, ticket.UserId, ticket.PricePaid, DateTime.UtcNow),
            cancellationToken);
    }

    public Task PublishMintTicketAsync(Ticket ticket, string userWalletAddress, CancellationToken cancellationToken)
    {
        var metadata = $"{{\"ticketNumber\":\"{ticket.TicketNumber}\",\"eventId\":\"{ticket.EventId}\",\"verificationCode\":\"{ticket.VerificationCode}\"}}";
        return _publishEndpoint.Publish(
            new MintTicketCommand(ticket.Id, ticket.EventId, userWalletAddress, ticket.PricePaid, metadata),
            cancellationToken);
    }
}
