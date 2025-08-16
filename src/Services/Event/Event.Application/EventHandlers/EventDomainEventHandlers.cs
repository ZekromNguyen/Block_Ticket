using Event.Application.Common.Interfaces;
using Event.Domain.Events;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Event.Application.EventHandlers;

/// <summary>
/// Handler for EventCreated domain event
/// </summary>
public class EventCreatedDomainEventHandler : IDomainEventHandler<EventCreatedDomainEvent>
{
    private readonly ILogger<EventCreatedDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly INotificationService _notificationService;

    public EventCreatedDomainEventHandler(
        ILogger<EventCreatedDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        INotificationService notificationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _notificationService = notificationService;
    }

    public async Task Handle(EventCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventCreated domain event for Event {EventId}: {EventName}", 
            notification.EventId, notification.EventName);

        try
        {
            // Invalidate related caches
            await InvalidateCaches(notification.EventId, notification.PromoterId, cancellationToken);

            // Publish integration event for external services
            await _integrationEventPublisher.PublishEventCreatedAsync(
                notification.EventId,
                notification.EventName,
                notification.PromoterId,
                notification.EventDate,
                notification.VenueId,
                cancellationToken);

            // Send notifications
            await _notificationService.SendEventNotificationAsync(
                notification.EventId,
                notification.EventName,
                "EventCreated",
                new { notification.PromoterId, notification.EventDate, notification.VenueId },
                cancellationToken);

            _logger.LogInformation("Successfully handled EventCreated domain event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling EventCreated domain event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }

    private async Task InvalidateCaches(Guid eventId, Guid promoterId, CancellationToken cancellationToken)
    {
        // Invalidate event catalog cache
        await _cacheService.RemoveByPatternAsync($"event:catalog:{eventId}:*", cancellationToken);
        
        // Invalidate promoter events cache
        await _cacheService.RemoveByPatternAsync($"promoter:events:{promoterId}:*", cancellationToken);
        
        // Invalidate search results cache
        await _cacheService.RemoveByPatternAsync("search:results:*", cancellationToken);
    }
}

/// <summary>
/// Handler for EventPublished domain event
/// </summary>
public class EventPublishedDomainEventHandler : IDomainEventHandler<EventPublishedDomainEvent>
{
    private readonly ILogger<EventPublishedDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly INotificationService _notificationService;

    public EventPublishedDomainEventHandler(
        ILogger<EventPublishedDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        INotificationService notificationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _notificationService = notificationService;
    }

    public async Task Handle(EventPublishedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventPublished domain event for Event {EventId}: {EventName}", 
            notification.EventId, notification.EventName);

        try
        {
            // Invalidate caches to ensure fresh data
            await InvalidateCaches(notification.EventId, cancellationToken);

            // Publish integration event
            await _integrationEventPublisher.PublishEventPublishedAsync(
                notification.EventId,
                notification.EventName,
                notification.PublishedAt,
                notification.EventDate,
                cancellationToken);

            // Send notifications
            await _notificationService.SendEventNotificationAsync(
                notification.EventId,
                notification.EventName,
                "EventPublished",
                new { notification.PublishedAt, notification.EventDate },
                cancellationToken);

            _logger.LogInformation("Successfully handled EventPublished domain event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling EventPublished domain event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }

    private async Task InvalidateCaches(Guid eventId, CancellationToken cancellationToken)
    {
        // Invalidate event catalog cache
        await _cacheService.RemoveByPatternAsync($"event:catalog:{eventId}:*", cancellationToken);
        
        // Invalidate search results cache (published events appear in search)
        await _cacheService.RemoveByPatternAsync("search:results:*", cancellationToken);
        
        // Invalidate availability cache
        await _cacheService.RemoveAsync($"event:availability:{eventId}", cancellationToken);
    }
}

/// <summary>
/// Handler for EventCancelled domain event
/// </summary>
public class EventCancelledDomainEventHandler : IDomainEventHandler<EventCancelledDomainEvent>
{
    private readonly ILogger<EventCancelledDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly INotificationService _notificationService;
    private readonly IReservationRepository _reservationRepository;

    public EventCancelledDomainEventHandler(
        ILogger<EventCancelledDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        INotificationService notificationService,
        IReservationRepository reservationRepository)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _notificationService = notificationService;
        _reservationRepository = reservationRepository;
    }

    public async Task Handle(EventCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling EventCancelled domain event for Event {EventId}: {EventName}", 
            notification.EventId, notification.EventName);

        try
        {
            // Handle active reservations
            await HandleActiveReservations(notification.EventId, cancellationToken);

            // Invalidate all caches for this event
            await InvalidateAllEventCaches(notification.EventId, cancellationToken);

            // Publish integration event
            await _integrationEventPublisher.PublishEventCancelledAsync(
                notification.EventId,
                notification.EventName,
                notification.CancelledAt,
                notification.Reason,
                cancellationToken);

            // Send notifications
            await _notificationService.SendEventNotificationAsync(
                notification.EventId,
                notification.EventName,
                "EventCancelled",
                new { notification.CancelledAt, notification.Reason },
                cancellationToken);

            _logger.LogInformation("Successfully handled EventCancelled domain event for Event {EventId}", 
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling EventCancelled domain event for Event {EventId}", 
                notification.EventId);
            throw;
        }
    }

    private async Task HandleActiveReservations(Guid eventId, CancellationToken cancellationToken)
    {
        // Get all active reservations for the event
        var activeReservations = await _reservationRepository.GetByEventIdAsync(eventId, cancellationToken);
        
        // Cancel active reservations (this would trigger their own domain events)
        foreach (var reservation in activeReservations.Where(r => r.Status.Equals(Domain.Enums.ReservationStatus.Active)))
        {
            reservation.Cancel("Event cancelled");
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);
        }
    }

    private async Task InvalidateAllEventCaches(Guid eventId, CancellationToken cancellationToken)
    {
        // Invalidate all event-related caches
        await _cacheService.RemoveByPatternAsync($"event:*:{eventId}:*", cancellationToken);
        await _cacheService.RemoveByPatternAsync($"event:*:{eventId}", cancellationToken);
        
        // Invalidate search results cache
        await _cacheService.RemoveByPatternAsync("search:results:*", cancellationToken);
    }
}

/// <summary>
/// Handler for InventoryChanged domain event
/// </summary>
public class InventoryChangedDomainEventHandler : IDomainEventHandler<InventoryChangedDomainEvent>
{
    private readonly ILogger<InventoryChangedDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly IInventorySnapshotService _inventorySnapshotService;

