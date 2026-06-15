using Ticketing.Application.DTOs;

namespace Ticketing.Application.Interfaces;

public interface IInventoryLockService
{
    Task<bool> TryReserveAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, TimeSpan ttl, CancellationToken cancellationToken);

    Task ReleaseAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, CancellationToken cancellationToken);
}
