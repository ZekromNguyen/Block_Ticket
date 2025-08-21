using Event.Application.Common.Interfaces;
using Event.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Reservations.EventHandlers;

/// <summary>
/// Domain event handler for ReservationCreated events
/// </summary>
public class ReservationCreatedDomainEventHandler : INotificationHandler<ReservationCreatedDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<ReservationCreatedDomainEventHandler> _logger;

    public ReservationCreatedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<ReservationCreatedDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReservationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationCreated domain event for Reservation {ReservationId}", 
            notification.ReservationId);

        try
        {
            await _integrationEventPublisher.PublishReservationCreatedAsync(
                notification.ReservationId,
                notification.EventId,
                notification.UserId,
                notification.SeatIds,
                notification.ExpiresAt,
                cancellationToken);

            _logger.LogInformation("Successfully published ReservationCreated integration event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ReservationCreated integration event for Reservation {ReservationId}", 
                notification.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for ReservationConfirmed events
/// </summary>
public class ReservationConfirmedDomainEventHandler : INotificationHandler<ReservationConfirmedDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<ReservationConfirmedDomainEventHandler> _logger;

    public ReservationConfirmedDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<ReservationConfirmedDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReservationConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationConfirmed domain event for Reservation {ReservationId}", 
            notification.ReservationId);

        try
        {
            await _integrationEventPublisher.PublishReservationConfirmedAsync(
                notification.ReservationId,
                notification.EventId,
                notification.UserId,
                notification.SeatIds,
                notification.TotalAmount,
                cancellationToken);

            _logger.LogInformation("Successfully published ReservationConfirmed integration event for Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ReservationConfirmed integration event for Reservation {ReservationId}", 
                notification.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for ReservationCancelled events
/// </summary>
public class ReservationCancelledDomainEventHandler : INotificationHandler<ReservationCancelledDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<ReservationCancelledDomainEventHandler> _logger;

    public ReservationCancelledDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<ReservationCancelledDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReservationCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationCancelled domain event for Reservation {ReservationId}", 
            notification.ReservationId);

        try
        {
            // Publish inventory changed event for the released seats
            await _integrationEventPublisher.PublishInventoryChangedAsync(
                notification.EventId,
                null, // Ticket type would need to be determined from the reservation
                0, // Previous quantity (would need to be calculated)
                notification.SeatIds.Count, // Released quantity
                $"Reservation {notification.ReservationId} cancelled: {notification.Reason}",
                cancellationToken);

            _logger.LogInformation("Successfully published inventory change for cancelled Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish inventory change for cancelled Reservation {ReservationId}", 
                notification.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Domain event handler for ReservationExpired events
/// </summary>
public class ReservationExpiredDomainEventHandler : INotificationHandler<ReservationExpiredDomainEvent>
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly ILogger<ReservationExpiredDomainEventHandler> _logger;

    public ReservationExpiredDomainEventHandler(
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<ReservationExpiredDomainEventHandler> logger)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _logger = logger;
    }

    public async Task Handle(ReservationExpiredDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReservationExpired domain event for Reservation {ReservationId}", 
            notification.ReservationId);

        try
        {
            // Publish inventory changed event for the released seats
            await _integrationEventPublisher.PublishInventoryChangedAsync(
                notification.EventId,
                null, // Ticket type would need to be determined from the reservation
                0, // Previous quantity (would need to be calculated)
                notification.SeatIds.Count, // Released quantity
                $"Reservation {notification.ReservationId} expired",
                cancellationToken);

            _logger.LogInformation("Successfully published inventory change for expired Reservation {ReservationId}", 
                notification.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish inventory change for expired Reservation {ReservationId}", 
                notification.ReservationId);
            throw;
        }
    }
}
