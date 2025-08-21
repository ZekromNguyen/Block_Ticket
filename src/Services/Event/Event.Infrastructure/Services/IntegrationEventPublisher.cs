using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Application.IntegrationEvents.Events;
using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of IIntegrationEventPublisher using MassTransit
/// </summary>
public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IVenueRepository _venueRepository;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        IVenueRepository venueRepository,
        ILogger<IntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _venueRepository = venueRepository;
        _logger = logger;
    }

    public async Task PublishEventCreatedAsync(Guid eventId, string eventName, Guid promoterId, DateTime eventDate, Guid venueId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing EventCreated integration event for Event {EventId} - {EventName}", eventId, eventName);

        try
        {
            // Get venue information
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            var venueInfo = venue != null ? new VenueInfoDto
            {
                Id = venue.Id,
                Name = venue.Name,
                Address = venue.Address.GetFullAddress(),
                City = venue.Address.City,
                State = venue.Address.State,
                Country = venue.Address.Country,
                TotalCapacity = venue.TotalCapacity
            } : new VenueInfoDto { Id = venueId, Name = "Unknown Venue" };

            var integrationEvent = new EventCreatedIntegrationEvent
            {
                EventId = eventId,
                Title = eventName,
                PromoterId = promoterId,
                VenueId = venueId,
                EventDate = eventDate,
                Venue = venueInfo
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published EventCreated integration event for Event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventCreated integration event for Event {EventId}", eventId);
            throw;
        }
    }

    public async Task PublishEventPublishedAsync(Guid eventId, string eventName, DateTime publishedAt, DateTime eventDate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing EventPublished integration event for Event {EventId} - {EventName}", eventId, eventName);

        try
        {
            var integrationEvent = new EventPublishedIntegrationEvent
            {
                EventId = eventId,
                Title = eventName,
                EventDate = eventDate,
                PublishedAt = publishedAt
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published EventPublished integration event for Event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventPublished integration event for Event {EventId}", eventId);
            throw;
        }
    }

    public async Task PublishEventCancelledAsync(Guid eventId, string eventName, DateTime cancelledAt, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing EventCancelled integration event for Event {EventId} - {EventName}", eventId, eventName);

        try
        {
            var integrationEvent = new EventCancelledIntegrationEvent
            {
                EventId = eventId,
                Title = eventName,
                CancelledAt = cancelledAt,
                Reason = reason
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published EventCancelled integration event for Event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EventCancelled integration event for Event {EventId}", eventId);
            throw;
        }
    }

    public async Task PublishInventoryChangedAsync(Guid eventId, Guid? ticketTypeId, int previousQuantity, int newQuantity, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing InventoryChanged integration event for Event {EventId}", eventId);

        try
        {
            var integrationEvent = new InventoryChangedIntegrationEvent
            {
                EventId = eventId,
                TicketTypeId = ticketTypeId,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                QuantityChange = newQuantity - previousQuantity,
                Reason = reason,
                ChangeType = DetermineChangeType(previousQuantity, newQuantity, reason)
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published InventoryChanged integration event for Event {EventId}", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish InventoryChanged integration event for Event {EventId}", eventId);
            throw;
        }
    }

    public async Task PublishReservationCreatedAsync(Guid reservationId, Guid eventId, Guid userId, List<Guid> seatIds, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing ReservationCreated integration event for Reservation {ReservationId}", reservationId);

        try
        {
            var integrationEvent = new ReservationCreatedIntegrationEvent
            {
                ReservationId = reservationId,
                EventId = eventId,
                UserId = userId,
                ExpiresAt = expiresAt,
                SeatIds = seatIds
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published ReservationCreated integration event for Reservation {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ReservationCreated integration event for Reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task PublishReservationConfirmedAsync(Guid reservationId, Guid eventId, Guid userId, List<Guid> seatIds, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing ReservationConfirmed integration event for Reservation {ReservationId}", reservationId);

        try
        {
            var integrationEvent = new ReservationConfirmedIntegrationEvent
            {
                ReservationId = reservationId,
                EventId = eventId,
                UserId = userId,
                SeatIds = seatIds,
                TotalAmount = new MoneyDto { Amount = totalAmount, Currency = "USD" },
                ConfirmedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(integrationEvent, cancellationToken);
            _logger.LogInformation("Successfully published ReservationConfirmed integration event for Reservation {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ReservationConfirmed integration event for Reservation {ReservationId}", reservationId);
            throw;
        }
    }

    private static string DetermineChangeType(int previousQuantity, int newQuantity, string reason)
    {
        if (newQuantity > previousQuantity)
        {
            return reason.ToLowerInvariant() switch
            {
                var r when r.Contains("restock") => "Restocked",
                var r when r.Contains("release") => "Released",
                var r when r.Contains("cancel") => "Released",
                _ => "Increased"
            };
        }
        else if (newQuantity < previousQuantity)
        {
            return reason.ToLowerInvariant() switch
            {
                var r when r.Contains("reserve") => "Reserved",
                var r when r.Contains("sold") => "Sold",
                var r when r.Contains("hold") => "Reserved",
                _ => "Decreased"
            };
        }

        return "NoChange";
    }
}
