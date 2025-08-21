using Event.Application.Common.Interfaces;
using Event.Application.IntegrationEvents.Events;
using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for refund requested events from Ticketing Service
/// </summary>
public class RefundRequestedConsumer : IConsumer<RefundRequestedIntegrationEvent>
{
    private readonly IEventRepository _eventRepository;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<RefundRequestedConsumer> _logger;

    public RefundRequestedConsumer(
        IEventRepository eventRepository,
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<RefundRequestedConsumer> logger)
    {
        _eventRepository = eventRepository;
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundRequestedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing RefundRequested for Order {OrderId}, Event {EventId}, Amount {Amount}", 
            message.OrderId, message.EventId, message.RefundAmount.Amount);

        try
        {
            // Get the event to validate refund policy
            var eventAggregate = await _eventRepository.GetByIdAsync(message.EventId, context.CancellationToken);
            if (eventAggregate == null)
            {
                _logger.LogWarning("Event {EventId} not found for refund request {RefundId}", 
                    message.EventId, message.RefundId);
                return;
            }

            // Validate refund eligibility based on event policies
            var isRefundEligible = ValidateRefundEligibility(eventAggregate, message);
            
            if (!isRefundEligible)
            {
                _logger.LogWarning("Refund request {RefundId} is not eligible based on event policies", 
                    message.RefundId);
                // Could publish a RefundRejected event here
                return;
            }

            // Log the refund request for audit purposes
            _logger.LogInformation("Refund request {RefundId} is eligible for processing", message.RefundId);

            // The actual refund processing would be handled by the Ticketing Service
            // This service just validates and tracks the request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process RefundRequested for RefundId {RefundId}", message.RefundId);
            throw;
        }
    }

    private static bool ValidateRefundEligibility(Domain.Entities.EventAggregate eventAggregate, RefundRequestedIntegrationEvent message)
    {
        // Check if event allows refunds
        if (!eventAggregate.AllowsRefunds())
        {
            return false;
        }

        // Check if we're within the refund window
        var refundCutoffDate = eventAggregate.EventDate.AddDays(-eventAggregate.RefundCutoffDays);
        if (DateTime.UtcNow > refundCutoffDate)
        {
            return false;
        }

        // Additional business rules could be checked here
        return true;
    }
}

/// <summary>
/// Consumer for refund processed events from Ticketing Service
/// </summary>
public class RefundProcessedConsumer : IConsumer<RefundProcessedIntegrationEvent>
{
    private readonly IEventRepository _eventRepository;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<RefundProcessedConsumer> _logger;

    public RefundProcessedConsumer(
        IEventRepository eventRepository,
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<RefundProcessedConsumer> logger)
    {
        _eventRepository = eventRepository;
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundProcessedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing RefundProcessed for Order {OrderId}, Event {EventId}, Amount {Amount}", 
            message.OrderId, message.EventId, message.RefundedAmount.Amount);

        try
        {
            if (message.ShouldRestockTickets && message.RefundedTickets.Any())
            {
                // Get the event
                var eventAggregate = await _eventRepository.GetByIdAsync(message.EventId, context.CancellationToken);
                if (eventAggregate == null)
                {
                    _logger.LogWarning("Event {EventId} not found for refund processing {RefundId}", 
                        message.EventId, message.RefundId);
                    return;
                }

                // Restock the tickets
                var restockedTickets = message.RefundedTickets.Select(ticket => new RestockedTicketDto
                {
                    TicketTypeId = ticket.TicketTypeId,
                    TicketTypeName = ticket.TicketTypeName,
                    Quantity = 1, // Each refunded ticket represents 1 quantity
                    SeatIds = ticket.SeatId.HasValue ? new List<Guid> { ticket.SeatId.Value } : null
                }).ToList();

                // Publish tickets restocked event
                await _integrationEventPublisher.PublishInventoryChangedAsync(
                    message.EventId,
                    null, // Will be determined by the specific ticket types
                    0, // Previous quantity would need to be calculated
                    restockedTickets.Sum(t => t.Quantity), // New quantity
                    $"Refund processed for Order {message.OrderId}",
                    context.CancellationToken);

                _logger.LogInformation("Restocked {Count} tickets for Event {EventId} due to refund processing", 
                    restockedTickets.Count, message.EventId);
            }

            _logger.LogInformation("Successfully processed RefundProcessed for RefundId {RefundId}", message.RefundId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process RefundProcessed for RefundId {RefundId}", message.RefundId);
            throw;
        }
    }
}
