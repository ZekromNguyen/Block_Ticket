using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Application.Features.Events.Queries.GetEvent;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.Commands.CancelEvent;

/// <summary>
/// Handler for CancelEventCommand
/// </summary>
public class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<CancelEventCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelEventCommandHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        ILogger<CancelEventCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventDto> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling event {EventId} with reason: {Reason}", 
            request.EventId, request.Reason);

        // Get the existing event
        var eventAggregate = await GetEventAggregate(request.EventId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventAggregate, request.ExpectedVersion);

        // Validate business rules for cancellation
        ValidateCancellationRules(eventAggregate);

        // Cancel the event
        eventAggregate.Cancel(request.Reason, _dateTimeProvider.UtcNow);

        // Handle active reservations
        await HandleActiveReservations(request.EventId, cancellationToken);

        // Save changes
        _eventRepository.Update(eventAggregate);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully cancelled event {EventId} to version {Version}", 
            eventAggregate.Id, eventAggregate.Version);

        // Convert to DTO
        var getEventQuery = new GetEventQuery(eventAggregate.Id, true, true, true);
        return GetEventQueryHandler.MapToDto(eventAggregate, getEventQuery);
    }

    private async Task<EventAggregate> GetEventAggregate(Guid eventId, CancellationToken cancellationToken)
    {
        var eventAggregate = await _eventRepository.GetWithFullDetailsAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event with ID '{eventId}' not found");
        }
        return eventAggregate;
    }

    private static void ValidateVersion(EventAggregate eventAggregate, int expectedVersion)
    {
        if (eventAggregate.Version != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict: Expected version {expectedVersion}, but current version is {eventAggregate.Version}");
        }
    }

    private void ValidateCancellationRules(EventAggregate eventAggregate)
    {
        // Cannot cancel already cancelled events
        if (eventAggregate.Status == EventStatus.Cancelled)
        {
            throw new InvalidOperationException("Event is already cancelled");
        }

        // Cannot cancel events that have already occurred
        if (eventAggregate.EventDate <= _dateTimeProvider.UtcNow)
        {
            throw new InvalidOperationException("Cannot cancel events that have already occurred");
        }

        // Business rule: May want to restrict cancellation based on how close to the event date
        var hoursUntilEvent = (eventAggregate.EventDate - _dateTimeProvider.UtcNow).TotalHours;
        if (hoursUntilEvent < 24)
        {
            _logger.LogWarning("Cancelling event {EventId} with less than 24 hours notice", eventAggregate.Id);
            // In a real system, you might want to require special permissions for last-minute cancellations
        }

        // Check if there are confirmed reservations
        var hasConfirmedReservations = eventAggregate.TicketTypes.Any(tt => tt.Capacity.Reserved > 0);
        if (hasConfirmedReservations)
        {
            _logger.LogWarning("Cancelling event {EventId} with confirmed reservations", eventAggregate.Id);
            // This will trigger refund processes through domain events
        }
    }

    private async Task HandleActiveReservations(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            // Get all active reservations for the event
            var activeReservations = await _reservationRepository.GetByEventIdAsync(eventId, cancellationToken);
            
            // Cancel active reservations
            foreach (var reservation in activeReservations.Where(r => r.Status.Equals(Domain.Enums.ReservationStatus.Active)))
            {
                reservation.Cancel("Event cancelled");
                await _reservationRepository.UpdateAsync(reservation, cancellationToken);
                
                _logger.LogInformation("Cancelled reservation {ReservationId} due to event cancellation", 
                    reservation.Id);
            }

            // Expire pending reservations
            foreach (var reservation in activeReservations.Where(r => r.Status.Equals(Domain.Enums.ReservationStatus.Pending)))
            {
                reservation.Expire();
                await _reservationRepository.UpdateAsync(reservation, cancellationToken);
                
                _logger.LogInformation("Expired reservation {ReservationId} due to event cancellation", 
                    reservation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling active reservations for cancelled event {EventId}", eventId);
            // Don't throw here - we still want the event cancellation to succeed
            // The reservation cleanup can be handled by background processes if needed
        }
    }
}
