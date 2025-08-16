using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Background service for continuous security monitoring and threat detection
/// </summary>
public class SecurityMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly TimeSpan _monitoringInterval;

    public SecurityMonitoringService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SecurityMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _monitoringInterval = TimeSpan.FromMinutes(configuration.GetValue<int>("Security:MonitoringIntervalMinutes", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSecurityMonitoringAsync(stoppingToken);
                await Task.Delay(_monitoringInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in security monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Security monitoring service stopped");
    }

    private async Task PerformSecurityMonitoringAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();
        var securityEventRepository = scope.ServiceProvider.GetRequiredService<ISecurityEventRepository>();
        var suspiciousActivityRepository = scope.ServiceProvider.GetRequiredService<ISuspiciousActivityRepository>();

        _logger.LogDebug("Starting security monitoring cycle");

        // Monitor for suspicious patterns
        await DetectSuspiciousPatternsAsync(securityEventRepository, securityService, cancellationToken);

        // Monitor for brute force attacks
        await DetectBruteForceAttacksAsync(securityEventRepository, securityService, cancellationToken);

        // Monitor for account enumeration attempts
        await DetectAccountEnumerationAsync(securityEventRepository, securityService, cancellationToken);

        // Monitor for unusual login patterns
        await DetectUnusualLoginPatternsAsync(securityEventRepository, securityService, cancellationToken);

        // Clean up old security events and activities
        await CleanupOldDataAsync(securityEventRepository, suspiciousActivityRepository, securityService, cancellationToken);

        // Update threat intelligence
        await UpdateThreatIntelligenceAsync(securityService, cancellationToken);

        _logger.LogDebug("Security monitoring cycle completed");
    }

    private async Task DetectSuspiciousPatternsAsync(ISecurityEventRepository securityEventRepository, ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            var timeWindow = TimeSpan.FromMinutes(15);
            var from = DateTime.UtcNow.Subtract(timeWindow);

            // Get recent security events
            var recentEvents = await securityEventRepository.GetEventsAsync(null, from, null, cancellationToken);

            // Group by IP address to detect patterns
            var eventsByIp = recentEvents.GroupBy(e => e.IpAddress);

            foreach (var ipGroup in eventsByIp)
            {
                var ipAddress = ipGroup.Key;
                var events = ipGroup.ToList();

                // Check for multiple failed logins from same IP
                var failedLogins = events.Count(e => e.EventType == SecurityEventTypes.LoginFailure);
                if (failedLogins >= 5)
                {
                    await LogSuspiciousActivityAsync(securityService, null, ipAddress, 
                        "MULTIPLE_FAILED_LOGINS", 
                        $"Multiple failed login attempts ({failedLogins}) from IP {ipAddress}",
                        70.0, cancellationToken);
                }

                // Check for rapid successive events
                if (events.Count >= 10)
                {
                    var timeSpan = events.Max(e => e.CreatedAt) - events.Min(e => e.CreatedAt);
                    if (timeSpan.TotalMinutes < 5)
                    {
                        await LogSuspiciousActivityAsync(securityService, null, ipAddress,
                            "HIGH_VELOCITY_REQUESTS",
                            $"High velocity requests ({events.Count} events in {timeSpan.TotalMinutes:F1} minutes)",
                            60.0, cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious patterns");
        }
    }

    private async Task DetectBruteForceAttacksAsync(ISecurityEventRepository securityEventRepository, ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            var timeWindow = TimeSpan.FromHours(1);
            var from = DateTime.UtcNow.Subtract(timeWindow);

            // Get failed login events
            var failedLogins = await securityEventRepository.GetEventsByTypeAsync(SecurityEventTypes.LoginFailure, from, null, cancellationToken);

            // Group by user to detect targeted attacks
            var failedLoginsByUser = failedLogins
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value);

            foreach (var userGroup in failedLoginsByUser)
            {
                var userId = userGroup.Key;
                var attempts = userGroup.ToList();

                if (attempts.Count >= 10) // Threshold for brute force
                {
                    var uniqueIps = attempts.Select(a => a.IpAddress).Distinct().Count();
                    var description = uniqueIps > 1 
                        ? $"Distributed brute force attack: {attempts.Count} failed attempts from {uniqueIps} IPs"
                        : $"Brute force attack: {attempts.Count} failed attempts from single IP";

                    await LogSuspiciousActivityAsync(securityService, userId, attempts.First().IpAddress,
                        "BRUTE_FORCE_ATTACK", description, 90.0, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting brute force attacks");
        }
    }

    private async Task DetectAccountEnumerationAsync(ISecurityEventRepository securityEventRepository, ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            var timeWindow = TimeSpan.FromMinutes(30);
            var from = DateTime.UtcNow.Subtract(timeWindow);

            // Get failed login events
            var failedLogins = await securityEventRepository.GetEventsByTypeAsync(SecurityEventTypes.LoginFailure, from, null, cancellationToken);

            // Group by IP to detect enumeration attempts
            var failedLoginsByIp = failedLogins.GroupBy(e => e.IpAddress);

            foreach (var ipGroup in failedLoginsByIp)
            {
                var ipAddress = ipGroup.Key;
                var attempts = ipGroup.ToList();

                // Check for attempts against many different users
                var uniqueUsers = attempts.Where(a => a.UserId.HasValue).Select(a => a.UserId).Distinct().Count();
                
                if (uniqueUsers >= 5 && attempts.Count >= 10)
                {
                    await LogSuspiciousActivityAsync(securityService, null, ipAddress,
                        "ACCOUNT_ENUMERATION",
                        $"Potential account enumeration: {attempts.Count} attempts against {uniqueUsers} different accounts",
                        75.0, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting account enumeration");
        }
    }

    private async Task DetectUnusualLoginPatternsAsync(ISecurityEventRepository securityEventRepository, ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            var timeWindow = TimeSpan.FromHours(24);
            var from = DateTime.UtcNow.Subtract(timeWindow);

            // Get successful login events
            var successfulLogins = await securityEventRepository.GetEventsByTypeAsync(SecurityEventTypes.LoginSuccess, from, null, cancellationToken);

            // Group by user to analyze patterns
            var loginsByUser = successfulLogins
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value);

            foreach (var userGroup in loginsByUser)
            {
                var userId = userGroup.Key;
                var logins = userGroup.ToList();

                // Check for logins from multiple locations
                var uniqueLocations = logins.Where(l => !string.IsNullOrEmpty(l.Location))
                    .Select(l => l.Location).Distinct().Count();

                if (uniqueLocations >= 3)
                {
                    await LogSuspiciousActivityAsync(securityService, userId, logins.First().IpAddress,
                        "MULTIPLE_LOCATIONS",
                        $"User logged in from {uniqueLocations} different locations in 24 hours",
                        50.0, cancellationToken);
                }

                // Check for unusual time patterns
                var nightLogins = logins.Count(l => l.CreatedAt.Hour < 6 || l.CreatedAt.Hour > 22);
                if (nightLogins >= 3)
                {
                    await LogSuspiciousActivityAsync(securityService, userId, logins.First().IpAddress,
                        "UNUSUAL_TIME_PATTERN",
                        $"Multiple logins during unusual hours ({nightLogins} night logins)",
                        40.0, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual login patterns");
        }
    }

    private async Task CleanupOldDataAsync(ISecurityEventRepository securityEventRepository, ISuspiciousActivityRepository suspiciousActivityRepository, ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            var retentionPeriod = TimeSpan.FromDays(_configuration.GetValue<int>("Security:DataRetentionDays", 90));

            // Clean up old security events
            await securityEventRepository.CleanupOldEventsAsync(retentionPeriod, cancellationToken);

            // Clean up old suspicious activities
            await suspiciousActivityRepository.CleanupOldActivitiesAsync(retentionPeriod, cancellationToken);

            // Clean up expired account lockouts
            await securityService.CleanupExpiredLockoutsAsync(cancellationToken);

            _logger.LogDebug("Security data cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during security data cleanup");
        }
    }

    private async Task UpdateThreatIntelligenceAsync(ISecurityService securityService, CancellationToken cancellationToken)
    {
        try
        {
            await securityService.UpdateThreatIntelligenceAsync(cancellationToken);
            _logger.LogDebug("Threat intelligence updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating threat intelligence");
        }
    }

    private async Task LogSuspiciousActivityAsync(ISecurityService securityService, Guid? userId, string ipAddress, string activityType, string description, double riskScore, CancellationToken cancellationToken)
    {
        try
        {
            var activity = new SuspiciousActivity(userId, activityType, description, ipAddress, riskScore);
            await securityService.LogSuspiciousActivityAsync(activity, cancellationToken);

            _logger.LogWarning("Suspicious activity detected: {ActivityType} - {Description}", activityType, description);

            // Send notification for high-risk activities
            if (riskScore >= 60)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var notificationService = scope.ServiceProvider.GetService<ISecurityNotificationService>();
                        if (notificationService != null)
                        {
                            await notificationService.SendSuspiciousActivityNotificationAsync(activity, cancellationToken);
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, "Error sending notification for suspicious activity {ActivityType}", activityType);
                    }
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging suspicious activity");
        }
    }
}
