using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Events.Commands.PublishEvent;

/// <summary>
/// Command to publish an event
/// </summary>
public record PublishEventCommand : IRequest<EventDto>
{
    public Guid EventId { get; init; }
    public int ExpectedVersion { get; init; } // For optimistic concurrency control

    public PublishEventCommand(Guid eventId, int expectedVersion)
    {
        EventId = eventId;
        ExpectedVersion = expectedVersion;
    }
}
