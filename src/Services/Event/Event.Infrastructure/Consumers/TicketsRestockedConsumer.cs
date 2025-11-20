using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using System.Linq;
using System.Threading.Tasks;

namespace Event.Infrastructure.Consumers;

public class TicketsRestockedConsumer : IConsumer<TicketsRestockedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TicketsRestockedConsumer> _logger;

    public TicketsRestockedConsumer(IUnitOfWork unitOfWork, ILogger<TicketsRestockedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketsRestockedIntegrationEvent> context)
    {
        _logger.LogInformation("TicketsRestockedIntegrationEvent received for event {EventId}, ticket type {TicketTypeId}", context.Message.EventId, context.Message.TicketTypeId);

        var eventAggregate = await _unitOfWork.Events.GetWithFullDetailsAsync(context.Message.EventId, context.CancellationToken);
        if (eventAggregate == null)
        {
            _logger.LogWarning("Event with ID {EventId} not found while processing tickets restock.", context.Message.EventId);
            return;
        }

        var ticketType = eventAggregate.TicketTypes.FirstOrDefault(tt => tt.Id == context.Message.TicketTypeId);
        if (ticketType == null)
        {
            _logger.LogWarning("Ticket type with ID {TicketTypeId} not found in event {EventId} for tickets restock.", context.Message.TicketTypeId, eventAggregate.Id);
            return;
        }

        ticketType.ReleaseCapacity(context.Message.Quantity);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Successfully processed TicketsRestockedIntegrationEvent for event {EventId}, ticket type {TicketTypeId}", context.Message.EventId, context.Message.TicketTypeId);
    }
}

