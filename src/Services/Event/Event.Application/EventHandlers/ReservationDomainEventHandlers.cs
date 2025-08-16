using Event.Application.Common.Interfaces;
using Event.Domain.Events;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Event.Application.EventHandlers;

/// <summary>
/// Handler for ReservationCreated domain event
/// </summary>
public class ReservationCreatedDomainEventHandler : IDomainEventHandler<ReservationCreatedDomainEvent>
{
    private readonly ILogger<ReservationCreatedDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly INotificationService _notificationService;
    private readonly ISeatLockService _seatLockService;

    public ReservationCreatedDomainEventHandler(
        ILogger<ReservationCreatedDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        INotificationService notificationService,
        ISeatLockService seatLockService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _notificationService = notificationService;
        _seatLockService = seatLockService;
    }

    public async Task Handle(ReservationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationCreated domain event for Reservation {ReservationId}, Event {EventId}, User {UserId}", 
            notification.ReservationId, notification.EventId, notification.UserId);

        try
        {
            // Lock seats if this is a reserved seating event
            if (notification.SeatIds?.Any() == true)
            {
                var lockDuration = TimeSpan.FromMinutes(10); // Default reservation TTL
                await _seatLockService.TryLockSeatsAsync(
                    notification.SeatIds, 
                    notification.ReservationId, 
                    lockDuration, 
                    cancellationToken);
            }

            // Invalidate availability caches
            await InvalidateAvailabilityCaches(notification.EventId, cancellationToken);

            // Publish integration event
            await _integrationEventPublisher.PublishReservationCreatedAsync(
                notification.ReservationId,
                notification.EventId,
                notification.UserId,
                notification.SeatIds ?? new List<Guid>(),
                notification.ExpiresAt,
                cancellationToken);

            // Send notification to user
            await _notificationService.SendReservationNotificationAsync(
                notification.ReservationId,
                notification.UserId,
                "ReservationCreated",
                new { notification.EventId, notification.ExpiresAt, SeatCount = notification.SeatIds?.Count ?? 0 },
                cancellationToken);

            _logger.LogInformation("Successfully handled ReservationCreated domain event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ReservationCreated domain event for Reservation {ReservationId}", 
                notification.ReservationId);
            throw;
        }
    }

    private async Task InvalidateAvailabilityCaches(Guid eventId, CancellationToken cancellationToken)
    {
        // Invalidate availability cache
        await _cacheService.RemoveAsync($"event:availability:{eventId}", cancellationToken);
    }
}

/// <summary>
/// Handler for ReservationConfirmed domain event
/// </summary>
public class ReservationConfirmedDomainEventHandler : IDomainEventHandler<ReservationConfirmedDomainEvent>
{
    private readonly ILogger<ReservationConfirmedDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly INotificationService _notificationService;

    public ReservationConfirmedDomainEventHandler(
        ILogger<ReservationConfirmedDomainEventHandler> logger,
        ICacheService cacheService,
        IIntegrationEventPublisher integrationEventPublisher,
        INotificationService notificationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _integrationEventPublisher = integrationEventPublisher;
        _notificationService = notificationService;
    }

    public async Task Handle(ReservationConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationConfirmed domain event for Reservation {ReservationId}, Event {EventId}, User {UserId}", 
            notification.ReservationId, notification.EventId, notification.UserId);

        try
        {
            // Invalidate availability caches (confirmed reservations reduce availability)
            await InvalidateAvailabilityCaches(notification.EventId, cancellationToken);

            // Publish integration event
            await _integrationEventPublisher.PublishReservationConfirmedAsync(
                notification.ReservationId,
                notification.EventId,
                notification.UserId,
                notification.SeatIds ?? new List<Guid>(),
                notification.TotalAmount,
                cancellationToken);

            // Send confirmation notification to user
            await _notificationService.SendReservationNotificationAsync(
                notification.ReservationId,
                notification.UserId,
                "ReservationConfirmed",
                new { notification.EventId, notification.TotalAmount, SeatCount = notification.SeatIds?.Count ?? 0 },
                cancellationToken);

            _logger.LogInformation("Successfully handled ReservationConfirmed domain event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ReservationConfirmed domain event for Reservation {ReservationId}", 
                notification.ReservationId);
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

/// <summary>
/// Handler for ReservationExpired domain event
/// </summary>
public class ReservationExpiredDomainEventHandler : IDomainEventHandler<ReservationExpiredDomainEvent>
{
    private readonly ILogger<ReservationExpiredDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ISeatLockService _seatLockService;
    private readonly INotificationService _notificationService;

    public ReservationExpiredDomainEventHandler(
        ILogger<ReservationExpiredDomainEventHandler> logger,
        ICacheService cacheService,
        ISeatLockService seatLockService,
        INotificationService notificationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _seatLockService = seatLockService;
        _notificationService = notificationService;
    }

    public async Task Handle(ReservationExpiredDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationExpired domain event for Reservation {ReservationId}, Event {EventId}", 
            notification.ReservationId, notification.EventId);

        try
        {
            // Release seat locks if this was a reserved seating reservation
            if (notification.SeatIds?.Any() == true)
            {
                await _seatLockService.ReleaseSeatLocksAsync(notification.SeatIds, cancellationToken);
            }

            // Invalidate availability caches (expired reservations increase availability)
            await InvalidateAvailabilityCaches(notification.EventId, cancellationToken);

            // Send notification about expiration
            await _notificationService.SendInventoryAlertAsync(
                notification.EventId,
                "ReservationExpired",
                new { notification.ReservationId, SeatCount = notification.SeatIds?.Count ?? 0 },
                cancellationToken);

            _logger.LogInformation("Successfully handled ReservationExpired domain event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ReservationExpired domain event for Reservation {ReservationId}", 
                notification.ReservationId);
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

/// <summary>
/// Handler for ReservationCancelled domain event
/// </summary>
public class ReservationCancelledDomainEventHandler : IDomainEventHandler<ReservationCancelledDomainEvent>
{
    private readonly ILogger<ReservationCancelledDomainEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ISeatLockService _seatLockService;
    private readonly INotificationService _notificationService;

    public ReservationCancelledDomainEventHandler(
        ILogger<ReservationCancelledDomainEventHandler> logger,
        ICacheService cacheService,
        ISeatLockService seatLockService,
        INotificationService notificationService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _seatLockService = seatLockService;
        _notificationService = notificationService;
    }

    public async Task Handle(ReservationCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationCancelled domain event for Reservation {ReservationId}, Event {EventId}", 
            notification.ReservationId, notification.EventId);

        try
        {
            // Release seat locks if this was a reserved seating reservation
            if (notification.SeatIds?.Any() == true)
            {
                await _seatLockService.ReleaseSeatLocksAsync(notification.SeatIds, cancellationToken);
            }

            // Invalidate availability caches (cancelled reservations increase availability)
            await InvalidateAvailabilityCaches(notification.EventId, cancellationToken);

            // Send cancellation notification
            await _notificationService.SendReservationNotificationAsync(
                notification.ReservationId,
                notification.UserId,
                "ReservationCancelled",
                new { notification.EventId, notification.Reason, SeatCount = notification.SeatIds?.Count ?? 0 },
                cancellationToken);

            _logger.LogInformation("Successfully handled ReservationCancelled domain event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ReservationCancelled domain event for Reservation {ReservationId}", 
                notification.ReservationId);
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
