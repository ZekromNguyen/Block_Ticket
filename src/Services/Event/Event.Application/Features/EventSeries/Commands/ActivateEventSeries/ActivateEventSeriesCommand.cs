using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Commands.ActivateEventSeries;

/// <summary>
/// Command to activate an event series
/// </summary>
public record ActivateEventSeriesCommand : IRequest<EventSeriesDto>
{
    public Guid SeriesId { get; init; }
    public int ExpectedVersion { get; init; } // For optimistic concurrency control

    public ActivateEventSeriesCommand(Guid seriesId, int expectedVersion)
    {
        SeriesId = seriesId;
        ExpectedVersion = expectedVersion;
    }
}
