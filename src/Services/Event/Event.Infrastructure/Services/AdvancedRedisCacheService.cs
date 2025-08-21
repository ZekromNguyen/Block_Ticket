using Event.Domain.Configuration;
using Event.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Advanced Redis cache service with enhanced features
/// </summary>
public class AdvancedRedisCacheService : IAdvancedCacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<AdvancedRedisCacheService> _logger;
    private readonly CacheConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, CacheMetrics> _metrics;
    private readonly string _keyPrefix;

    public AdvancedRedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheConfiguration> config,
        ILogger<AdvancedRedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase(config.Value.Redis.Database);
        _subscriber = _connectionMultiplexer.GetSubscriber();
        _config = config.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = _config.Redis.KeyPrefix;
        _metrics = new ConcurrentDictionary<string, CacheMetrics>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Basic Cache Operations

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var result = await GetWithResultAsync<T>(key, cancellationToken);
        return result.Value;
    }

    public async Task<CacheOperationResult<T>> GetWithResultAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var prefixedKey = GetPrefixedKey(key);

        try
        {
            var value = await _database.StringGetAsync(prefixedKey);

            if (!value.HasValue)
            {
                RecordMetric("miss", stopwatch.Elapsed);
                _logger.LogDebug("Cache miss for key: {Key}", key);
                
                return new CacheOperationResult<T>
                {
                    Success = true,
                    WasFromCache = false,
                    OperationTime = stopwatch.Elapsed
                };
            }

            var deserializedValue = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            RecordMetric("hit", stopwatch.Elapsed);
            _logger.LogDebug("Cache hit for key: {Key}", key);

            return new CacheOperationResult<T>
            {
                Value = deserializedValue,
                Success = true,
                WasFromCache = true,
                OperationTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            RecordMetric("error", stopwatch.Elapsed);
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            
            return new CacheOperationResult<T>
            {
                Success = false,
                ErrorMessage = ex.Message,
                OperationTime = stopwatch.Elapsed
            };
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var policy = GetPolicyForKey(key);
        policy.Ttl = expiration ?? policy.Ttl;
        await SetAsync(key, value, policy, cancellationToken);
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var prefixedKey = GetPrefixedKey(key);

        try
        {
            // Skip caching null values if policy doesn't allow it
            if (value == null && !policy.CacheNullResults)
            {
                return;
            }

            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            // Check size limit
            var size = System.Text.Encoding.UTF8.GetByteCount(serializedValue);
            if (size > policy.MaxSize)
            {
                _logger.LogWarning("Cache value too large for key {Key}. Size: {Size}, Limit: {Limit}", 
                    key, size, policy.MaxSize);
                return;
            }

            // Set with expiration
            await _database.StringSetAsync(prefixedKey, serializedValue, policy.Ttl);

            // Add tags for invalidation if specified
            if (policy.Tags.Any())
            {
                var tasks = policy.Tags.Select(tag => 
                    _database.SetAddAsync(GetTagKey(tag), prefixedKey));
                await Task.WhenAll(tasks);
            }

            RecordMetric("set", stopwatch.Elapsed);
            _logger.LogDebug("Cached value for key: {Key} with TTL: {TTL}", key, policy.Ttl);
        }
        catch (Exception ex)
        {
            RecordMetric("error", stopwatch.Elapsed);
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var prefixedKey = GetPrefixedKey(key);

        try
        {
            await _database.KeyDeleteAsync(prefixedKey);
            RecordMetric("remove", stopwatch.Elapsed);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            RecordMetric("error", stopwatch.Elapsed);
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            return await _database.KeyExistsAsync(prefixedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    #endregion

    #region Advanced Operations

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var policy = GetPolicyForKey(key);
        policy.Ttl = expiration ?? policy.Ttl;
        return await GetOrSetAsync(key, factory, policy, cancellationToken);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache first
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Generate value using factory
        var value = await factory(cancellationToken);
        
        // Cache the generated value
        await SetAsync(key, value, policy, cancellationToken);
        
        return value;
    }

    public async Task SetBatchAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!items.Any()) return;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var tasks = items.Select(kvp =>
            {
                var prefixedKey = GetPrefixedKey(kvp.Key);
                var serializedValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                var ttl = expiration ?? GetPolicyForKey(kvp.Key).Ttl;
                return _database.StringSetAsync(prefixedKey, serializedValue, ttl);
            });

            await Task.WhenAll(tasks);
            RecordMetric("batch_set", stopwatch.Elapsed);
            _logger.LogDebug("Batch cached {Count} items", items.Count);
        }
        catch (Exception ex)
        {
            RecordMetric("error", stopwatch.Elapsed);
            _logger.LogError(ex, "Error in batch cache set operation");
        }
    }

    public async Task<Dictionary<string, T?>> GetBatchAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class
    {
        var keyList = keys.ToList();
        if (!keyList.Any()) return new Dictionary<string, T?>();

        var stopwatch = Stopwatch.StartNew();
        var result = new Dictionary<string, T?>();

        try
        {
            var prefixedKeys = keyList.Select(GetPrefixedKey).Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(prefixedKeys);

            for (int i = 0; i < keyList.Count; i++)
            {
                var originalKey = keyList[i];
                var value = values[i];

                if (value.HasValue)
                {
                    try
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                        result[originalKey] = deserializedValue;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}", originalKey);
                        result[originalKey] = null;
                    }
                }
                else
                {
                    result[originalKey] = null;
                }
            }

            RecordMetric("batch_get", stopwatch.Elapsed);
            _logger.LogDebug("Batch retrieved {Count} items", keyList.Count);
        }
        catch (Exception ex)
        {
            RecordMetric("error", stopwatch.Elapsed);
            _logger.LogError(ex, "Error in batch cache get operation");
        }

        return result;
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var prefixedPattern = GetPrefixedKey(pattern);
            var keys = server.Keys(pattern: prefixedPattern);

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

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await RemoveByTagsAsync(new[] { tag }, cancellationToken);
    }

    public async Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var allKeys = new HashSet<RedisValue>();

            foreach (var tag in tags)
            {
                var tagKey = GetTagKey(tag);
                var keys = await _database.SetMembersAsync(tagKey);
                foreach (var key in keys)
                {
                    allKeys.Add(key);
                }
                
                // Remove the tag set itself
                await _database.KeyDeleteAsync(tagKey);
            }

            if (allKeys.Any())
            {
                var keyArray = allKeys.Select(k => (RedisKey)k.ToString()).ToArray();
                await _database.KeyDeleteAsync(keyArray);
                _logger.LogDebug("Removed {Count} cache keys by tags: {Tags}", 
                    allKeys.Count, string.Join(", ", tags));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by tags: {Tags}", string.Join(", ", tags));
        }
    }

    #endregion

    #region Metrics and Monitoring

    public Task<CacheMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var aggregatedMetrics = new CacheMetrics();

        foreach (var metric in _metrics.Values)
        {
            aggregatedMetrics.HitCount += metric.HitCount;
            aggregatedMetrics.MissCount += metric.MissCount;
            aggregatedMetrics.SetCount += metric.SetCount;
            aggregatedMetrics.RemoveCount += metric.RemoveCount;
            aggregatedMetrics.ErrorCount += metric.ErrorCount;
        }

        // Calculate average times
        if (_metrics.Values.Any())
        {
            aggregatedMetrics.AverageGetTime = TimeSpan.FromMilliseconds(
                _metrics.Values.Average(m => m.AverageGetTime.TotalMilliseconds));
            aggregatedMetrics.AverageSetTime = TimeSpan.FromMilliseconds(
                _metrics.Values.Average(m => m.AverageSetTime.TotalMilliseconds));
        }

        return Task.FromResult(aggregatedMetrics);
    }

    public Task ClearMetricsAsync(CancellationToken cancellationToken = default)
    {
        _metrics.Clear();
        _logger.LogInformation("Cache metrics cleared");
        return Task.CompletedTask;
    }

    public async Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            
            var exists = await _database.KeyExistsAsync(prefixedKey);
            if (!exists)
            {
                return null;
            }

            var ttl = await _database.KeyTimeToLiveAsync(prefixedKey);
            var type = await _database.KeyTypeAsync(prefixedKey);
            
            return new CacheKeyInfo
            {
                Key = key,
                Exists = exists,
                TimeToLive = ttl,
                Type = type.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key info: {Key}", key);
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var prefixedPattern = GetPrefixedKey(pattern);
            var keys = server.Keys(pattern: prefixedPattern);
            
            return keys.Select(k => RemovePrefix(k.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache keys with pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<CacheSizeInfo> GetSizeInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var infoResult = await server.InfoAsync("memory");
            
            var totalKeys = await _database.ExecuteAsync("DBSIZE");
            var totalKeysLong = (long)totalKeys;
            
            // Process Redis INFO result properly
            var memoryUsage = 0L;
            foreach (var section in infoResult)
            {
                foreach (var kvp in section)
                {
                    if (kvp.Key == "used_memory" && long.TryParse(kvp.Value, out var mem))
                    {
                        memoryUsage = mem;
                        break;
                    }
                }
            }

            return new CacheSizeInfo
            {
                TotalKeys = totalKeysLong,
                TotalMemoryUsage = memoryUsage,
                AverageKeySize = totalKeysLong > 0 ? memoryUsage / totalKeysLong : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache size info");
            return new CacheSizeInfo();
        }
    }

    #endregion

    #region Utility Operations

    public async Task RefreshAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default) where T : class
    {
        // Remove existing value
        await RemoveAsync(key, cancellationToken);
        
        // Generate and cache new value
        var value = await factory(cancellationToken);
        await SetAsync(key, value, cancellationToken: cancellationToken);
    }

    public async Task ExtendExpirationAsync(string key, TimeSpan extension, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var currentTtl = await _database.KeyTimeToLiveAsync(prefixedKey);
            
            if (currentTtl.HasValue)
            {
                var newTtl = currentTtl.Value.Add(extension);
                await _database.KeyExpireAsync(prefixedKey, newTtl);
                _logger.LogDebug("Extended expiration for key {Key} by {Extension}", key, extension);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending expiration for key: {Key}", key);
        }
    }

    public async Task SetExpirationAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _database.KeyExpireAsync(prefixedKey, expiration);
            _logger.LogDebug("Set expiration for key {Key} to {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiration for key: {Key}", key);
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            return await _database.KeyTimeToLiveAsync(prefixedKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for key: {Key}", key);
            return null;
        }
    }

    #endregion

    #region Atomic Operations

    public async Task<long> IncrementAsync(string key, long increment = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            return await _database.StringIncrementAsync(prefixedKey, increment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key}", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long decrement = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(key);
            return await _database.StringDecrementAsync(prefixedKey, decrement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing key: {Key}", key);
            return 0;
        }
    }

    #endregion

    #region Set Operations

    public async Task AddToSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(setKey);
            await _database.SetAddAsync(prefixedKey, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to set: {SetKey}", setKey);
        }
    }

    public async Task RemoveFromSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(setKey);
            await _database.SetRemoveAsync(prefixedKey, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from set: {SetKey}", setKey);
        }
    }

    public async Task<IEnumerable<string>> GetSetMembersAsync(string setKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(setKey);
            var members = await _database.SetMembersAsync(prefixedKey);
            return members.Select(m => m.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting set members: {SetKey}", setKey);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> IsInSetAsync(string setKey, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixedKey = GetPrefixedKey(setKey);
            return await _database.SetContainsAsync(prefixedKey, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking set membership: {SetKey}", setKey);
            return false;
        }
    }

    #endregion

    #region Pub/Sub Operations

    public async Task PublishInvalidationAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = GetInvalidationChannel();
            await _subscriber.PublishAsync(channel, pattern);
            _logger.LogDebug("Published invalidation for pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing invalidation: {Pattern}", pattern);
        }
    }

    public async Task SubscribeToInvalidationAsync(Func<string, Task> onInvalidation, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = GetInvalidationChannel();
            await _subscriber.SubscribeAsync(channel, async (ch, pattern) =>
            {
                await onInvalidation(pattern!);
            });
            _logger.LogInformation("Subscribed to cache invalidation notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to invalidation notifications");
        }
    }

    #endregion

    #region Private Helper Methods

    private string GetPrefixedKey(string key)
    {
        return $"{_keyPrefix}:{key}";
    }

    private string RemovePrefix(string prefixedKey)
    {
        var prefix = $"{_keyPrefix}:";
        return prefixedKey.StartsWith(prefix) ? prefixedKey.Substring(prefix.Length) : prefixedKey;
    }

    private string GetTagKey(string tag)
    {
        return GetPrefixedKey($"tag:{tag}");
    }

    private string GetInvalidationChannel()
    {
        return GetPrefixedKey("invalidation");
    }

    private CachePolicy GetPolicyForKey(string key)
    {
        // Try to find specific policy for the key pattern
        foreach (var policy in _config.Policies)
        {
            if (key.Contains(policy.Key, StringComparison.OrdinalIgnoreCase))
            {
                return policy.Value;
            }
        }

        return _config.Default;
    }

    private void RecordMetric(string operation, TimeSpan duration)
    {
        var key = "global";
        var metrics = _metrics.GetOrAdd(key, _ => new CacheMetrics());

        switch (operation.ToLowerInvariant())
        {
            case "hit":
                metrics.HitCount++;
                break;
            case "miss":
                metrics.MissCount++;
                break;
            case "set":
            case "batch_set":
                metrics.SetCount++;
                break;
            case "remove":
                metrics.RemoveCount++;
                break;
            case "error":
                metrics.ErrorCount++;
                break;
        }

        // Update average times (simplified)
        if (operation.Contains("get"))
        {
            metrics.AverageGetTime = TimeSpan.FromMilliseconds(
                (metrics.AverageGetTime.TotalMilliseconds + duration.TotalMilliseconds) / 2);
        }
        else if (operation.Contains("set"))
        {
            metrics.AverageSetTime = TimeSpan.FromMilliseconds(
                (metrics.AverageSetTime.TotalMilliseconds + duration.TotalMilliseconds) / 2);
        }

        metrics.LastUpdated = DateTime.UtcNow;
    }

    private static long ExtractMemoryUsage(string info)
    {
        // Parse Redis INFO memory output
        var lines = info.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("used_memory:"))
            {
                var value = line.Split(':')[1].Trim();
                if (long.TryParse(value, out var memory))
                {
                    return memory;
                }
            }
        }
        return 0;
    }

    #endregion
}
