using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Redis-based cache service implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Default TTL values
    private static readonly TimeSpan DefaultCatalogTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultSeatMapTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultAvailabilityTtl = TimeSpan.FromSeconds(10);

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = expiration ?? GetDefaultTtl(key);
            
            await _database.StringSetAsync(key, serializedValue, expiry);
            
            _logger.LogDebug("Cached value for key: {Key} with TTL: {TTL}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            var keyArray = keys.ToArray();
            if (keyArray.Length > 0)
            {
                await _database.KeyDeleteAsync(keyArray);
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keyArray.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }

    // Event-specific cache methods
    public async Task CacheEventCatalogAsync<T>(Guid eventId, T data, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventCatalogKey(eventId);
        await SetAsync(key, data, DefaultCatalogTtl, cancellationToken);
    }

    public async Task<T?> GetEventCatalogAsync<T>(Guid eventId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventCatalogKey(eventId);
        return await GetAsync<T>(key, cancellationToken);
    }

    public async Task InvalidateEventCatalogAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var pattern = $"event:catalog:{eventId}:*";
        await RemoveByPatternAsync(pattern, cancellationToken);
    }

    // Seat map cache methods
    public async Task CacheSeatMapAsync<T>(Guid venueId, T seatMap, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetSeatMapKey(venueId);
        await SetAsync(key, seatMap, DefaultSeatMapTtl, cancellationToken);
    }

    public async Task<T?> GetSeatMapAsync<T>(Guid venueId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetSeatMapKey(venueId);
        return await GetAsync<T>(key, cancellationToken);
    }

    public async Task InvalidateSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        var key = GetSeatMapKey(venueId);
        await RemoveAsync(key, cancellationToken);
    }

    // Availability cache methods
    public async Task CacheAvailabilityAsync<T>(Guid eventId, T availability, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetAvailabilityKey(eventId);
        await SetAsync(key, availability, DefaultAvailabilityTtl, cancellationToken);
    }

    public async Task<T?> GetAvailabilityAsync<T>(Guid eventId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetAvailabilityKey(eventId);
        return await GetAsync<T>(key, cancellationToken);
    }

    public async Task InvalidateAvailabilityAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var key = GetAvailabilityKey(eventId);
        await RemoveAsync(key, cancellationToken);
    }

    // Search results cache methods
    public async Task CacheSearchResultsAsync<T>(string searchHash, T results, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetSearchResultsKey(searchHash);
        await SetAsync(key, results, DefaultCatalogTtl, cancellationToken);
    }

    public async Task<T?> GetSearchResultsAsync<T>(string searchHash, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetSearchResultsKey(searchHash);
        return await GetAsync<T>(key, cancellationToken);
    }

    // Pricing cache methods
    public async Task CachePricingAsync<T>(Guid eventId, Guid ticketTypeId, T pricing, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetPricingKey(eventId, ticketTypeId);
        await SetAsync(key, pricing, TimeSpan.FromMinutes(1), cancellationToken);
    }

    public async Task<T?> GetPricingAsync<T>(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetPricingKey(eventId, ticketTypeId);
        return await GetAsync<T>(key, cancellationToken);
    }

    public async Task InvalidatePricingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var pattern = $"event:pricing:{eventId}:*";
        await RemoveByPatternAsync(pattern, cancellationToken);
    }

    // Key generation methods
    private static string GetEventCatalogKey(Guid eventId) => $"event:catalog:{eventId}";
    private static string GetSeatMapKey(Guid venueId) => $"venue:seatmap:{venueId}";
    private static string GetAvailabilityKey(Guid eventId) => $"event:availability:{eventId}";
    private static string GetSearchResultsKey(string searchHash) => $"search:results:{searchHash}";
    private static string GetPricingKey(Guid eventId, Guid ticketTypeId) => $"event:pricing:{eventId}:{ticketTypeId}";

    private static TimeSpan GetDefaultTtl(string key)
    {
        return key switch
        {
            var k when k.Contains("seatmap") => DefaultSeatMapTtl,
            var k when k.Contains("availability") => DefaultAvailabilityTtl,
            var k when k.Contains("pricing") => TimeSpan.FromMinutes(1),
            _ => DefaultCatalogTtl
        };
    }

    public void Dispose()
    {
        _connectionMultiplexer?.Dispose();
    }
}
