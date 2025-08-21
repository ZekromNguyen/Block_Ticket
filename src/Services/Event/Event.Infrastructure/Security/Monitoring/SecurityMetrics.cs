using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Event.Infrastructure.Security.Monitoring;

/// <summary>
/// Collects and tracks security-related metrics
/// </summary>
public class SecurityMetrics : IRateLimitMetrics, IDisposable
{
    private readonly ILogger<SecurityMetrics> _logger;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _rateLimitChecksCounter;
    private readonly Counter<long> _rateLimitViolationsCounter;
    private readonly Counter<long> _rateLimitErrorsCounter;
    private readonly Counter<long> _authenticationFailuresCounter;
    private readonly Counter<long> _authorizationViolationsCounter;
    private readonly Counter<long> _suspiciousActivitiesCounter;
    private readonly Counter<long> _inputValidationFailuresCounter;
    private readonly Counter<long> _securityHeadersAppliedCounter;

    // Histograms
    private readonly Histogram<double> _rateLimitCheckDuration;
    private readonly Histogram<double> _requestSizeHistogram;

    // In-memory storage for recent events (for alerting)
    private readonly ConcurrentQueue<SecurityEvent> _recentEvents;
    private readonly Timer _cleanupTimer;

    public SecurityMetrics(ILogger<SecurityMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meter = new Meter("Event.Security", "1.0.0");

        // Initialize counters
        _rateLimitChecksCounter = _meter.CreateCounter<long>("security_rate_limit_checks_total", "requests", "Total number of rate limit checks");
        _rateLimitViolationsCounter = _meter.CreateCounter<long>("security_rate_limit_violations_total", "violations", "Total number of rate limit violations");
        _rateLimitErrorsCounter = _meter.CreateCounter<long>("security_rate_limit_errors_total", "errors", "Total number of rate limit errors");
        _authenticationFailuresCounter = _meter.CreateCounter<long>("security_authentication_failures_total", "failures", "Total number of authentication failures");
        _authorizationViolationsCounter = _meter.CreateCounter<long>("security_authorization_violations_total", "violations", "Total number of authorization violations");
        _suspiciousActivitiesCounter = _meter.CreateCounter<long>("security_suspicious_activities_total", "activities", "Total number of suspicious activities detected");
        _inputValidationFailuresCounter = _meter.CreateCounter<long>("security_input_validation_failures_total", "failures", "Total number of input validation failures");
        _securityHeadersAppliedCounter = _meter.CreateCounter<long>("security_headers_applied_total", "headers", "Total number of security headers applied");

        // Initialize histograms
        _rateLimitCheckDuration = _meter.CreateHistogram<double>("security_rate_limit_check_duration_ms", "ms", "Duration of rate limit checks");
        _requestSizeHistogram = _meter.CreateHistogram<double>("security_request_size_bytes", "bytes", "Size of incoming requests");

        // Initialize in-memory storage
        _recentEvents = new ConcurrentQueue<SecurityEvent>();
        _cleanupTimer = new Timer(CleanupOldEvents, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    #region IRateLimitMetrics Implementation

    public void RecordRateLimitCheck(RateLimitContext context, RateLimitResult result, TimeSpan duration)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("rule_type", result.AppliedRule?.Type.ToString() ?? "unknown"),
            new("is_allowed", result.IsAllowed),
            new("client_ip", GetHashedIp(context.IpAddress)),
            new("endpoint", context.Endpoint ?? "unknown")
        };

        _rateLimitChecksCounter.Add(1, tags);
        _rateLimitCheckDuration.Record(duration.TotalMilliseconds, tags);

        // Log detailed information
        _logger.LogDebug("Rate limit check: {IsAllowed}, Duration: {Duration}ms, Rule: {RuleType}",
            result.IsAllowed, duration.TotalMilliseconds, result.AppliedRule?.Type);
    }

