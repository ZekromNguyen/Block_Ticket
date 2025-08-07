namespace Shared.Contracts.Events;

public record UserRegistered(
    Guid UserId,
    string Email,
    string UserType,
    DateTime RegisteredAt
);
