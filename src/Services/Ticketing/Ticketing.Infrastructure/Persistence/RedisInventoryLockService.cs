using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Persistence;

public sealed class RedisInventoryLockService : IInventoryLockService, IAsyncDisposable
{
    private const int DefaultLocalCapacityPerTicketType = 1000;

    private readonly Lazy<Task<IConnectionMultiplexer?>> _connection;
    private readonly InMemoryInventoryLockService _fallback;
    private readonly ILogger<RedisInventoryLockService> _logger;

    public RedisInventoryLockService(
        IConfiguration configuration,
        InMemoryInventoryLockService fallback,
        ILogger<RedisInventoryLockService> logger)
    {
        _fallback = fallback;
        _logger = logger;
        _connection = new Lazy<Task<IConnectionMultiplexer?>>(async () =>
        {
            var connectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            try
            {
                return await ConnectionMultiplexer.ConnectAsync(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis inventory locking is unavailable; falling back to local locks");
                return null;
            }
        });
    }

    public async Task<bool> TryReserveAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            return await _fallback.TryReserveAsync(eventId, items, lockOwner, ttl, cancellationToken);
        }

        foreach (var item in items)
        {
            var key = BuildKey(eventId, item.TicketTypeId);
            var reservations = await database.HashGetAllAsync(key);
            var reserved = reservations.Sum(entry => int.TryParse(entry.Value.ToString(), out var quantity) ? quantity : 0);
            var alreadyOwned = await database.HashGetAsync(key, lockOwner);
            var alreadyOwnedQuantity = alreadyOwned.HasValue && int.TryParse(alreadyOwned.ToString(), out var parsed) ? parsed : 0;

            if (reserved - alreadyOwnedQuantity + item.Quantity > DefaultLocalCapacityPerTicketType)
            {
                return false;
            }
        }

        foreach (var item in items)
        {
            var key = BuildKey(eventId, item.TicketTypeId);
            await database.HashSetAsync(key, lockOwner, item.Quantity);
            await database.KeyExpireAsync(key, ttl);
        }

        return true;
    }

    public async Task ReleaseAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, CancellationToken cancellationToken)
    {
        var database = await GetDatabaseAsync();
        if (database is null)
        {
            await _fallback.ReleaseAsync(eventId, items, lockOwner, cancellationToken);
            return;
        }

        foreach (var item in items)
        {
            await database.HashDeleteAsync(BuildKey(eventId, item.TicketTypeId), lockOwner);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated)
        {
            var connection = await _connection.Value;
            connection?.Dispose();
        }
    }

    private async Task<IDatabase?> GetDatabaseAsync()
    {
        var connection = await _connection.Value;
        return connection?.GetDatabase();
    }

    private static string BuildKey(Guid eventId, Guid ticketTypeId) => $"ticketing:inventory:{eventId:N}:{ticketTypeId:N}";
}
