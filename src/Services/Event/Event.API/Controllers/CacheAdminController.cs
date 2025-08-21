using Event.Domain.Configuration;
using Event.Application.Interfaces;
using Event.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Event.API.Controllers;

/// <summary>
/// Administrative controller for cache management
/// </summary>
[ApiController]
[Route("api/v1/admin/cache")]
[ApiVersion("1.0")]
public class CacheAdminController : ControllerBase
{
    private readonly IAdvancedCacheService _cacheService;
    private readonly ICacheWarmupService _warmupService;
    private readonly ICacheInvalidationService _invalidationService;
    private readonly ILogger<CacheAdminController> _logger;

    public CacheAdminController(
        IAdvancedCacheService cacheService,
        ICacheWarmupService warmupService,
        ICacheInvalidationService invalidationService,
        ILogger<CacheAdminController> logger)
    {
        _cacheService = cacheService;
        _warmupService = warmupService;
        _invalidationService = invalidationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets cache metrics and statistics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<CacheMetrics>> GetMetrics(CancellationToken cancellationToken = default)
    {
        var metrics = await _cacheService.GetMetricsAsync(cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Gets cache size information
    /// </summary>
    [HttpGet("size")]
    public async Task<ActionResult<CacheSizeInfo>> GetSizeInfo(CancellationToken cancellationToken = default)
    {
        var sizeInfo = await _cacheService.GetSizeInfoAsync(cancellationToken);
        return Ok(sizeInfo);
    }

    /// <summary>
    /// Gets information about a specific cache key
    /// </summary>
    [HttpGet("keys/{key}")]
    public async Task<ActionResult<CacheKeyInfo>> GetKeyInfo(
        string key,
        CancellationToken cancellationToken = default)
    {
        var keyInfo = await _cacheService.GetKeyInfoAsync(key, cancellationToken);
        
        if (keyInfo == null)
        {
            return NotFound($"Cache key '{key}' not found");
        }

        return Ok(keyInfo);
    }

    /// <summary>
    /// Gets all cache keys matching a pattern
    /// </summary>
    [HttpGet("keys")]
    public async Task<ActionResult<IEnumerable<string>>> GetKeys(
        [FromQuery] string pattern = "*",
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var keys = await _cacheService.GetKeysAsync(pattern, cancellationToken);
        var limitedKeys = keys.Take(limit);
        
        return Ok(new
        {
            Pattern = pattern,
            Limit = limit,
            Keys = limitedKeys,
            TotalFound = keys.Count()
        });
    }

    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    [HttpGet("values/{key}")]
    public async Task<ActionResult<object>> GetValue(
        string key,
        CancellationToken cancellationToken = default)
    {
        var result = await _cacheService.GetWithResultAsync<object>(key, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        if (!result.WasFromCache)
        {
            return NotFound($"Cache key '{key}' not found");
        }

        return Ok(new
        {
            Key = key,
            Value = result.Value,
            WasFromCache = result.WasFromCache,
            OperationTime = result.OperationTime,
            Timestamp = result.Timestamp
        });
    }

    /// <summary>
    /// Sets a cache value
    /// </summary>
    [HttpPost("values/{key}")]
    public async Task<ActionResult> SetValue(
        string key,
        [FromBody] SetCacheValueRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.SetAsync(key, request.Value, request.Expiration, cancellationToken);
            
            _logger.LogInformation("Cache value set by admin for key: {Key}", key);
            
            return Ok(new { message = $"Cache value set for key '{key}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Removes a cache entry
    /// </summary>
    [HttpDelete("values/{key}")]
    public async Task<ActionResult> RemoveValue(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveAsync(key, cancellationToken);
        
        _logger.LogInformation("Cache value removed by admin for key: {Key}", key);
        
        return Ok(new { message = $"Cache value removed for key '{key}'" });
    }

    /// <summary>
    /// Removes cache entries by pattern
    /// </summary>
    [HttpDelete("patterns")]
    public async Task<ActionResult> RemoveByPattern(
        [FromQuery, Required] string pattern,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        
        _logger.LogInformation("Cache values removed by admin for pattern: {Pattern}", pattern);
        
        return Ok(new { message = $"Cache values removed for pattern '{pattern}'" });
    }

    /// <summary>
    /// Removes cache entries by tag
    /// </summary>
    [HttpDelete("tags/{tag}")]
    public async Task<ActionResult> RemoveByTag(
        string tag,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByTagAsync(tag, cancellationToken);
        
        _logger.LogInformation("Cache values removed by admin for tag: {Tag}", tag);
        
        return Ok(new { message = $"Cache values removed for tag '{tag}'" });
    }

    /// <summary>
    /// Removes cache entries by multiple tags
    /// </summary>
    [HttpDelete("tags")]
    public async Task<ActionResult> RemoveByTags(
        [FromBody] RemoveByTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByTagsAsync(request.Tags, cancellationToken);
        
        _logger.LogInformation("Cache values removed by admin for tags: {Tags}", string.Join(", ", request.Tags));
        
        return Ok(new { message = $"Cache values removed for {request.Tags.Count} tags" });
    }

    /// <summary>
    /// Extends the expiration of a cache entry
    /// </summary>
    [HttpPost("values/{key}/extend")]
    public async Task<ActionResult> ExtendExpiration(
        string key,
        [FromBody] ExtendExpirationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.ExtendExpirationAsync(key, request.Extension, cancellationToken);
        
        _logger.LogInformation("Cache expiration extended by admin for key: {Key}, Extension: {Extension}", 
            key, request.Extension);
        
        return Ok(new { message = $"Cache expiration extended for key '{key}' by {request.Extension}" });
    }

    /// <summary>
    /// Sets the expiration of a cache entry
    /// </summary>
    [HttpPost("values/{key}/expire")]
    public async Task<ActionResult> SetExpiration(
        string key,
        [FromBody] SetExpirationRequest request,
        CancellationToken cancellationToken = default)
    {
        await _cacheService.SetExpirationAsync(key, request.Expiration, cancellationToken);
        
        _logger.LogInformation("Cache expiration set by admin for key: {Key}, Expiration: {Expiration}", 
            key, request.Expiration);
        
        return Ok(new { message = $"Cache expiration set for key '{key}' to {request.Expiration}" });
    }

    /// <summary>
    /// Gets the remaining TTL for a cache entry
    /// </summary>
    [HttpGet("values/{key}/ttl")]
    public async Task<ActionResult<object>> GetTimeToLive(
        string key,
        CancellationToken cancellationToken = default)
    {
        var ttl = await _cacheService.GetTimeToLiveAsync(key, cancellationToken);
        
        if (ttl == null)
        {
            return NotFound($"Cache key '{key}' not found");
        }

        return Ok(new
        {
            Key = key,
            TimeToLive = ttl,
            ExpiresAt = DateTime.UtcNow.Add(ttl.Value),
            IsExpired = ttl.Value <= TimeSpan.Zero
        });
    }

    /// <summary>
    /// Refreshes a cache entry (if the refresh logic is available)
    /// </summary>
    [HttpPost("values/{key}/refresh")]
    public async Task<ActionResult> RefreshValue(
        string key,
        CancellationToken cancellationToken = default)
    {
        // This would require a registry of refresh functions
        // For now, we'll just remove the key to force a refresh on next access
        await _cacheService.RemoveAsync(key, cancellationToken);
        
        _logger.LogInformation("Cache key invalidated for refresh by admin: {Key}", key);
        
        return Ok(new { message = $"Cache key '{key}' invalidated for refresh" });
    }

    /// <summary>
    /// Triggers cache warmup
    /// </summary>
    [HttpPost("warmup")]
    public async Task<ActionResult> TriggerWarmup(CancellationToken cancellationToken = default)
    {
        try
        {
            await _warmupService.WarmupAsync(cancellationToken);
            
            _logger.LogInformation("Cache warmup triggered by admin");
            
            return Ok(new { message = "Cache warmup completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin-triggered cache warmup");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Triggers warmup for a specific data type
    /// </summary>
    [HttpPost("warmup/{dataType}")]
    public async Task<ActionResult> TriggerWarmupForDataType(
        string dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _warmupService.WarmupDataTypeAsync(dataType, cancellationToken);
            
            _logger.LogInformation("Cache warmup triggered by admin for data type: {DataType}", dataType);
            
            return Ok(new { message = $"Cache warmup completed for data type '{dataType}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin-triggered cache warmup for data type: {DataType}", dataType);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets active invalidation strategies
    /// </summary>
    [HttpGet("invalidation/strategies")]
    public async Task<ActionResult<IEnumerable<InvalidationStrategy>>> GetInvalidationStrategies(
        CancellationToken cancellationToken = default)
    {
        var strategies = await _invalidationService.GetStrategiesAsync(cancellationToken);
        return Ok(strategies);
    }

    /// <summary>
    /// Registers a new invalidation strategy
    /// </summary>
    [HttpPost("invalidation/strategies")]
    public async Task<ActionResult> RegisterInvalidationStrategy(
        [FromBody] InvalidationStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _invalidationService.RegisterStrategyAsync(strategy, cancellationToken);
            
            _logger.LogInformation("Invalidation strategy registered by admin: {DataType}", strategy.DataType);
            
            return Ok(new { message = $"Invalidation strategy registered for '{strategy.DataType}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering invalidation strategy: {DataType}", strategy.DataType);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Triggers manual invalidation for an event type
    /// </summary>
    [HttpPost("invalidation/trigger")]
    public async Task<ActionResult> TriggerInvalidation(
        [FromBody] TriggerInvalidationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _invalidationService.InvalidateOnEventAsync(
                request.EventType, 
                request.EventData, 
                cancellationToken);
            
            _logger.LogInformation("Manual invalidation triggered by admin for event type: {EventType}", 
                request.EventType);
            
            return Ok(new { message = $"Invalidation triggered for event type '{request.EventType}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering manual invalidation: {EventType}", request.EventType);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clears all cache metrics
    /// </summary>
    [HttpDelete("metrics")]
    public async Task<ActionResult> ClearMetrics(CancellationToken cancellationToken = default)
    {
        await _cacheService.ClearMetricsAsync(cancellationToken);
        
        _logger.LogInformation("Cache metrics cleared by admin");
        
        return Ok(new { message = "Cache metrics cleared successfully" });
    }

    /// <summary>
    /// Gets cache health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetCacheHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache connectivity
            var testKey = $"health-check:{Guid.NewGuid()}";
            var testValue = DateTime.UtcNow.ToString();
            
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1), cancellationToken);
            var retrievedValue = await _cacheService.GetAsync<string>(testKey, cancellationToken);
            await _cacheService.RemoveAsync(testKey, cancellationToken);
            
            var isHealthy = retrievedValue == testValue;
            var metrics = await _cacheService.GetMetricsAsync(cancellationToken);
            
            return Ok(new
            {
                IsHealthy = isHealthy,
                Timestamp = DateTime.UtcNow,
                Metrics = metrics,
                TestResult = new
                {
                    TestKey = testKey,
                    TestPassed = isHealthy,
                    Expected = testValue,
                    Actual = retrievedValue
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return Ok(new
            {
                IsHealthy = false,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}

#region Request Models

public class SetCacheValueRequest
{
    [Required]
    public object Value { get; set; } = null!;
    
    public TimeSpan? Expiration { get; set; }
}

public class RemoveByTagsRequest
{
    [Required]
    public List<string> Tags { get; set; } = new();
}

public class ExtendExpirationRequest
{
    [Required]
    public TimeSpan Extension { get; set; }
}

public class SetExpirationRequest
{
    [Required]
    public TimeSpan Expiration { get; set; }
}

public class TriggerInvalidationRequest
{
    [Required]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public object EventData { get; set; } = null!;
}

#endregion
