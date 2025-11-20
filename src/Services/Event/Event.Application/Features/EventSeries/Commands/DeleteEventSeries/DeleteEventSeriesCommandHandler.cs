using Event.Application.Common.Interfaces;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Commands.DeleteEventSeries;

/// <summary>
/// Handler for DeleteEventSeriesCommand
/// </summary>
public class DeleteEventSeriesCommandHandler : IRequestHandler<DeleteEventSeriesCommand, bool>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<DeleteEventSeriesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        IEventRepository eventRepository,
        ILogger<DeleteEventSeriesCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _eventRepository = eventRepository;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteEventSeriesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting event series {SeriesId} with expected version {ExpectedVersion}", 
            request.SeriesId, request.ExpectedVersion);

        // Get the existing event series
        var eventSeries = await GetEventSeries(request.SeriesId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventSeries, request.ExpectedVersion);

        // Validate business rules for deletion
        await ValidateDeletionRules(eventSeries, request.ForceDelete, cancellationToken);

        // If series has events and force delete is enabled, remove events from series first
        if (eventSeries.EventIds.Any() && request.ForceDelete)
        {
            await RemoveAllEventsFromSeries(eventSeries, cancellationToken);
        }

        // Perform soft delete
        await _eventSeriesRepository.DeleteAsync(eventSeries, cancellationToken);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted event series {SeriesId}", eventSeries.Id);
        return true;
    }

    private async Task<Domain.Entities.EventSeries> GetEventSeries(Guid seriesId, CancellationToken cancellationToken)
    {
        var eventSeries = await _eventSeriesRepository.GetByIdAsync(seriesId, cancellationToken);
        if (eventSeries == null)
        {
            throw new InvalidOperationException($"Event series with ID {seriesId} not found");
        }
        return eventSeries;
    }

    private static void ValidateVersion(Domain.Entities.EventSeries eventSeries, int expectedVersion)
    {
        if (eventSeries.Version != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict: Expected version {expectedVersion}, but current version is {eventSeries.Version}");
        }
    }

    private async Task ValidateDeletionRules(Domain.Entities.EventSeries eventSeries, bool forceDelete, CancellationToken cancellationToken)
    {
        // Check if series has events
        if (eventSeries.EventIds.Any() && !forceDelete)
        {
            throw new InvalidOperationException(
                $"Cannot delete event series '{eventSeries.Name}' because it contains {eventSeries.EventIds.Count} events. " +
                "Use ForceDelete=true to remove all events from the series and delete it.");
        }

        // Additional business rule validations can be added here
        // For example: Check if any events in the series are currently on sale
        if (eventSeries.EventIds.Any() && forceDelete)
        {
            await ValidateEventsCanBeRemovedFromSeries(eventSeries.EventIds, cancellationToken);
        }
    }

    private async Task ValidateEventsCanBeRemovedFromSeries(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken)
    {
        // Check if any events are in a state that prevents removal from series
        foreach (var eventId in eventIds)
        {
            var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (eventAggregate != null)
            {
                // Add business rules here - for example, don't allow removal if event is on sale
                if (eventAggregate.Status == Domain.Enums.EventStatus.Published)
                {
                    throw new InvalidOperationException(
                        $"Cannot remove event '{eventAggregate.Title}' from series because it is currently on sale");
                }
            }
        }
    }

    private async Task RemoveAllEventsFromSeries(Domain.Entities.EventSeries eventSeries, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing {EventCount} events from series {SeriesId} before deletion", 
            eventSeries.EventIds.Count, eventSeries.Id);

        // Create a copy of the event IDs to avoid modification during iteration
        var eventIds = eventSeries.EventIds.ToList();

        foreach (var eventId in eventIds)
        {
            // Remove event from series
            eventSeries.RemoveEvent(eventId);

            // Update the event to remove series reference
            var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (eventAggregate != null)
            {
                // Assuming EventAggregate has a method to remove series reference
                // This would need to be implemented in the EventAggregate entity
                // eventAggregate.RemoveFromSeries();
                _eventRepository.Update(eventAggregate);
            }
        }

        // Save changes to update the series
        _eventSeriesRepository.Update(eventSeries);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);
    }
}
