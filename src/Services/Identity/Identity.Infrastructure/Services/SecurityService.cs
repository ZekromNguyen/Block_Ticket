using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Identity.Infrastructure.Services;

public class SecurityService : ISecurityService
{
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly IAccountLockoutRepository _accountLockoutRepository;
    private readonly ISuspiciousActivityRepository _suspiciousActivityRepository;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        ISecurityEventRepository securityEventRepository,
        IAccountLockoutRepository accountLockoutRepository,
        ISuspiciousActivityRepository suspiciousActivityRepository,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<SecurityService> logger)
    {
        _securityEventRepository = securityEventRepository;
        _accountLockoutRepository = accountLockoutRepository;
        _suspiciousActivityRepository = suspiciousActivityRepository;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    // Security Event Management
    public async Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _securityEventRepository.AddAsync(securityEvent, cancellationToken);
            _logger.LogInformation("Security event logged: {EventType} for user {UserId}", 
                securityEvent.EventType, securityEvent.UserId);

            // Check if this event should trigger additional security measures
            await CheckForSecurityThresholdsAsync(securityEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event: {EventType}", securityEvent.EventType);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        return await _securityEventRepository.GetEventsAsync(userId, from, to, cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetUnresolvedSecurityEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _securityEventRepository.GetUnresolvedEventsAsync(cancellationToken);
    }

    public async Task ResolveSecurityEventAsync(Guid eventId, string resolvedBy, string resolution, CancellationToken cancellationToken = default)
    {
        var securityEvent = await _securityEventRepository.GetByIdAsync(eventId, cancellationToken);
        if (securityEvent != null)
        {
            securityEvent.Resolve(resolvedBy, resolution);
            await _securityEventRepository.UpdateAsync(securityEvent, cancellationToken);
        }
    }

    // Account Lockout Management
    public async Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var lockout = await _accountLockoutRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (lockout == null) return false;

        var lockoutDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Security:LockoutDurationMinutes", 15));
        if (lockout.IsExpired(lockoutDuration))
        {
            lockout.Unlock("System - Expired");
            await _accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
            return false;
        }

        return true;
    }

    public async Task LockAccountAsync(Guid userId, string reason, int failedAttempts, string ipAddress, CancellationToken cancellationToken = default)
    {
        var lockout = new AccountLockout(userId, reason, failedAttempts, ipAddress);
        await _accountLockoutRepository.AddAsync(lockout, cancellationToken);

        // Log security event
        var securityEvent = SecurityEvent.CreateAccountLockout(userId, ipAddress, reason);
        await LogSecurityEventAsync(securityEvent, cancellationToken);

        _logger.LogWarning("Account locked: User {UserId}, Reason: {Reason}", userId, reason);
    }

    public async Task UnlockAccountAsync(Guid userId, string unlockedBy, CancellationToken cancellationToken = default)
    {
        var lockout = await _accountLockoutRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (lockout != null)
        {
            lockout.Unlock(unlockedBy);
            await _accountLockoutRepository.UpdateAsync(lockout, cancellationToken);

            _logger.LogInformation("Account unlocked: User {UserId} by {UnlockedBy}", userId, unlockedBy);
        }
    }

    public async Task<AccountLockout?> GetAccountLockoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _accountLockoutRepository.GetActiveByUserIdAsync(userId, cancellationToken);
    }

    public async Task CleanupExpiredLockoutsAsync(CancellationToken cancellationToken = default)
    {
        var lockoutDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Security:LockoutDurationMinutes", 15));
        await _accountLockoutRepository.CleanupExpiredAsync(lockoutDuration, cancellationToken);
    }

    // Suspicious Activity Detection
    public async Task<double> CalculateRiskScoreAsync(Guid? userId, string ipAddress, string? userAgent, string activityType, CancellationToken cancellationToken = default)
    {
        double riskScore = 0.0;

        // Check IP reputation
        if (await IsIpAddressSuspiciousAsync(ipAddress, cancellationToken))
        {
            riskScore += 30.0;
        }

        // Check for unusual location
        if (userId.HasValue)
        {
            var location = await GetLocationFromIpAsync(ipAddress, cancellationToken);
            if (location != null && await IsLocationUnusualForUserAsync(userId.Value, location, cancellationToken))
            {
                riskScore += 25.0;
            }
        }

        // Check user agent
        if (!string.IsNullOrEmpty(userAgent) && await IsUserAgentSuspiciousAsync(userAgent, cancellationToken))
        {
            riskScore += 15.0;
        }

        // Check recent failed attempts
        if (userId.HasValue)
        {
            var recentFailures = await GetRecentFailedAttemptsAsync(userId.Value, TimeSpan.FromHours(1), cancellationToken);
            if (recentFailures > 3)
            {
                riskScore += Math.Min(recentFailures * 5.0, 20.0);
            }
        }

        // Check time of day (higher risk for unusual hours)
        var hour = DateTime.UtcNow.Hour;
        if (hour < 6 || hour > 22) // Outside normal business hours
        {
            riskScore += 10.0;
        }

        return Math.Min(riskScore, 100.0);
    }

    public async Task LogSuspiciousActivityAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default)
    {
        await _suspiciousActivityRepository.AddAsync(activity, cancellationToken);
        
        // Log as security event as well
        var securityEvent = SecurityEvent.CreateSuspiciousActivity(
            activity.UserId, 
            activity.ActivityType, 
            activity.Description, 
            activity.IpAddress, 
            activity.UserAgent);
        
        await LogSecurityEventAsync(securityEvent, cancellationToken);

        _logger.LogWarning("Suspicious activity detected: {ActivityType} for user {UserId} from {IpAddress}", 
            activity.ActivityType, activity.UserId, activity.IpAddress);
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetSuspiciousActivitiesAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        return await _suspiciousActivityRepository.GetActivitiesAsync(userId, from, to, cancellationToken);
    }

    public async Task ResolveSuspiciousActivityAsync(Guid activityId, string resolution, string resolvedBy, CancellationToken cancellationToken = default)
    {
        var activity = await _suspiciousActivityRepository.GetByIdAsync(activityId, cancellationToken);
        if (activity != null)
        {
            activity.Resolve(resolution, resolvedBy);
            await _suspiciousActivityRepository.UpdateAsync(activity, cancellationToken);
        }
    }

    // Rate Limiting and Throttling
    public async Task<bool> IsRateLimitExceededAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var count = await GetRateLimitCountAsync(key, cancellationToken);
        return count >= limit;
    }

    public async Task IncrementRateLimitCounterAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rate_limit:{key}";
        var countStr = await _cache.GetStringAsync(cacheKey, cancellationToken);
        var count = int.TryParse(countStr, out var currentCount) ? currentCount : 0;
        
        count++;
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = window
        };
        
        await _cache.SetStringAsync(cacheKey, count.ToString(), options, cancellationToken);
    }

    public async Task<int> GetRateLimitCountAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rate_limit:{key}";
        var countStr = await _cache.GetStringAsync(cacheKey, cancellationToken);
        return int.TryParse(countStr, out var count) ? count : 0;
    }

    // IP and Location Analysis
    public async Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"ip_reputation:{ipAddress}";
        var cachedResult = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return bool.Parse(cachedResult);
        }

        // Check against threat intelligence
        var isSuspicious = await IsIpAddressInThreatListAsync(ipAddress, cancellationToken);
        
        // Cache result for 1 hour
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        await _cache.SetStringAsync(cacheKey, isSuspicious.ToString(), options, cancellationToken);
        
        return isSuspicious;
    }

    public async Task<string?> GetLocationFromIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        // This would integrate with a geolocation service like MaxMind or IPStack
        // For now, return a placeholder
        await Task.Delay(1, cancellationToken); // Simulate async call
        return "Unknown"; // TODO: Implement actual geolocation
    }

    public async Task<bool> IsLocationUnusualForUserAsync(Guid userId, string location, CancellationToken cancellationToken = default)
    {
        // Check user's historical locations
        var recentEvents = await _securityEventRepository.GetRecentLocationEventsAsync(userId, TimeSpan.FromDays(30), cancellationToken);
        var knownLocations = recentEvents.Select(e => e.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct();
        
        return !knownLocations.Contains(location);
    }

    public async Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        var blockDuration = duration ?? TimeSpan.FromHours(24);
        var cacheKey = $"blocked_ip:{ipAddress}";
        
        var blockInfo = new
        {
            Reason = reason,
            BlockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(blockDuration)
        };
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = blockDuration
        };
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(blockInfo), options, cancellationToken);
        
        _logger.LogWarning("IP address blocked: {IpAddress}, Reason: {Reason}", ipAddress, reason);
    }

    // Device Fingerprinting
    public Task<string> GenerateDeviceFingerprintAsync(string userAgent, string? additionalData = null)
    {
        var data = $"{userAgent}|{additionalData ?? ""}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Task.FromResult(Convert.ToBase64String(hash));
    }

    public async Task<bool> IsDeviceKnownAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        // Check recent security events for this device fingerprint
        var recentEvents = await _securityEventRepository.GetRecentDeviceEventsAsync(userId, deviceFingerprint, TimeSpan.FromDays(30), cancellationToken);
        return recentEvents.Any();
    }

    public async Task RegisterDeviceAsync(Guid userId, string deviceFingerprint, string? deviceName = null, CancellationToken cancellationToken = default)
    {
        // This would typically store device information in a dedicated table
        // For now, we'll log it as a security event
        var securityEvent = new SecurityEvent(
            userId,
            "DEVICE_REGISTERED",
            SecurityEventCategories.Security,
            SecurityEventSeverity.Low,
            $"New device registered: {deviceName ?? "Unknown"}",
            "Unknown", // IP would be provided by caller
            deviceFingerprint: deviceFingerprint);
        
        await LogSecurityEventAsync(securityEvent, cancellationToken);
    }

    // Threat Intelligence
    public async Task<bool> IsIpAddressInThreatListAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        // This would integrate with threat intelligence feeds
        // For now, return false as placeholder
        await Task.Delay(1, cancellationToken);
        return false; // TODO: Implement actual threat intelligence integration
    }

    public async Task<bool> IsUserAgentSuspiciousAsync(string userAgent, CancellationToken cancellationToken = default)
    {
        // Check for suspicious patterns in user agent
        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python", "java"
        };
        
        await Task.Delay(1, cancellationToken);
        return suspiciousPatterns.Any(pattern => userAgent.ToLower().Contains(pattern));
    }

    public async Task UpdateThreatIntelligenceAsync(CancellationToken cancellationToken = default)
    {
        // This would update threat intelligence data from external sources
        await Task.Delay(1, cancellationToken);
        _logger.LogInformation("Threat intelligence updated");
    }

    // Private helper methods
    private async Task CheckForSecurityThresholdsAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        if (securityEvent.EventType == SecurityEventTypes.LoginFailure && securityEvent.UserId.HasValue)
        {
            var maxAttempts = _configuration.GetValue<int>("Security:MaxLoginAttempts", 5);
            var recentFailures = await GetRecentFailedAttemptsAsync(securityEvent.UserId.Value, TimeSpan.FromMinutes(15), cancellationToken);
            
            if (recentFailures >= maxAttempts)
            {
                await LockAccountAsync(securityEvent.UserId.Value, "Too many failed login attempts", recentFailures, securityEvent.IpAddress, cancellationToken);
            }
        }
    }

    private async Task<int> GetRecentFailedAttemptsAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Subtract(timeWindow);
        var events = await _securityEventRepository.GetEventsAsync(userId, from, null, cancellationToken);
        return events.Count(e => e.EventType == SecurityEventTypes.LoginFailure);
    }
}
