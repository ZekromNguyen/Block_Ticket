using MediatR;

namespace Event.Application.Features.EventSeries.Commands.DeleteEventSeries;

/// <summary>
/// Command to delete an event series
/// </summary>
public record DeleteEventSeriesCommand : IRequest<bool>
{
    public Guid SeriesId { get; init; }
    public int ExpectedVersion { get; init; } // For optimistic concurrency control
    public bool ForceDelete { get; init; } = false; // Force delete even if series has events

    public DeleteEventSeriesCommand(Guid seriesId, int expectedVersion, bool forceDelete = false)
    {
        SeriesId = seriesId;
        ExpectedVersion = expectedVersion;
        ForceDelete = forceDelete;
    }
}

/// <summary>
/// Request DTO for deleting event series
/// </summary>
public record DeleteEventSeriesRequest
{
    public bool ForceDelete { get; init; } = false;
}
