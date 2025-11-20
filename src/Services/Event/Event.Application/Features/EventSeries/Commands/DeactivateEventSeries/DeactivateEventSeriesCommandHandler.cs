using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Commands.DeactivateEventSeries;

/// <summary>
/// Handler for DeactivateEventSeriesCommand
/// </summary>
public class DeactivateEventSeriesCommandHandler : IRequestHandler<DeactivateEventSeriesCommand, EventSeriesDto>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<DeactivateEventSeriesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeactivateEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        IEventRepository eventRepository,
        ILogger<DeactivateEventSeriesCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _eventRepository = eventRepository;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<EventSeriesDto> Handle(DeactivateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating event series {SeriesId} with expected version {ExpectedVersion}. Reason: {Reason}", 
            request.SeriesId, request.ExpectedVersion, request.Reason ?? "Not specified");

        // Get the existing event series
        var eventSeries = await GetEventSeries(request.SeriesId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventSeries, request.ExpectedVersion);

        // Validate business rules for deactivation
        await ValidateDeactivationRules(eventSeries, cancellationToken);

        // Deactivate the series
        eventSeries.Deactivate();

        // Save changes
        _eventSeriesRepository.Update(eventSeries);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deactivated event series {SeriesId} to version {Version}", 
            eventSeries.Id, eventSeries.Version);

        return EventSeriesDto.FromEntity(eventSeries);
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

    private async Task ValidateDeactivationRules(Domain.Entities.EventSeries eventSeries, CancellationToken cancellationToken)
    {
        // Check if series is already inactive
        if (!eventSeries.IsActive)
        {
            throw new InvalidOperationException($"Event series '{eventSeries.Name}' is already inactive");
        }

        // Check if any events in the series are currently on sale
        if (eventSeries.EventIds.Any())
        {
            await ValidateEventsCanBeDeactivated(eventSeries.EventIds, cancellationToken);
        }
    }

    private async Task ValidateEventsCanBeDeactivated(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken)
    {
        var onSaleEvents = new List<string>();

        foreach (var eventId in eventIds)
        {
            var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (eventAggregate != null && eventAggregate.Status == Domain.Enums.EventStatus.Published)
            {
                onSaleEvents.Add(eventAggregate.Title);
            }
        }

        if (onSaleEvents.Any())
        {
            throw new InvalidOperationException(
                $"Cannot deactivate event series because the following events are currently on sale: {string.Join(", ", onSaleEvents)}. " +
                "Please take these events off sale before deactivating the series.");
        }
    }


}
