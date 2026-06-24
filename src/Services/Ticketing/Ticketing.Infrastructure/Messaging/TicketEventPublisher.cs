using System.Diagnostics;
using MassTransit;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;
using Shared.Common.Extensions;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Messaging;

public sealed class TicketEventPublisher : ITicketEventPublisher
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly IPublishEndpoint _publishEndpoint;

    public TicketEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishTicketPurchasedAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new TicketPurchased(ticket.Id, ticket.EventId, ticket.UserId, ticket.PricePaid, DateTime.UtcNow),
            cancellationToken);
    }

    public Task PublishMintTicketAsync(Ticket ticket, string userWalletAddress, CancellationToken cancellationToken)
    {
        var metadata = $"{{\"ticketNumber\":\"{ticket.TicketNumber}\",\"eventId\":\"{ticket.EventId}\",\"verificationCode\":\"{ticket.VerificationCode}\"}}";
        return PublishWithCorrelationIdAsync(
            new MintTicketCommand(ticket.Id, ticket.EventId, userWalletAddress, ticket.PricePaid, metadata),
            cancellationToken);
    }

    public Task PublishTicketRefundedAsync(Ticket ticket, decimal amount, string reason, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new TicketRefunded(ticket.Id, ticket.EventId, ticket.UserId, amount, reason, DateTime.UtcNow),
            cancellationToken);
    }

    public Task PublishTicketTransferredAsync(Ticket ticket, Guid fromUserId, Guid toUserId, decimal price, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new TicketTransferred(ticket.Id, ticket.EventId, fromUserId, toUserId, price, DateTime.UtcNow),
            cancellationToken);
    }

    public Task PublishTicketListedForResaleAsync(Ticket ticket, decimal price, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new TicketListedForResale(ticket.Id, ticket.UserId, ticket.EventId, price, DateTime.UtcNow),
            cancellationToken);
    }

    public Task PublishResaleListingCancelledAsync(Ticket ticket, string reason, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new ResaleListingCancelled(ticket.Id, ticket.UserId, reason, DateTime.UtcNow),
            cancellationToken);
    }

    public async Task PublishWaitingListOfferAsync(WaitingListEntry entry, CancellationToken cancellationToken)
    {
        var availableUntil = entry.OfferExpiresAt ?? DateTime.UtcNow.AddMinutes(10);
        await PublishWithCorrelationIdAsync(
            new YourTurnInWaitingList(entry.UserId, entry.EventId, availableUntil),
            cancellationToken);

        await PublishWithCorrelationIdAsync(
            new WaitingListOfferCreated(entry.Id, entry.UserId, entry.EventId, entry.TicketTypeId, availableUntil),
            cancellationToken);
    }

    public Task PublishBurnTicketAsync(Ticket ticket, string userWalletAddress, string reason, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new BurnTicketCommand(ticket.Id, userWalletAddress, reason),
            cancellationToken);
    }

    public Task PublishRetryMintAsync(Ticket ticket, string userWalletAddress, string requestedBy, string reason, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new RetryMintTicketCommand(ticket.Id, userWalletAddress, requestedBy, reason),
            cancellationToken);
    }

    public Task PublishReservationReleasedAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var item = reservation.Items.FirstOrDefault();
        if (item is null)
        {
            return Task.CompletedTask;
        }

        return PublishWithCorrelationIdAsync(
            new ReservationReleasedIntegrationEvent(
                reservation.Id,
                reservation.EventId,
                new List<Guid>(),
                item.TicketTypeId,
                reservation.Items.Sum(reservationItem => reservationItem.Quantity)),
            cancellationToken);
    }

    public Task PublishTicketsRestockedAsync(Ticket ticket, string reason, CancellationToken cancellationToken)
    {
        return PublishWithCorrelationIdAsync(
            new TicketsRestockedIntegrationEvent(ticket.EventId, ticket.TicketTypeId, 1, reason),
            cancellationToken);
    }

    /// <summary>
    /// Publishes a message and copies the current <c>correlation_id</c> OpenTelemetry
    /// tag onto the message header so downstream services receive the same id.
    /// </summary>
    private Task PublishWithCorrelationIdAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        return _publishEndpoint.Publish(message, ctx => StampCorrelationId(ctx), cancellationToken);
    }

    private static void StampCorrelationId(PublishContext context)
    {
        var correlationId = Activity.Current?.GetTagItem("correlation_id") as string;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            context.Headers.Set(CorrelationIdHeader, correlationId);
        }
    }
}