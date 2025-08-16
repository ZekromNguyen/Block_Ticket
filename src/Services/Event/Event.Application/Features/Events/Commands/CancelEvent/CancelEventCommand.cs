using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Events.Commands.CancelEvent;

/// <summary>
/// Command to cancel an event
/// </summary>
public record CancelEventCommand : IRequest<EventDto>
{
    public Guid EventId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int ExpectedVersion { get; init; } // For optimistic concurrency control

    public CancelEventCommand(Guid eventId, string reason, int expectedVersion)
    {
        EventId = eventId;
        Reason = reason;
        ExpectedVersion = expectedVersion;
    }
}
