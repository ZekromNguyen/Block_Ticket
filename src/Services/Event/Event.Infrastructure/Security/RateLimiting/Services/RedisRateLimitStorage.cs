using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Event.Infrastructure.Security.RateLimiting.Services;

/// <summary>
/// Redis-based implementation of rate limit storage using sliding window algorithm
/// </summary>
public class RedisRateLimitStorage : IRateLimitStorage, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisRateLimitStorage> _logger;
    private readonly string _keyPrefix;

    // Lua script for atomic sliding window increment
    private const string SlidingWindowScript = @"
        local key = KEYS[1]
        local window = tonumber(ARGV[1])
        local limit = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])
        local clearBefore = now - window

        -- Remove expired entries
        redis.call('ZREMRANGEBYSCORE', key, 0, clearBefore)

        -- Get current count
        local current = redis.call('ZCARD', key)

        -- Check if limit would be exceeded
        if current >= limit then
            return {current, 1} -- {count, isExceeded}
        end

        -- Add current request
        redis.call('ZADD', key, now, now)
        redis.call('EXPIRE', key, window)

        return {current + 1, 0} -- {count, isExceeded}
    ";

    // Lua script for fixed window increment
    private const string FixedWindowScript = @"
        local key = KEYS[1]
        local window = tonumber(ARGV[1])
        local current = redis.call('INCR', key)
        
        if current == 1 then
            redis.call('EXPIRE', key, window)
        end
        
        local ttl = redis.call('TTL', key)
        return {current, ttl}
    ";

    public RedisRateLimitStorage(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimitStorage> logger,
        string keyPrefix = "rate_limit:")
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _database = _redis.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix;
    }

    /// <summary>
    /// Increments counter using fixed window algorithm
    /// </summary>
    public async Task<(long count, DateTime expiration)> IncrementAsync(string key, int windowSizeSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{_keyPrefix}{key}";
            var result = await _database.ScriptEvaluateAsync(
                FixedWindowScript,
                new RedisKey[] { fullKey },
                new RedisValue[] { windowSizeSeconds });

            var values = (RedisValue[])result!;
            var count = (long)values[0];
            var ttl = (int)values[1];
            var expiration = DateTime.UtcNow.AddSeconds(ttl);

            _logger.LogDebug("Fixed window increment for key {Key}: count={Count}, ttl={TTL}s", 
                key, count, ttl);

            return (count, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing rate limit counter for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets current count using fixed window
    /// </summary>
    public async Task<(long count, DateTime expiration)> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{_keyPrefix}{key}";
            var tasks = new Task[]
            {
                _database.StringGetAsync(fullKey),
                _database.KeyTimeToLiveAsync(fullKey)
            };

            await Task.WhenAll(tasks);

            var count = (long)((Task<RedisValue>)tasks[0]).Result;
            var ttl = ((Task<TimeSpan?>)tasks[1]).Result;
            var expiration = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : DateTime.UtcNow;

            _logger.LogDebug("Get count for key {Key}: count={Count}, ttl={TTL}", 
                key, count, ttl?.TotalSeconds ?? 0);

            return (count, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit count for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Resets counter for a key
    /// </summary>
    public async Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{_keyPrefix}{key}";
            var result = await _database.KeyDeleteAsync(fullKey);

            _logger.LogDebug("Reset rate limit for key {Key}: success={Success}", key, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for key {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Increments using sliding window algorithm for more accurate rate limiting
    /// </summary>
    public async Task<(long count, bool isExceeded)> SlidingWindowIncrementAsync(string key, int windowSizeSeconds, int limit, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{_keyPrefix}sw:{key}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var result = await _database.ScriptEvaluateAsync(
                SlidingWindowScript,
                new RedisKey[] { fullKey },
                new RedisValue[] { windowSizeSeconds, limit, now });

            var values = (RedisValue[])result!;
            var count = (long)values[0];
            var isExceeded = (int)values[1] == 1;

            _logger.LogDebug("Sliding window increment for key {Key}: count={Count}, limit={Limit}, exceeded={Exceeded}", 
                key, count, limit, isExceeded);

            return (count, isExceeded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sliding window increment for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets health status of Redis connection
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ping = await _database.PingAsync();
            return ping.TotalMilliseconds < 1000; // Consider healthy if ping < 1s
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed");
            return false;
        }
    }

    /// <summary>
    /// Gets Redis connection statistics
    /// </summary>
    public async Task<Dictionary<string, object>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync();
            
            var stats = new Dictionary<string, object>();
            
            foreach (var group in info)
            {
                foreach (var item in group)
                {
                    stats[$"redis_{group.Key}_{item.Key}"] = item.Value;
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis statistics");
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    /// <summary>
    /// Cleans up expired keys (maintenance operation)
    /// </summary>
    public async Task CleanupExpiredKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_keyPrefix}*");

            var expiredCount = 0;
            foreach (var key in keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var ttl = await _database.KeyTimeToLiveAsync(key);
                if (!ttl.HasValue || ttl.Value.TotalSeconds <= 0)
                {
                    await _database.KeyDeleteAsync(key);
                    expiredCount++;
                }
            }

            _logger.LogInformation("Cleaned up {ExpiredCount} expired rate limit keys", expiredCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup expired rate limit keys");
        }
    }

    public void Dispose()
    {
        // Don't dispose the connection multiplexer as it's shared
        // The DI container will handle disposal
    }
}
