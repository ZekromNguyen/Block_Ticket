using Shared.Contracts.Dtos;

namespace Ticketing.Application.Interfaces;

/// <summary>
/// Cross-service boundary into Event Service's seat map. Ticketing never owns seats;
/// it requests the Event Service to hold, confirm, or release them.
/// </summary>
public interface ISeatMapAvailabilityService
{
    Task<SeatAvailabilitySnapshotDto?> GetSnapshotAsync(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken);

    Task<SeatHoldResponseDto?> HoldSeatsAsync(SeatHoldRequestDto request, CancellationToken cancellationToken);

    Task ReleaseSeatsAsync(Guid eventId, Guid ticketTypeId, string holdOwner, CancellationToken cancellationToken);

    Task ConfirmSeatsAsync(Guid eventId, Guid ticketTypeId, string holdOwner, CancellationToken cancellationToken);
}

/// <summary>
/// Event-side resale policy consulted by Ticketing when a user lists a ticket for resale.
/// </summary>
public interface ITicketResalePolicy
{
    Task<ResalePolicyDto> GetPolicyAsync(Guid eventId, CancellationToken cancellationToken);

    Task<ResalePolicyCheckResult> CheckAsync(Guid eventId, decimal originalPrice, decimal requestedPrice, CancellationToken cancellationToken);
}

public sealed record ResalePolicyCheckResult(bool Allowed, string? Reason)
{
    public static ResalePolicyCheckResult Allow() => new(true, null);
    public static ResalePolicyCheckResult Deny(string reason) => new(false, reason);
}