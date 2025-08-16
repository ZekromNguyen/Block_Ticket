using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Application.Features.Events.Queries.GetEvent;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.Commands.PublishEvent;

/// <summary>
/// Handler for PublishEventCommand
/// </summary>
public class PublishEventCommandHandler : IRequestHandler<PublishEventCommand, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<PublishEventCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishEventCommandHandler(
        IEventRepository eventRepository,
        ILogger<PublishEventCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventRepository = eventRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventDto> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing event {EventId} with expected version {ExpectedVersion}", 
            request.EventId, request.ExpectedVersion);

        // Get the existing event
        var eventAggregate = await GetEventAggregate(request.EventId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventAggregate, request.ExpectedVersion);

        // Validate business rules for publishing
        ValidatePublishingRules(eventAggregate);

        // Publish the event
        eventAggregate.Publish(_dateTimeProvider.UtcNow);

        // Save changes
        _eventRepository.Update(eventAggregate);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully published event {EventId} to version {Version}", 
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

    private void ValidatePublishingRules(EventAggregate eventAggregate)
    {
        // Cannot publish already published events
        if (eventAggregate.Status == EventStatus.Published || eventAggregate.Status == EventStatus.OnSale)
        {
            throw new InvalidOperationException("Event is already published");
        }

        // Cannot publish cancelled events
        if (eventAggregate.Status == EventStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot publish cancelled events");
        }

        // Cannot publish sold out events
        if (eventAggregate.Status == EventStatus.SoldOut)
        {
            throw new InvalidOperationException("Cannot publish sold out events");
        }

        // Event must be in the future
        if (eventAggregate.EventDate <= _dateTimeProvider.UtcNow.AddHours(1))
        {
            throw new InvalidOperationException("Cannot publish events that are less than 1 hour away");
        }

        // Event must have at least one ticket type
        if (!eventAggregate.TicketTypes.Any())
        {
            throw new InvalidOperationException("Cannot publish event without ticket types");
        }

        // All ticket types must have valid pricing
        foreach (var ticketType in eventAggregate.TicketTypes)
        {
            if (ticketType.BasePrice.Amount <= 0)
            {
                throw new InvalidOperationException($"Ticket type '{ticketType.Name}' must have a valid base price");
            }

            if (ticketType.Capacity.Total <= 0)
            {
                throw new InvalidOperationException($"Ticket type '{ticketType.Name}' must have a valid capacity");
            }

            // Check if ticket type has valid on-sale windows
            if (!ticketType.OnSaleWindows.Any())
            {
                throw new InvalidOperationException($"Ticket type '{ticketType.Name}' must have at least one on-sale window");
            }

            // Check if any on-sale window is currently active or will be active
            var now = _dateTimeProvider.UtcNow;
            var hasValidWindow = ticketType.OnSaleWindows.Any(w => w.EndDate > now);
            if (!hasValidWindow)
            {
                throw new InvalidOperationException($"Ticket type '{ticketType.Name}' has no future on-sale windows");
            }
        }

        // Check publish window if specified
        if (eventAggregate.PublishWindow != null)
        {
            var now = _dateTimeProvider.UtcNow;
            if (now < eventAggregate.PublishWindow.StartDate)
            {
                throw new InvalidOperationException("Cannot publish event before the publish window start date");
            }

            if (now > eventAggregate.PublishWindow.EndDate)
            {
                throw new InvalidOperationException("Cannot publish event after the publish window end date");
            }
        }

        // Validate that the event has required information for publishing
        if (string.IsNullOrWhiteSpace(eventAggregate.Title))
        {
            throw new InvalidOperationException("Event must have a title to be published");
        }

        if (string.IsNullOrWhiteSpace(eventAggregate.Description))
        {
            throw new InvalidOperationException("Event must have a description to be published");
        }

        // Optional: Check if venue exists and is valid
        // This would require injecting IVenueRepository
        // var venue = await _venueRepository.GetByIdAsync(eventAggregate.VenueId, cancellationToken);
        // if (venue == null)
        // {
        //     throw new InvalidOperationException("Event venue must exist to publish the event");
        // }
    }
}