    public void RecordRateLimitViolation(RateLimitContext context, RateLimitRule rule)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("rule_type", rule.Type.ToString()),
            new("rule_id", rule.Id),
            new("client_ip", GetHashedIp(context.IpAddress)),
            new("endpoint", context.Endpoint ?? "unknown"),
            new("client_id", context.ClientId ?? "anonymous")
        };

        _rateLimitViolationsCounter.Add(1, tags);

        // Record as security event for alerting
        var securityEvent = new SecurityEvent
        {
            Type = SecurityEventType.RateLimitViolation,
            Timestamp = DateTime.UtcNow,
            IpAddress = context.IpAddress,
            ClientId = context.ClientId,
            Endpoint = context.Endpoint,
            Details = new Dictionary<string, object>
            {
                { "rule_type", rule.Type },
                { "rule_id", rule.Id },
                { "limit", rule.Limit },
                { "window_seconds", rule.WindowSizeInSeconds }
            }
        };

        _recentEvents.Enqueue(securityEvent);

        _logger.LogWarning("Rate limit violation: {RuleType} rule {RuleId} exceeded by {ClientIp} on {Endpoint}",
            rule.Type, rule.Id, GetHashedIp(context.IpAddress), context.Endpoint);
    }

    public void RecordRateLimitError(RateLimitContext context, Exception error)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("error_type", error.GetType().Name),
            new("client_ip", GetHashedIp(context.IpAddress)),
            new("endpoint", context.Endpoint ?? "unknown")
        };

        _rateLimitErrorsCounter.Add(1, tags);

        _logger.LogError(error, "Rate limit error for {ClientIp} on {Endpoint}",
            GetHashedIp(context.IpAddress), context.Endpoint);
    }

    public async Task<RateLimitStatistics> GetStatisticsAsync(TimeSpan timeRange, CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation
        // In a real system, you'd query your metrics storage (Prometheus, etc.)
        var cutoff = DateTime.UtcNow - timeRange;
        var relevantEvents = _recentEvents.Where(e => e.Timestamp >= cutoff).ToList();

        var rateLimitEvents = relevantEvents.Where(e => e.Type == SecurityEventType.RateLimitViolation).ToList();

        return new RateLimitStatistics
        {
            TotalRequests = relevantEvents.Count,
            RateLimitedRequests = rateLimitEvents.Count,
            AverageResponseTime = TimeSpan.FromMilliseconds(50), // Placeholder
            ErrorCount = relevantEvents.Count(e => e.Type == SecurityEventType.Error),
            TopViolatingIPs = rateLimitEvents
                .GroupBy(e => e.IpAddress ?? "unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => (g.Key, (long)g.Count()))
                .ToList(),
            TopViolatingClients = rateLimitEvents
                .Where(e => !string.IsNullOrEmpty(e.ClientId))
                .GroupBy(e => e.ClientId!)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => (g.Key, (long)g.Count()))
                .ToList()
        };
    }

    #endregion

    #region Additional Security Metrics

    public void RecordAuthenticationFailure(string? ipAddress, string? username, string reason)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("client_ip", GetHashedIp(ipAddress)),
            new("username", GetHashedUsername(username)),
            new("reason", reason)
        };

        _authenticationFailuresCounter.Add(1, tags);

        var securityEvent = new SecurityEvent
        {
            Type = SecurityEventType.AuthenticationFailure,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            Details = new Dictionary<string, object>
            {
                { "username", GetHashedUsername(username) },
                { "reason", reason }
            }
        };

        _recentEvents.Enqueue(securityEvent);

        _logger.LogWarning("Authentication failure: {Reason} for user {Username} from {ClientIp}",
            reason, GetHashedUsername(username), GetHashedIp(ipAddress));
    }

    public void RecordAuthorizationViolation(string? ipAddress, string? userId, string resource, string action)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("client_ip", GetHashedIp(ipAddress)),
            new("user_id", GetHashedUserId(userId)),
            new("resource", resource),
            new("action", action)
        };

        _authorizationViolationsCounter.Add(1, tags);

        var securityEvent = new SecurityEvent
        {
            Type = SecurityEventType.AuthorizationViolation,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            Details = new Dictionary<string, object>
            {
                { "user_id", GetHashedUserId(userId) },
                { "resource", resource },
                { "action", action }
            }
        };

        _recentEvents.Enqueue(securityEvent);

        _logger.LogWarning("Authorization violation: User {UserId} attempted {Action} on {Resource} from {ClientIp}",
            GetHashedUserId(userId), action, resource, GetHashedIp(ipAddress));
    }

    public void RecordSuspiciousActivity(string? ipAddress, string activityType, Dictionary<string, object> details)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("client_ip", GetHashedIp(ipAddress)),
            new("activity_type", activityType)
        };

        _suspiciousActivitiesCounter.Add(1, tags);

        var securityEvent = new SecurityEvent
        {
            Type = SecurityEventType.SuspiciousActivity,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            Details = new Dictionary<string, object>(details)
            {
                { "activity_type", activityType }
            }
        };

        _recentEvents.Enqueue(securityEvent);

        _logger.LogWarning("Suspicious activity detected: {ActivityType} from {ClientIp}",
            activityType, GetHashedIp(ipAddress));
    }

    public void RecordInputValidationFailure(string? ipAddress, string endpoint, string validationError)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("client_ip", GetHashedIp(ipAddress)),
            new("endpoint", endpoint),
            new("validation_error", validationError)
        };

        _inputValidationFailuresCounter.Add(1, tags);

        _logger.LogInformation("Input validation failure: {ValidationError} on {Endpoint} from {ClientIp}",
            validationError, endpoint, GetHashedIp(ipAddress));
    }

    public void RecordRequestSize(double sizeInBytes, string endpoint)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint)
        };

        _requestSizeHistogram.Record(sizeInBytes, tags);
    }

    public void RecordSecurityHeadersApplied(int headerCount)
    {
        _securityHeadersAppliedCounter.Add(headerCount);
    }

    #endregion

    #region Alert Detection

    public async Task<List<SecurityAlert>> DetectAlertsAsync(TimeSpan timeWindow)
    {
        var alerts = new List<SecurityAlert>();
        var cutoff = DateTime.UtcNow - timeWindow;
        var recentEvents = _recentEvents.Where(e => e.Timestamp >= cutoff).ToList();

        // Detect high rate limit violations from single IP
        var ipViolations = recentEvents
            .Where(e => e.Type == SecurityEventType.RateLimitViolation)
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() >= 10) // 10+ violations in time window
            .ToList();

        foreach (var group in ipViolations)
        {
            alerts.Add(new SecurityAlert
            {
                Type = SecurityAlertType.HighRateLimitViolations,
                Severity = SecurityAlertSeverity.High,
                Message = $"High rate limit violations from IP {GetHashedIp(group.Key)}: {group.Count()} violations",
                IpAddress = group.Key,
                EventCount = group.Count(),
                TimeWindow = timeWindow
            });
        }

        // Detect authentication brute force attempts
        var authFailures = recentEvents
            .Where(e => e.Type == SecurityEventType.AuthenticationFailure)
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() >= 5) // 5+ failures in time window
            .ToList();

        foreach (var group in authFailures)
        {
            alerts.Add(new SecurityAlert
            {
                Type = SecurityAlertType.BruteForceAttempt,
                Severity = SecurityAlertSeverity.Critical,
                Message = $"Potential brute force attack from IP {GetHashedIp(group.Key)}: {group.Count()} failed attempts",
                IpAddress = group.Key,
                EventCount = group.Count(),
                TimeWindow = timeWindow
            });
        }

        return alerts;
    }

    #endregion

    #region Helper Methods

    private string GetHashedIp(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "unknown";

        // Hash IP for privacy while maintaining uniqueness for monitoring
        return $"ip_{ipAddress.GetHashCode():X8}";
    }

    private string GetHashedUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
            return "unknown";

        return $"user_{username.GetHashCode():X8}";
    }

    private string GetHashedUserId(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return "unknown";

        return $"uid_{userId.GetHashCode():X8}";
    }

    private void CleanupOldEvents(object? state)
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromHours(24); // Keep 24 hours of events
        
        while (_recentEvents.TryPeek(out var oldestEvent) && oldestEvent.Timestamp < cutoff)
        {
            _recentEvents.TryDequeue(out _);
        }
    }

    #endregion

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _meter?.Dispose();
    }
}

/// <summary>
/// Represents a security event for monitoring and alerting
/// </summary>
public class SecurityEvent
{
    public SecurityEventType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? ClientId { get; set; }
    public string? Endpoint { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Types of security events
/// </summary>
public enum SecurityEventType
{
    RateLimitViolation,
    AuthenticationFailure,
    AuthorizationViolation,
    SuspiciousActivity,
    InputValidationFailure,
    Error
}

/// <summary>
/// Represents a security alert
/// </summary>
public class SecurityAlert
{
    public SecurityAlertType Type { get; set; }
    public SecurityAlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public int EventCount { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of security alerts
/// </summary>
public enum SecurityAlertType
{
    HighRateLimitViolations,
    BruteForceAttempt,
    SuspiciousPattern,
    SystemError
}

/// <summary>
/// Severity levels for security alerts
/// </summary>
public enum SecurityAlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}
