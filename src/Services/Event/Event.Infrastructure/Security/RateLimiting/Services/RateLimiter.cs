using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Event.Infrastructure.Security.RateLimiting.Services;

/// <summary>
/// Main rate limiting service that orchestrates rate limit checks
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly IRateLimitStorage _storage;
    private readonly IRateLimitKeyGenerator _keyGenerator;
    private readonly IRateLimitRuleProvider _ruleProvider;
    private readonly IRateLimitMetrics _metrics;
    private readonly RateLimitConfiguration _configuration;
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(
        IRateLimitStorage storage,
        IRateLimitKeyGenerator keyGenerator,
        IRateLimitRuleProvider ruleProvider,
        IRateLimitMetrics metrics,
        IOptions<RateLimitConfiguration> configuration,
        ILogger<RateLimiter> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _ruleProvider = ruleProvider ?? throw new ArgumentNullException(nameof(ruleProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a request is allowed based on all applicable rate limiting rules
    /// </summary>
    public async Task<RateLimitResult> CheckAsync(RateLimitContext context, CancellationToken cancellationToken = default)
    {
        if (!_configuration.IsEnabled)
        {
            return new RateLimitResult { IsAllowed = true };
        }

        var stopwatch = Stopwatch.StartNew();
        RateLimitResult? result = null;

        try
        {
            // Get applicable rules ordered by priority
            var rules = await _ruleProvider.GetApplicableRulesAsync(context, cancellationToken);
            
            if (!rules.Any())
            {
                result = new RateLimitResult { IsAllowed = true };
                return result;
            }

            // Check each rule until one fails or all pass
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var key = _keyGenerator.GenerateKey(rule, context);
                var ruleResult = await CheckRuleAsync(rule, key, cancellationToken);

                if (!ruleResult.IsAllowed)
                {
                    result = ruleResult;
                    _metrics.RecordRateLimitViolation(context, rule);
                    _logger.LogWarning("Rate limit exceeded for rule {RuleId}, key {Key}: {CurrentCount}/{Limit}",
                        rule.Id, key, ruleResult.CurrentCount, ruleResult.Limit);
                    break;
                }
            }

            result ??= new RateLimitResult { IsAllowed = true };
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limits for context");
            _metrics.RecordRateLimitError(context, ex);

            // Fail open if configured to do so
            if (_configuration.FailOpen)
            {
                result = new RateLimitResult { IsAllowed = true };
                return result;
            }

            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (result != null)
            {
                _metrics.RecordRateLimitCheck(context, result, stopwatch.Elapsed);
            }
        }
    }

    /// <summary>
    /// Checks if a request is allowed for a specific rule
    /// </summary>
    public async Task<RateLimitResult> CheckRuleAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(_configuration.TimeoutMilliseconds);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            if (_configuration.UseSlidingWindow)
            {
                var (count, isExceeded) = await _storage.SlidingWindowIncrementAsync(
                    key, rule.WindowSizeInSeconds, rule.Limit, combinedCts.Token);

                return new RateLimitResult
                {
                    IsAllowed = !isExceeded,
                    AppliedRule = rule,
                    CurrentCount = count,
                    Limit = rule.Limit,
                    ResetTimeInSeconds = rule.WindowSizeInSeconds,
                    RetryAfterSeconds = isExceeded ? rule.WindowSizeInSeconds : null,
                    Metadata = new Dictionary<string, object>
                    {
                        { "algorithm", "sliding_window" },
                        { "key", key }
                    }
                };
            }
            else
            {
                var (count, expiration) = await _storage.IncrementAsync(
                    key, rule.WindowSizeInSeconds, combinedCts.Token);

                var isAllowed = count <= rule.Limit;
                var resetTime = (int)(expiration - DateTime.UtcNow).TotalSeconds;

                return new RateLimitResult
                {
                    IsAllowed = isAllowed,
                    AppliedRule = rule,
                    CurrentCount = count,
                    Limit = rule.Limit,
                    ResetTimeInSeconds = Math.Max(0, resetTime),
                    RetryAfterSeconds = isAllowed ? null : Math.Max(0, resetTime),
                    Metadata = new Dictionary<string, object>
                    {
                        { "algorithm", "fixed_window" },
                        { "key", key }
                    }
                };
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Rate limit check timed out for rule {RuleId}, key {Key}", rule.Id, key);
            
            if (_configuration.FailOpen)
            {
                return new RateLimitResult { IsAllowed = true };
            }

            throw new TimeoutException($"Rate limit check timed out after {_configuration.TimeoutMilliseconds}ms");
        }
    }

    /// <summary>
    /// Gets the current rate limit status for a key without incrementing
    /// </summary>
    public async Task<RateLimitResult> GetStatusAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var (count, expiration) = await _storage.GetCountAsync(key, cancellationToken);
            var resetTime = (int)(expiration - DateTime.UtcNow).TotalSeconds;

            return new RateLimitResult
            {
                IsAllowed = count < rule.Limit,
                AppliedRule = rule,
                CurrentCount = count,
                Limit = rule.Limit,
                ResetTimeInSeconds = Math.Max(0, resetTime),
                Metadata = new Dictionary<string, object>
                {
                    { "status_only", true },
                    { "key", key }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit status for rule {RuleId}, key {Key}", rule.Id, key);
            throw;
        }
    }

    /// <summary>
    /// Resets the rate limit for a specific key
    /// </summary>
    public async Task<bool> ResetAsync(RateLimitRule rule, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _storage.ResetAsync(key, cancellationToken);
            
            if (result)
            {
                _logger.LogInformation("Reset rate limit for rule {RuleId}, key {Key}", rule.Id, key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for rule {RuleId}, key {Key}", rule.Id, key);
            return false;
        }
    }
}
