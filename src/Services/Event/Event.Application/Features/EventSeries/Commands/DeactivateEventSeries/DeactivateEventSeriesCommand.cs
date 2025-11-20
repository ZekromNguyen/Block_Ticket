using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Commands.DeactivateEventSeries;

/// <summary>
/// Command to deactivate an event series
/// </summary>
public record DeactivateEventSeriesCommand : IRequest<EventSeriesDto>
{
    public Guid SeriesId { get; init; }
    public int ExpectedVersion { get; init; } // For optimistic concurrency control
    public string? Reason { get; init; } // Optional reason for deactivation

    public DeactivateEventSeriesCommand(Guid seriesId, int expectedVersion, string? reason = null)
    {
        SeriesId = seriesId;
        ExpectedVersion = expectedVersion;
        Reason = reason;
    }
}

/// <summary>
/// Request DTO for deactivating event series
/// </summary>
public record DeactivateEventSeriesRequest
{
    public string? Reason { get; init; }
}
