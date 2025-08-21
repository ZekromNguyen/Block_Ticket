using Event.Application.Common.Interfaces;
using Event.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.EventHandlers;

/// <summary>
/// Domain event handler for EventCreated events
/// </summary>
public class EventCreatedDomainEventHandler : INotificationHandler<EventCreatedDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<EventCreatedDomainEventHandler> _logger;

    public EventCreatedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<EventCreatedDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(EventCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventCreated domain event for Event {EventId}", notification.EventId);

        try
        {
            await _integrationEventPublisher.PublishEventCreatedAsync(
                notification.EventId,
                notification.Title,
                notification.PromoterId,
                notification.EventDate,
                notification.VenueId,
                cancellationToken);

            _logger.LogInformation("Successfully published EventCreated integration event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventCreated integration event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for EventPublished events
/// </summary>
public class EventPublishedDomainEventHandler : INotificationHandler<EventPublishedDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<EventPublishedDomainEventHandler> _logger;

    public EventPublishedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<EventPublishedDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(EventPublishedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventPublished domain event for Event {EventId}", notification.EventId);

        try
        {
            await _integrationEventPublisher.PublishEventPublishedAsync(
                notification.EventId,
                notification.Title,
                notification.PublishedAt,
                notification.EventDate,
                cancellationToken);

            _logger.LogInformation("Successfully published EventPublished integration event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventPublished integration event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for EventCancelled events
/// </summary>
public class EventCancelledDomainEventHandler : INotificationHandler<EventCancelledDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<EventCancelledDomainEventHandler> _logger;

    public EventCancelledDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<EventCancelledDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(EventCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventCancelled domain event for Event {EventId}", notification.EventId);

        try
        {
            await _integrationEventPublisher.PublishEventCancelledAsync(
                notification.EventId,
                notification.Title,
                notification.CancelledAt,
                notification.Reason,
                cancellationToken);

            _logger.LogInformation("Successfully published EventCancelled integration event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventCancelled integration event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for EventUpdated events
/// </summary>
public class EventUpdatedDomainEventHandler : INotificationHandler<EventUpdatedDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<EventUpdatedDomainEventHandler> _logger;

    public EventUpdatedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<EventUpdatedDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(EventUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventUpdated domain event for Event {EventId}", notification.EventId);

        try
        {
            // For now, we'll use a generic event updated publisher
            // In a full implementation, you might want to create a specific method for this
            _logger.LogInformation("Event {EventId} updated with changes: {Changes}", 
                notification.EventId, 
                string.Join(", ", notification.Changes.Keys));

            // Could publish a specific EventUpdated integration event here
            // await _integrationEventPublisher.PublishEventUpdatedAsync(...)

            _logger.LogInformation("Successfully handled EventUpdated domain event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle EventUpdated domain event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }
}
