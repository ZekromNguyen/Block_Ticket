using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.WaitingList.Commands;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Messaging;

public sealed class TicketsRestockedConsumer : IConsumer<TicketsRestockedIntegrationEvent>
{
    private static readonly TimeSpan OfferTtl = TimeSpan.FromMinutes(10);

    private readonly IMediator _mediator;
    private readonly ILogger<TicketsRestockedConsumer> _logger;

    public TicketsRestockedConsumer(IMediator mediator, ILogger<TicketsRestockedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketsRestockedIntegrationEvent> context)
    {
        for (var index = 0; index < Math.Max(1, context.Message.Quantity); index++)
        {
            var result = await _mediator.Send(
                new CreateWaitingListOfferCommand(new WaitingListOfferRequest(context.Message.EventId, context.Message.TicketTypeId, OfferTtl)),
                context.CancellationToken);

            if (!result.Succeeded)
            {
                _logger.LogInformation(
                    "No waiting-list offer created for event {EventId}, ticket type {TicketTypeId}: {Reason}",
                    context.Message.EventId,
                    context.Message.TicketTypeId,
                    result.Error);
                return;
            }
        }
    }
}

public sealed class ReservationReleasedConsumer : IConsumer<ReservationReleasedIntegrationEvent>, IConsumer<ReservationExpiredIntegrationEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ReservationReleasedConsumer(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task Consume(ConsumeContext<ReservationReleasedIntegrationEvent> context)
    {
        return PublishRestockedAsync(context.Message.EventId, context.Message.TicketTypeId, context.Message.Quantity, "Reservation released", context.CancellationToken);
    }

    public Task Consume(ConsumeContext<ReservationExpiredIntegrationEvent> context)
    {
        return PublishRestockedAsync(context.Message.EventId, context.Message.TicketTypeId, context.Message.Quantity, "Reservation expired", context.CancellationToken);
    }

    private Task PublishRestockedAsync(Guid eventId, Guid ticketTypeId, int quantity, string reason, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(new TicketsRestockedIntegrationEvent(eventId, ticketTypeId, quantity, reason), cancellationToken);
    }
}

public sealed class EventCancelledConsumer : IConsumer<EventCancelledIntegrationEvent>
{
    private readonly ITicketingRepository _repository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITicketEventPublisher _publisher;
    private readonly ILogger<EventCancelledConsumer> _logger;

    public EventCancelledConsumer(
        ITicketingRepository repository,
        IPaymentProvider paymentProvider,
        ITicketEventPublisher publisher,
        ILogger<EventCancelledConsumer> logger)
    {
        _repository = repository;
        _paymentProvider = paymentProvider;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventCancelledIntegrationEvent> context)
    {
        var tickets = await _repository.GetRefundableTicketsByEventAsync(context.Message.EventId, context.CancellationToken);
        foreach (var ticket in tickets)
        {
            if (ticket.Status == TicketStatus.Refunded)
            {
                continue;
            }

            var refund = await _paymentProvider.RefundPaymentAsync(ticket.Id, ticket.PricePaid, context.Message.Reason, context.CancellationToken);
            if (!refund.Succeeded)
            {
                _logger.LogWarning("Refund failed for ticket {TicketId}: {Reason}", ticket.Id, refund.Error);
                continue;
            }

            ticket.MarkRefunded(ticket.PricePaid, context.Message.Reason);
            await _publisher.PublishTicketRefundedAsync(ticket, ticket.PricePaid, context.Message.Reason, context.CancellationToken);
            await _publisher.PublishTicketsRestockedAsync(ticket, context.Message.Reason, context.CancellationToken);
        }

        await _repository.SaveChangesAsync(context.CancellationToken);
    }
}
