using Event.Domain.Configuration;
using Event.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Redis-based distributed rate limiting service
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _redisDatabase;
    private readonly RateLimitConfiguration _config;
    private readonly ILogger<RateLimitService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RateLimitService(
        IDistributedCache cache,
        IConnectionMultiplexer? connectionMultiplexer,
        IOptions<RateLimitConfiguration> config,
        ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _redisDatabase = connectionMultiplexer?.GetDatabase();
        _config = config.Value;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        string method,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check IP-based rate limits first (more restrictive)
            var ipResult = await CheckRateLimitForKeyAsync($"ip:{ipAddress}:{endpoint}", _config.IpRateLimit, endpoint);
            if (ipResult.IsBlocked)
            {
                ipResult.Reason = "IP rate limit exceeded";
                return ipResult;
            }

            // Check client-based rate limits
            var clientResult = await CheckRateLimitForKeyAsync($"client:{clientId}:{endpoint}", _config.ClientRateLimit, endpoint);
            if (clientResult.IsBlocked)
            {
                clientResult.Reason = "Client rate limit exceeded";
                return clientResult;
            }

            // Check organization-based rate limits if applicable
            if (!string.IsNullOrEmpty(organizationId))
            {
                var orgResult = await CheckRateLimitForKeyAsync($"org:{organizationId}:{endpoint}", _config.Default, endpoint);
                if (orgResult.IsBlocked)
                {
                    orgResult.Reason = "Organization rate limit exceeded";
                    return orgResult;
                }
            }

            // Check endpoint-specific rate limits
            var endpointRule = GetEndpointRule(endpoint, method);
            if (endpointRule != null)
            {
                var endpointResult = await CheckEndpointRateLimitAsync(clientId, ipAddress, endpoint, endpointRule);
                if (endpointResult.IsBlocked)
                {
                    return endpointResult;
                }
            }

            // Return the most restrictive non-blocked result
            var results = new[] { ipResult, clientResult };
            return results.OrderByDescending(r => r.CurrentCount).First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for client {ClientId}", clientId);
            
            // In case of error, don't block requests
            return new RateLimitResult
            {
                IsBlocked = false,
                CurrentCount = 0,
                Limit = long.MaxValue,
                Period = TimeSpan.FromMinutes(1),
                ResetTime = DateTime.UtcNow.AddMinutes(1),
                RetryAfter = TimeSpan.Zero,
                Endpoint = endpoint,
                Reason = "Rate limiting service unavailable"
            };
        }
    }

    public async Task RecordRequestAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        string method,
        bool wasBlocked,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new List<Task>();

            // Record for IP
            tasks.Add(IncrementCounterAsync($"ip:{ipAddress}:{endpoint}"));

            // Record for client
            tasks.Add(IncrementCounterAsync($"client:{clientId}:{endpoint}"));

            // Record for organization if applicable
            if (!string.IsNullOrEmpty(organizationId))
            {
                tasks.Add(IncrementCounterAsync($"org:{organizationId}:{endpoint}"));
            }

            // Record metrics if enabled
            if (_config.EnableMetrics)
            {
                tasks.Add(RecordMetricsAsync(clientId, ipAddress, endpoint, method, wasBlocked));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for client {ClientId}", clientId);
        }
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"client:{clientId}:{endpoint}";
            var count = await GetCurrentCountAsync(key);
            var policy = _config.ClientRateLimit;
            var rule = policy.Rules.FirstOrDefault() ?? new RateLimitRule { Limit = 1000, Period = "1h" };

            var windowStart = GetWindowStart(rule.PeriodTimespan);
            var windowEnd = windowStart.Add(rule.PeriodTimespan);

            return new RateLimitStatus
            {
                ClientId = clientId,
                IpAddress = ipAddress,
                Endpoint = endpoint,
                CurrentCount = count,
                Limit = rule.Limit,
                RemainingRequests = Math.Max(0, rule.Limit - count),
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                IsBlocked = count >= rule.Limit,
                TimeUntilReset = windowEnd - DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit status for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<IEnumerable<RateLimitMetrics>> GetMetricsAsync(
        TimeSpan? window = null,
        string? clientId = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = new List<RateLimitMetrics>();
            var searchWindow = window ?? TimeSpan.FromHours(1);
            var keys = await GetMetricsKeysAsync(searchWindow, clientId, endpoint);

            foreach (var key in keys)
            {
                var metricsData = await GetMetricsDataAsync(key);
                if (metricsData != null)
                {
                    metrics.Add(metricsData);
                }
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit metrics");
            return Array.Empty<RateLimitMetrics>();
        }
    }

    public async Task ClearRateLimitAsync(
        string? clientId = null,
        string? ipAddress = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = new List<string>();

            if (!string.IsNullOrEmpty(clientId))
            {
                patterns.Add($"client:{clientId}:*");
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                patterns.Add($"ip:{ipAddress}:*");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                patterns.Add($"*:{endpoint}");
            }

            if (patterns.Count == 0)
            {
                patterns.Add("*"); // Clear all (admin operation)
            }

            foreach (var pattern in patterns)
            {
                await ClearKeysAsync(pattern);
            }

            _logger.LogInformation("Cleared rate limit data for patterns: {Patterns}", string.Join(", ", patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing rate limit data");
            throw;
        }
    }

    public async Task AddToWhitelistAsync(
        string clientId,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"whitelist:{clientId}";
            var expiry = duration ?? TimeSpan.FromHours(24);
            
            await _cache.SetStringAsync(key, "true", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            }, cancellationToken);

            _logger.LogInformation("Added client {ClientId} to whitelist for {Duration}", clientId, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding client {ClientId} to whitelist", clientId);
            throw;
        }
    }

    public async Task RemoveFromWhitelistAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"whitelist:{clientId}";
            await _cache.RemoveAsync(key, cancellationToken);

            _logger.LogInformation("Removed client {ClientId} from whitelist", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing client {ClientId} from whitelist", clientId);
            throw;
        }
    }

    public async Task<bool> IsWhitelistedAsync(
        string? clientId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check configuration-based whitelists
            if (!string.IsNullOrEmpty(clientId) && _config.ClientWhitelist.Contains(clientId))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(ipAddress) && _config.IpWhitelist.Contains(ipAddress))
            {
                return true;
            }

            // Check dynamic whitelists
            if (!string.IsNullOrEmpty(clientId))
            {
                var key = $"whitelist:{clientId}";
                var result = await _cache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(result))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                var key = $"whitelist:ip:{ipAddress}";
                var result = await _cache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(result))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking whitelist status");
            return false; // Fail secure
        }
    }

    public RateLimitConfiguration GetConfiguration()
    {
        return _config;
    }

    public async Task UpdateConfigurationAsync(
        RateLimitConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Note: This would typically update a configuration store
        // For now, we'll just log the request
        _logger.LogInformation("Rate limit configuration update requested");
        await Task.CompletedTask;
    }

    #region Private Methods

    private async Task<RateLimitResult> CheckRateLimitForKeyAsync(string key, RateLimitPolicy policy, string endpoint)
    {
        var mostRestrictiveResult = new RateLimitResult
        {
            IsBlocked = false,
            CurrentCount = 0,
            Limit = long.MaxValue,
            Period = TimeSpan.FromMinutes(1),
            ResetTime = DateTime.UtcNow.AddMinutes(1),
            RetryAfter = TimeSpan.Zero,
            Endpoint = endpoint
        };

        foreach (var rule in policy.Rules)
        {
            var ruleKey = $"{key}:{rule.Period}";
            var count = await GetCurrentCountAsync(ruleKey);
            var period = ParsePeriod(rule.Period);
            var windowStart = GetWindowStart(period);
            var resetTime = windowStart.Add(period);

            var result = new RateLimitResult
            {
                IsBlocked = count >= rule.Limit,
                CurrentCount = count,
                Limit = rule.Limit,
                Period = period,
                ResetTime = resetTime,
                RetryAfter = resetTime - DateTime.UtcNow,
                ViolatedRule = rule,
                Endpoint = endpoint
            };

            if (result.IsBlocked)
            {
                return result; // Return immediately if any rule is violated
            }

            // Track the most restrictive non-blocked rule
            if (count > mostRestrictiveResult.CurrentCount)
            {
                mostRestrictiveResult = result;
            }
        }

        return mostRestrictiveResult;
    }

    private async Task<RateLimitResult> CheckEndpointRateLimitAsync(
        string clientId,
        string ipAddress,
        string endpoint,
        EndpointRateLimitRule rule)
    {
        var period = ParsePeriod(rule.Period);
        var windowStart = GetWindowStart(period);
        var resetTime = windowStart.Add(period);

        // Check IP-specific limit for this endpoint
        if (rule.LimitPerIP.HasValue)
        {
            var ipKey = $"endpoint:ip:{ipAddress}:{rule.Endpoint}:{rule.Period}";
            var ipCount = await GetCurrentCountAsync(ipKey);
            
            if (ipCount >= rule.LimitPerIP.Value)
            {
                return new RateLimitResult
                {
                    IsBlocked = true,
                    CurrentCount = ipCount,
                    Limit = rule.LimitPerIP.Value,
                    Period = period,
                    ResetTime = resetTime,
                    RetryAfter = resetTime - DateTime.UtcNow,
                    Endpoint = endpoint,
                    Reason = "Endpoint IP rate limit exceeded"
                };
            }
        }

        // Check client-specific limit for this endpoint
        if (rule.LimitPerClient.HasValue)
        {
            var clientKey = $"endpoint:client:{clientId}:{rule.Endpoint}:{rule.Period}";
            var clientCount = await GetCurrentCountAsync(clientKey);
            
            if (clientCount >= rule.LimitPerClient.Value)
            {
                return new RateLimitResult
                {
                    IsBlocked = true,
                    CurrentCount = clientCount,
                    Limit = rule.LimitPerClient.Value,
                    Period = period,
                    ResetTime = resetTime,
                    RetryAfter = resetTime - DateTime.UtcNow,
                    Endpoint = endpoint,
                    Reason = "Endpoint client rate limit exceeded"
                };
            }
        }

        return new RateLimitResult
        {
            IsBlocked = false,
            CurrentCount = 0,
            Limit = long.MaxValue,
            Period = period,
            ResetTime = resetTime,
            RetryAfter = TimeSpan.Zero,
            Endpoint = endpoint
        };
    }

    private EndpointRateLimitRule? GetEndpointRule(string endpoint, string method)
    {
        return _config.EndpointRules.FirstOrDefault(rule =>
            (rule.Method == "*" || rule.Method.Equals(method, StringComparison.OrdinalIgnoreCase)) &&
            IsEndpointMatch(endpoint, rule.Endpoint));
    }

    private bool IsEndpointMatch(string endpoint, string pattern)
    {
        if (pattern == "*") return true;
        if (pattern.Contains("*"))
        {
            // Simple wildcard matching
            var regexPattern = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(endpoint, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return endpoint.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<long> GetCurrentCountAsync(string key)
    {
        try
        {
            if (_redisDatabase != null)
            {
                var value = await _redisDatabase.StringGetAsync(key);
                return value.HasValue ? (long)value : 0;
            }
            else
            {
                var value = await _cache.GetStringAsync(key);
                return long.TryParse(value, out var count) ? count : 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    private async Task IncrementCounterAsync(string key)
    {
        try
        {
            var period = TimeSpan.FromMinutes(1); // Default period for counters
            var expiry = GetWindowStart(period).Add(period) - DateTime.UtcNow;

            if (_redisDatabase != null)
            {
                await _redisDatabase.StringIncrementAsync(key);
                await _redisDatabase.KeyExpireAsync(key, expiry);
            }
            else
            {
                await _semaphore.WaitAsync();
                try
                {
                    var currentValue = await _cache.GetStringAsync(key);
                    var newValue = (long.TryParse(currentValue, out var count) ? count : 0) + 1;
                    
                    await _cache.SetStringAsync(key, newValue.ToString(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry
                    });
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter for key {Key}", key);
        }
    }

    private async Task RecordMetricsAsync(string clientId, string ipAddress, string endpoint, string method, bool wasBlocked)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var metricsKey = $"metrics:{timestamp:yyyyMMddHH}:{clientId}:{endpoint}";
            
            var metrics = new RateLimitMetrics
            {
                Endpoint = endpoint,
                ClientId = clientId,
                IpAddress = ipAddress,
                RequestCount = wasBlocked ? 0 : 1,
                BlockedCount = wasBlocked ? 1 : 0,
                WindowStart = timestamp.Date.AddHours(timestamp.Hour),
                WindowEnd = timestamp.Date.AddHours(timestamp.Hour + 1)
            };

            var json = JsonSerializer.Serialize(metrics);
            await _cache.SetStringAsync(metricsKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording metrics");
        }
    }

    private async Task<List<string>> GetMetricsKeysAsync(TimeSpan window, string? clientId, string? endpoint)
    {
        // This would typically use Redis SCAN or similar to find keys
        // For now, return empty list
        await Task.CompletedTask;
        return new List<string>();
    }

    private async Task<RateLimitMetrics?> GetMetricsDataAsync(string key)
    {
        try
        {
            var json = await _cache.GetStringAsync(key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<RateLimitMetrics>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task ClearKeysAsync(string pattern)
    {
        // This would typically use Redis to delete keys matching pattern
        // For distributed cache, we'd need to track keys separately
        await Task.CompletedTask;
    }

    private TimeSpan ParsePeriod(string period)
    {
        if (string.IsNullOrEmpty(period)) return TimeSpan.FromMinutes(1);

        var unit = period.Last();
        var value = period.Substring(0, period.Length - 1);

        if (!int.TryParse(value, out var number)) return TimeSpan.FromMinutes(1);

        return unit switch
        {
            's' => TimeSpan.FromSeconds(number),
            'm' => TimeSpan.FromMinutes(number),
            'h' => TimeSpan.FromHours(number),
            'd' => TimeSpan.FromDays(number),
            _ => TimeSpan.FromMinutes(1)
        };
    }

    private DateTime GetWindowStart(TimeSpan period)
    {
        var now = DateTime.UtcNow;
        
        if (period.TotalDays >= 1)
        {
            return now.Date;
        }
        else if (period.TotalHours >= 1)
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        }
        else if (period.TotalMinutes >= 1)
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
        }
        else
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
        }
    }

    #endregion
}
