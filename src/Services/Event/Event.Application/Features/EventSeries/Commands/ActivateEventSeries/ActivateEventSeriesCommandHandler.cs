using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Commands.ActivateEventSeries;

/// <summary>
/// Handler for ActivateEventSeriesCommand
/// </summary>
public class ActivateEventSeriesCommandHandler : IRequestHandler<ActivateEventSeriesCommand, EventSeriesDto>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ILogger<ActivateEventSeriesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ActivateEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        ILogger<ActivateEventSeriesCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<EventSeriesDto> Handle(ActivateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating event series {SeriesId} with expected version {ExpectedVersion}", 
            request.SeriesId, request.ExpectedVersion);

        // Get the existing event series
        var eventSeries = await GetEventSeries(request.SeriesId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventSeries, request.ExpectedVersion);

        // Validate business rules for activation
        ValidateActivationRules(eventSeries);

        // Activate the series
        eventSeries.Activate();

        // Save changes
        _eventSeriesRepository.Update(eventSeries);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully activated event series {SeriesId} to version {Version}", 
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

    private static void ValidateActivationRules(Domain.Entities.EventSeries eventSeries)
    {
        // Add business rules for activation
        if (eventSeries.IsActive)
        {
            throw new InvalidOperationException($"Event series '{eventSeries.Name}' is already active");
        }

        // Additional validation rules can be added here
        // For example: Check if series has required information
        if (string.IsNullOrWhiteSpace(eventSeries.Name))
        {
            throw new InvalidOperationException("Event series must have a name to be activated");
        }
    }


}