    public InventoryChangedDomainEventHandler(
        ILogger<InventoryChangedDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        IInventorySnapshotService inventorySnapshotService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _inventorySnapshotService = inventorySnapshotService;
    }

    public async Task Handle(InventoryChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling InventoryChanged domain event for Event {EventId}, TicketType {TicketTypeId}: {PreviousQuantity} -> {NewQuantity}",
            notification.EventId, notification.TicketTypeId, notification.PreviousQuantity, notification.NewQuantity);

        try
        {
            // Invalidate availability caches
            await InvalidateAvailabilityCaches(notification.EventId, cancellationToken);

            // Update inventory snapshot ETag
            await _inventorySnapshotService.InvalidateInventoryETagAsync(notification.EventId, cancellationToken);

            // Publish integration event
            await _integrationEventPublisher.PublishInventoryChangedAsync(
                notification.EventId,
                notification.TicketTypeId,
                notification.PreviousQuantity,
                notification.NewQuantity,
                notification.Reason,
                cancellationToken);

            _logger.LogInformation("Successfully handled InventoryChanged domain event for Event {EventId}",
                notification.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling InventoryChanged domain event for Event {EventId}",
                notification.EventId);
            throw;
        }
    }

    private async Task InvalidateAvailabilityCaches(Guid eventId, CancellationToken cancellationToken)
    {
        // Invalidate availability cache
        await _cacheService.RemoveAsync($"event:availability:{eventId}", cancellationToken);

        // Invalidate search results cache (availability affects search results)
        await _cacheService.RemoveByPatternAsync("search:results:*", cancellationToken);
    }
}
