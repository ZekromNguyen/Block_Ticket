namespace Event.Domain.Configuration;

/// <summary>
/// Configuration for caching behavior
/// </summary>
public class CacheConfiguration
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache configuration
    /// </summary>
    public CachePolicy Default { get; set; } = new();

    /// <summary>
    /// Cache policies for different data types
    /// </summary>
    public Dictionary<string, CachePolicy> Policies { get; set; } = new();

    /// <summary>
    /// Redis-specific configuration
    /// </summary>
    public RedisConfiguration Redis { get; set; } = new();

    /// <summary>
    /// In-memory cache configuration
    /// </summary>
    public MemoryCacheConfiguration Memory { get; set; } = new();

    /// <summary>
    /// Cache warming settings
    /// </summary>
    public CacheWarmupConfiguration Warmup { get; set; } = new();

    /// <summary>
    /// Cache invalidation strategies
    /// </summary>
    public CacheInvalidationConfiguration Invalidation { get; set; } = new();
}

/// <summary>
/// Cache policy for specific data types
/// </summary>
public class CachePolicy
{
    /// <summary>
    /// Time-to-live for cached items
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to enable sliding expiration
    /// </summary>
    public bool SlidingExpiration { get; set; } = false;

    /// <summary>
    /// Priority for cache items
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// Whether to compress cached data
    /// </summary>
    public bool Compress { get; set; } = false;

    /// <summary>
    /// Maximum size for cached items (in bytes)
    /// </summary>
    public long MaxSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Whether to cache null/empty results
    /// </summary>
    public bool CacheNullResults { get; set; } = false;

    /// <summary>
    /// Tags for cache invalidation
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Redis-specific configuration
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// Connection string for Redis
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Database number to use
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for all cache entries
    /// </summary>
    public string KeyPrefix { get; set; } = "event-service";

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Operation timeout in milliseconds
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int ConnectRetry { get; set; } = 3;

    /// <summary>
    /// Whether to abort on connection failure
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Redis cluster configuration
    /// </summary>
    public RedisClusterConfiguration Cluster { get; set; } = new();
}

/// <summary>
/// Redis cluster configuration
/// </summary>
public class RedisClusterConfiguration
{
    /// <summary>
    /// Whether cluster mode is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Cluster endpoints
    /// </summary>
    public List<string> Endpoints { get; set; } = new();

    /// <summary>
    /// Cluster configuration check interval
    /// </summary>
    public TimeSpan ConfigCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// In-memory cache configuration
/// </summary>
public class MemoryCacheConfiguration
{
    /// <summary>
    /// Maximum size limit for memory cache
    /// </summary>
    public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Compaction percentage when size limit is reached
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;

    /// <summary>
    /// Scan frequency for expired items
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Cache warming configuration
/// </summary>
public class CacheWarmupConfiguration
{
    /// <summary>
    /// Whether cache warming is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between warmup operations
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Types of data to warm up
    /// </summary>
    public List<string> DataTypes { get; set; } = new() 
    { 
        "popular-events", 
        "venue-seatmaps", 
        "active-pricing-rules" 
    };

    /// <summary>
    /// Maximum number of items to warm up per type
    /// </summary>
    public int MaxItemsPerType { get; set; } = 100;
}

/// <summary>
/// Cache invalidation configuration
/// </summary>
public class CacheInvalidationConfiguration
{
    /// <summary>
    /// Whether auto-invalidation is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache invalidation strategies
    /// </summary>
    public List<InvalidationStrategy> Strategies { get; set; } = new();

    /// <summary>
    /// Whether to use versioned cache keys
    /// </summary>
    public bool UseVersionedKeys { get; set; } = true;

    /// <summary>
    /// Default cache version
    /// </summary>
    public string DefaultVersion { get; set; } = "v1";
}

/// <summary>
/// Cache invalidation strategy
/// </summary>
public class InvalidationStrategy
{
    /// <summary>
    /// Type of data this strategy applies to
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Events that trigger invalidation
    /// </summary>
    public List<string> TriggerEvents { get; set; } = new();

    /// <summary>
    /// Patterns of cache keys to invalidate
    /// </summary>
    public List<string> InvalidationPatterns { get; set; } = new();

    /// <summary>
    /// Delay before invalidation (for eventual consistency)
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
}

/// <summary>
/// Cache priority levels
/// </summary>
public enum CachePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Cache metrics for monitoring
/// </summary>
public class CacheMetrics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public long SetCount { get; set; }
    public long RemoveCount { get; set; }
    public long ErrorCount { get; set; }
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    public TimeSpan AverageGetTime { get; set; }
    public TimeSpan AverageSetTime { get; set; }
    public long CurrentSize { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache operation result
/// </summary>
public class CacheOperationResult<T>
{
    public T? Value { get; set; }
    public bool Success { get; set; }
    public bool WasFromCache { get; set; }
    public TimeSpan OperationTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
