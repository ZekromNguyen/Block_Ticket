namespace Shared.Contracts.Events;

public record YourTurnInWaitingList(
    Guid UserId,
    Guid EventId,
    DateTime AvailableUntil
);
