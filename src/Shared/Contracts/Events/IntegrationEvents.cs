namespace Shared.Contracts.Events;

/// <summary>
/// Published when a new user is successfully created and confirmed.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="UserType">The type of user (e.g., "Promoter", "Organization").</param>
public record UserCreatedIntegrationEvent(Guid UserId, string Email, string FirstName, string LastName, string UserType);

/// <summary>
/// Published when a user's details are updated.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="Email">The user's updated email address.</param>
/// <param name="FirstName">The user's updated first name.</param>
/// <param name="LastName">The user's updated last name.</param>
public record UserUpdatedIntegrationEvent(Guid UserId, string Email, string FirstName, string LastName);



/// <summary>
/// Published when a reservation is created for tickets.
/// </summary>
public record ReservationCreatedIntegrationEvent(Guid ReservationId, Guid EventId, List<Guid> SeatIds, Guid TicketTypeId, int Quantity, DateTime ExpiresAtUtc);

/// <summary>
/// Published when a reservation is released, making tickets available again.
/// </summary>
public record ReservationReleasedIntegrationEvent(Guid ReservationId, Guid EventId, List<Guid> SeatIds, Guid TicketTypeId, int Quantity);

/// <summary>
/// Published when a reservation is confirmed and tickets are sold.
/// </summary>
public record ReservationConfirmedIntegrationEvent(Guid ReservationId, Guid EventId, Guid OrderId, List<Guid> SeatIds, Guid TicketTypeId, int Quantity);

/// <summary>
/// Published when a reservation expires.
/// </summary>
public record ReservationExpiredIntegrationEvent(Guid ReservationId, Guid EventId, List<Guid> SeatIds, Guid TicketTypeId, int Quantity);

/// <summary>
/// Published when tickets are restocked for an event.
/// </summary>
public record TicketsRestockedIntegrationEvent(Guid EventId, Guid TicketTypeId, int Quantity, string Reason);

/// <summary>
/// Published when an event is cancelled and issued tickets should be refunded.
/// </summary>
public record EventCancelledIntegrationEvent(Guid EventId, string Reason, DateTime CancelledAt);
