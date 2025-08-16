using MediatR;
using Shared.Common.Models;

namespace Event.Application.Common.Interfaces;

/// <summary>
/// Base interface for domain event handlers
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent, INotification
{
}

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent;

    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for current user context
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    string? RequestId { get; }
    string? CorrelationId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

/// <summary>
/// Interface for date/time operations
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }
}

/// <summary>
/// Interface for external notification services
/// </summary>
public interface INotificationService
{
    Task SendEventNotificationAsync(Guid eventId, string eventName, string notificationType, object data, CancellationToken cancellationToken = default);
    Task SendReservationNotificationAsync(Guid reservationId, Guid userId, string notificationType, object data, CancellationToken cancellationToken = default);
    Task SendInventoryAlertAsync(Guid eventId, string alertType, object data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for integration events (external messaging)
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishEventCreatedAsync(Guid eventId, string eventName, Guid promoterId, DateTime eventDate, Guid venueId, CancellationToken cancellationToken = default);
    Task PublishEventPublishedAsync(Guid eventId, string eventName, DateTime publishedAt, DateTime eventDate, CancellationToken cancellationToken = default);
    Task PublishEventCancelledAsync(Guid eventId, string eventName, DateTime cancelledAt, string reason, CancellationToken cancellationToken = default);
    Task PublishInventoryChangedAsync(Guid eventId, Guid? ticketTypeId, int previousQuantity, int newQuantity, string reason, CancellationToken cancellationToken = default);
    Task PublishReservationCreatedAsync(Guid reservationId, Guid eventId, Guid userId, List<Guid> seatIds, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task PublishReservationConfirmedAsync(Guid reservationId, Guid eventId, Guid userId, List<Guid> seatIds, decimal totalAmount, CancellationToken cancellationToken = default);
}
