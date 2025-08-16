using Event.Domain.Configuration;
using Event.Domain.Interfaces;

namespace Event.Domain.Services;

/// <summary>
/// Advanced cache service with enhanced features
/// </summary>
public interface IAdvancedCacheService : ICacheService
{
    /// <summary>
    /// Gets a value with detailed operation result
    /// </summary>
    Task<CacheOperationResult<T>> GetWithResultAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value with specific cache policy
    /// </summary>
    Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets or sets a value using a factory function
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets or sets a value with cache policy
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets multiple values in a batch operation
    /// </summary>
    Task SetBatchAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets multiple values in a batch operation
    /// </summary>
    Task<Dictionary<string, T?>> GetBatchAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes items by tags
    /// </summary>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes items by multiple tags
    /// </summary>
    Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache metrics
    /// </summary>
    Task<CacheMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache metrics
    /// </summary>
    Task ClearMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache key information
    /// </summary>
    Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cache keys matching a pattern
    /// </summary>
    Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache size information
    /// </summary>
    Task<CacheSizeInfo> GetSizeInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes (reloads) a cache entry
    /// </summary>
    Task RefreshAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Extends the expiration time of a cache entry
    /// </summary>
    Task ExtendExpirationAsync(string key, TimeSpan extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets expiration for an existing cache entry
    /// </summary>
    Task SetExpirationAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining TTL for a cache entry
    /// </summary>
    Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment a numeric value atomically
    /// </summary>
    Task<long> IncrementAsync(string key, long increment = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrement a numeric value atomically
    /// </summary>
    Task<long> DecrementAsync(string key, long decrement = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to a cache set
    /// </summary>
    Task AddToSetAsync(string setKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from a cache set
    /// </summary>
    Task RemoveFromSetAsync(string setKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all members of a cache set
    /// </summary>
    Task<IEnumerable<string>> GetSetMembersAsync(string setKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a value exists in a cache set
    /// </summary>
    Task<bool> IsInSetAsync(string setKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a cache invalidation message
    /// </summary>
    Task PublishInvalidationAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to cache invalidation messages
    /// </summary>
    Task SubscribeToInvalidationAsync(Func<string, Task> onInvalidation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache key information
/// </summary>
public class CacheKeyInfo
{
    public string Key { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public TimeSpan? TimeToLive { get; set; }
    public DateTime? LastAccessed { get; set; }
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Cache size information
/// </summary>
public class CacheSizeInfo
{
    public long TotalKeys { get; set; }
    public long TotalMemoryUsage { get; set; }
    public long AverageKeySize { get; set; }
    public Dictionary<string, long> SizeByPattern { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache warming service interface
/// </summary>
public interface ICacheWarmupService
{
    /// <summary>
    /// Warms up frequently accessed data
    /// </summary>
    Task WarmupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Warms up specific data type
    /// </summary>
    Task WarmupDataTypeAsync(string dataType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules cache warmup
    /// </summary>
    Task ScheduleWarmupAsync(TimeSpan interval, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache invalidation service interface  
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates cache based on domain events
    /// </summary>
    Task InvalidateOnEventAsync(string eventType, object eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers invalidation strategy
    /// </summary>
    Task RegisterStrategyAsync(InvalidationStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active invalidation strategies
    /// </summary>
    Task<IEnumerable<InvalidationStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);
}
