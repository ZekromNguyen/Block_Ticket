using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Persistence;

public sealed class InMemoryInventoryLockService : IInventoryLockService
{
    private const int DefaultLocalCapacityPerTicketType = 1000;
    private static readonly object Sync = new();
    private static readonly Dictionary<string, List<InventoryLock>> Locks = new();

    public Task<bool> TryReserveAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.Add(ttl);

        lock (Sync)
        {
            CleanupExpired();

            foreach (var item in items)
            {
                var key = BuildKey(eventId, item.TicketTypeId);
                var locks = Locks.TryGetValue(key, out var existing) ? existing : new List<InventoryLock>();
                var ownedQuantity = locks.Count(itemLock => itemLock.Owner == lockOwner);
                var reservedByOthers = locks.Count - ownedQuantity;

                if (reservedByOthers + item.Quantity > DefaultLocalCapacityPerTicketType)
                {
                    return Task.FromResult(false);
                }
            }

            foreach (var item in items)
            {
                var key = BuildKey(eventId, item.TicketTypeId);
                if (!Locks.TryGetValue(key, out var locks))
                {
                    locks = new List<InventoryLock>();
                    Locks[key] = locks;
                }

                var owned = locks.Where(itemLock => itemLock.Owner == lockOwner).ToList();
                if (owned.Count >= item.Quantity)
                {
                    foreach (var itemLock in owned)
                    {
                        itemLock.ExpiresAt = expiresAt;
                    }
                }
                else
                {
                    for (var index = owned.Count; index < item.Quantity; index++)
                    {
                        locks.Add(new InventoryLock(lockOwner, expiresAt));
                    }
                }
            }

            return Task.FromResult(true);
        }
    }

    public Task ReleaseAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            foreach (var item in items)
            {
                var key = BuildKey(eventId, item.TicketTypeId);
                if (Locks.TryGetValue(key, out var existing))
                {
                    existing.RemoveAll(itemLock => itemLock.Owner == lockOwner);
                    if (existing.Count == 0)
                    {
                        Locks.Remove(key);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static string BuildKey(Guid eventId, Guid ticketTypeId) => $"{eventId:N}:{ticketTypeId:N}";

    private static void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in Locks.Keys.ToList())
        {
            Locks[key].RemoveAll(itemLock => itemLock.ExpiresAt <= now);
            if (Locks[key].Count == 0)
            {
                Locks.Remove(key);
            }
        }
    }

    private sealed class InventoryLock
    {
        public InventoryLock(string owner, DateTime expiresAt)
        {
            Owner = owner;
            ExpiresAt = expiresAt;
        }

        public string Owner { get; }

        public DateTime ExpiresAt { get; set; }
    }
}
