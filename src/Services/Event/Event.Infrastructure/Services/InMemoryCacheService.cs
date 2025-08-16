using Event.Domain.Configuration;
using Event.Domain.Interfaces;
using Event.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// In-memory cache service implementation (fallback when Redis is not available)
/// </summary>
public class InMemoryCacheService : IAdvancedCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Default TTL values (same as Redis implementation)
    private static readonly TimeSpan DefaultCatalogTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultSeatMapTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultAvailabilityTtl = TimeSpan.FromSeconds(10);

    public InMemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<InMemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                
                if (value is T directValue)
                {
                    return Task.FromResult<T?>(directValue);
                }
                
                if (value is string serializedValue)
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(serializedValue, _jsonOptions);
                    return Task.FromResult(deserializedValue);
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var expiry = expiration ?? GetDefaultTtl(key);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry,
                Priority = CacheItemPriority.Normal
            };

            // Store the object directly to avoid serialization overhead in memory
            _memoryCache.Set(key, value, cacheEntryOptions);
            
            _logger.LogDebug("Cached value for key: {Key} with TTL: {TTL}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // In-memory cache doesn't support pattern-based removal natively
            // This is a simplified implementation that works for common patterns
            
            if (_memoryCache is MemoryCache memCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field?.GetValue(memCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
                    {
                        var keysToRemove = new List<object>();
                        
                        foreach (DictionaryEntry entry in entries)
                        {
                            if (entry.Key.ToString()?.Contains(pattern.Replace("*", "")) == true)
                            {
                                keysToRemove.Add(entry.Key);
                            }
                        }
                        
                        foreach (var keyToRemove in keysToRemove)
                        {
                            _memoryCache.Remove(keyToRemove);
                        }
                        
                        _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", 
                            keysToRemove.Count, pattern);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return Task.FromResult(false);
        }
    }

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

    #region IAdvancedCacheService Implementation (Simplified for In-Memory)

    public Task<CacheOperationResult<T>> GetWithResultAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var result = GetAsync<T>(key, cancellationToken).Result;
        return Task.FromResult(new CacheOperationResult<T>
        {
            Value = result,
            Success = true,
            WasFromCache = result != null
        });
    }

    public Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken cancellationToken = default) where T : class
    {
        return SetAsync(key, value, policy.Ttl, cancellationToken);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null) return cached;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CachePolicy policy, CancellationToken cancellationToken = default) where T : class
    {
        return GetOrSetAsync(key, factory, policy.Ttl, cancellationToken);
    }

    public Task SetBatchAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public async Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();
        foreach (var key in keys)
        {
            result[key] = await GetAsync<T>(key, cancellationToken);
        }
        return result;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        // In-memory implementation doesn't support tags natively
        _logger.LogWarning("Tag-based removal not supported in in-memory cache");
        return Task.CompletedTask;
    }

    public Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Tag-based removal not supported in in-memory cache");
        return Task.CompletedTask;
    }

    public Task<CacheMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CacheMetrics { HitCount = 0, MissCount = 0 });
    }

    public Task ClearMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = ExistsAsync(key, cancellationToken).Result;
        return Task.FromResult(exists ? new CacheKeyInfo { Key = key, Exists = true } : null);
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<CacheSizeInfo> GetSizeInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CacheSizeInfo());
    }

    public async Task RefreshAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        await RemoveAsync(key, cancellationToken);
        var value = await factory(cancellationToken);
        await SetAsync(key, value, cancellationToken: cancellationToken);
    }

    public Task ExtendExpirationAsync(string key, TimeSpan extension, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extend expiration not supported in in-memory cache");
        return Task.CompletedTask;
    }

    public Task SetExpirationAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Set expiration not supported in in-memory cache");
        return Task.CompletedTask;
    }

    public Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task<long> IncrementAsync(string key, long increment = 1, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }

    public Task<long> DecrementAsync(string key, long decrement = 1, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }

    public Task AddToSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveFromSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetSetMembersAsync(string setKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<bool> IsInSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task PublishInvalidationAsync(string pattern, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SubscribeToInvalidationAsync(Func<string, Task> onInvalidation, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    #endregion
}
