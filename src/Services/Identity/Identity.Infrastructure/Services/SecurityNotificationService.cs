using Identity.Domain.Entities;
using Identity.Domain.Models;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Service for managing security event notifications across multiple channels
/// </summary>
public class SecurityNotificationService : ISecurityNotificationService
{
    private readonly IDiscordNotificationService _discordService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IUserRepository _userRepository;
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly ISuspiciousActivityRepository _suspiciousActivityRepository;
    private readonly IAccountLockoutRepository _accountLockoutRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SecurityNotificationService> _logger;
    private readonly SecurityNotificationConfig _config;

    public SecurityNotificationService(
        IDiscordNotificationService discordService,
        IEmailService emailService,
        ISmsService smsService,
        IUserRepository userRepository,
        ISecurityEventRepository securityEventRepository,
        ISuspiciousActivityRepository suspiciousActivityRepository,
        IAccountLockoutRepository accountLockoutRepository,
        IDistributedCache cache,
        ILogger<SecurityNotificationService> logger,
        IConfiguration configuration)
    {
        _discordService = discordService;
        _emailService = emailService;
        _smsService = smsService;
        _userRepository = userRepository;
        _securityEventRepository = securityEventRepository;
        _suspiciousActivityRepository = suspiciousActivityRepository;
        _accountLockoutRepository = accountLockoutRepository;
        _cache = cache;
        _logger = logger;
        _config = LoadConfiguration(configuration);
    }

    public async Task SendSecurityEventNotificationAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                _logger.LogDebug("Security notifications are disabled");
                return;
            }

            // Check if we should notify for this event
            if (!ShouldNotify(securityEvent))
            {
                _logger.LogDebug("Notification not required for event type {EventType} with severity {Severity}", 
                    securityEvent.EventType, securityEvent.Severity);
                return;
            }

            // Check throttling
            if (await IsThrottledAsync(securityEvent.EventType, cancellationToken))
            {
                _logger.LogDebug("Notification throttled for event type {EventType}", securityEvent.EventType);
                return;
            }

            // Get applicable channels
            var channels = GetApplicableChannels(securityEvent.EventType, securityEvent.Severity);

            var tasks = new List<Task>();

            foreach (var channel in channels)
            {
                tasks.Add(channel.Type.ToLower() switch
                {
                    "discord" => _discordService.SendSecurityAlertAsync(securityEvent, cancellationToken),
                    "email" => SendEmailNotificationAsync(securityEvent, channel.Target, cancellationToken),
                    "sms" => SendSmsNotificationAsync(securityEvent, channel.Target, cancellationToken),
                    _ => Task.CompletedTask
                });
            }

            await Task.WhenAll(tasks);

            // Update throttling cache
            await UpdateThrottlingCacheAsync(securityEvent.EventType, cancellationToken);

            _logger.LogInformation("Security event notification sent for {EventType} to {ChannelCount} channels", 
                securityEvent.EventType, channels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security event notification for {EventType}", securityEvent.EventType);
        }
    }

    public async Task SendSuspiciousActivityNotificationAsync(SuspiciousActivity suspiciousActivity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                return;
            }

            // Always notify for suspicious activities with high risk scores
            if (suspiciousActivity.RiskScore < 60)
            {
                _logger.LogDebug("Suspicious activity risk score {RiskScore} below notification threshold", 
                    suspiciousActivity.RiskScore);
                return;
            }

            // Check throttling
            if (await IsThrottledAsync($"suspicious_activity_{suspiciousActivity.ActivityType}", cancellationToken))
            {
                return;
            }

            var channels = GetApplicableChannels("SUSPICIOUS_ACTIVITY", SecurityEventSeverity.High);

            var tasks = new List<Task>();

            foreach (var channel in channels)
            {
                tasks.Add(channel.Type.ToLower() switch
                {
                    "discord" => _discordService.SendSuspiciousActivityAlertAsync(suspiciousActivity, cancellationToken),
                    "email" => SendEmailSuspiciousActivityAsync(suspiciousActivity, channel.Target, cancellationToken),
                    "sms" => SendSmsSuspiciousActivityAsync(suspiciousActivity, channel.Target, cancellationToken),
                    _ => Task.CompletedTask
                });
            }

            await Task.WhenAll(tasks);

            await UpdateThrottlingCacheAsync($"suspicious_activity_{suspiciousActivity.ActivityType}", cancellationToken);

            _logger.LogInformation("Suspicious activity notification sent for {ActivityType} with risk score {RiskScore}", 
                suspiciousActivity.ActivityType, suspiciousActivity.RiskScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending suspicious activity notification for {ActivityType}", 
                suspiciousActivity.ActivityType);
        }
    }

    public async Task SendAccountLockoutNotificationAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                return;
            }

            var channels = GetApplicableChannels("ACCOUNT_LOCKED", SecurityEventSeverity.High);

            var tasks = new List<Task>();

            foreach (var channel in channels)
            {
                tasks.Add(channel.Type.ToLower() switch
                {
                    "discord" => _discordService.SendAccountLockoutAlertAsync(accountLockout, cancellationToken),
                    "email" => SendEmailAccountLockoutAsync(accountLockout, channel.Target, cancellationToken),
                    "sms" => SendSmsAccountLockoutAsync(accountLockout, channel.Target, cancellationToken),
                    _ => Task.CompletedTask
                });
            }

            // Also notify the affected user if we have their email
            try
            {
                var user = await _userRepository.GetByIdAsync(accountLockout.UserId, cancellationToken);
                if (user != null)
                {
                    var userNotificationMessage = $"Your BlockTicket account has been temporarily locked due to {accountLockout.Reason.ToLower()}. " +
                        $"Please contact support if you believe this was done in error.";
                    await _emailService.SendSecurityAlertAsync(user.Email.Value, userNotificationMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send account lockout notification to user {UserId}", accountLockout.UserId);
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("Account lockout notification sent for user {UserId}", accountLockout.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending account lockout notification for user {UserId}", accountLockout.UserId);
        }
    }

    public async Task SendCriticalSecurityAlertAsync(string message, string? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                return;
            }

            var channels = GetApplicableChannels("CRITICAL_ALERT", SecurityEventSeverity.Critical);

            var tasks = new List<Task>();

            foreach (var channel in channels)
            {
                tasks.Add(channel.Type.ToLower() switch
                {
                    "discord" => _discordService.SendCriticalAlertAsync("Security Alert", message, context, cancellationToken),
                    "email" => SendEmailCriticalAlertAsync(message, context, channel.Target, cancellationToken),
                    "sms" => SendSmsCriticalAlertAsync(message, channel.Target, cancellationToken),
                    _ => Task.CompletedTask
                });
            }

            await Task.WhenAll(tasks);

            _logger.LogWarning("Critical security alert sent: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending critical security alert: {Message}", message);
        }
    }

    public async Task SendSecuritySummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.Enabled)
            {
                return;
            }

            var summary = await GenerateSecuritySummaryAsync(from, to, cancellationToken);

            var channels = _config.Channels.Where(c => c.Enabled && c.Type.ToLower() == "discord").ToList();

            var tasks = channels.Select(channel => 
                _discordService.SendDailySummaryAsync(summary, cancellationToken)).ToList();

            await Task.WhenAll(tasks);

            _logger.LogInformation("Security summary sent for period {From} to {To}", from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security summary for period {From} to {To}", from, to);
        }
    }

    private async Task<SecuritySummary> GenerateSecuritySummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var events = await _securityEventRepository.GetEventsAsync(null, from, to, cancellationToken);
        var suspiciousActivities = await _suspiciousActivityRepository.GetActivitiesAsync(null, from, to, cancellationToken);
        var accountLockouts = await _accountLockoutRepository.GetLockoutsAsync(null, from, to, cancellationToken);

        var eventsList = events.ToList();
        var suspiciousActivitiesList = suspiciousActivities.ToList();
        var accountLockoutsList = accountLockouts.ToList();

        var summary = new SecuritySummary
        {
            FromDate = from,
            ToDate = to,
            TotalEvents = eventsList.Count,
            CriticalEvents = eventsList.Count(e => e.Severity == SecurityEventSeverity.Critical),
            HighSeverityEvents = eventsList.Count(e => e.Severity == SecurityEventSeverity.High),
            MediumSeverityEvents = eventsList.Count(e => e.Severity == SecurityEventSeverity.Medium),
            LowSeverityEvents = eventsList.Count(e => e.Severity == SecurityEventSeverity.Low),
            UnresolvedEvents = eventsList.Count(e => !e.IsResolved),
            AccountLockouts = accountLockoutsList.Count,
            SuspiciousActivities = suspiciousActivitiesList.Count,
            FailedLogins = eventsList.Count(e => e.EventType == SecurityEventTypes.LoginFailure),
            SuccessfulLogins = eventsList.Count(e => e.EventType == SecurityEventTypes.LoginSuccess),
            TopEventTypes = eventsList.GroupBy(e => e.EventType)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList(),
            TopSourceIps = eventsList.GroupBy(e => e.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList(),
            EventBreakdown = eventsList.GroupBy(e => new { e.EventType, e.EventCategory })
                .Select(g => new SecurityEventSummaryItem
                {
                    EventType = g.Key.EventType,
                    EventCategory = g.Key.EventCategory,
                    Count = g.Count(),
                    MaxSeverity = g.Max(e => e.Severity),
                    UnresolvedCount = g.Count(e => !e.IsResolved)
                })
                .OrderByDescending(s => s.Count)
                .ToList()
        };

        return summary;
    }

    private bool ShouldNotify(SecurityEvent securityEvent)
    {
        // Check if this event type should trigger notifications
        var applicableRules = _config.Rules.Where(r => r.Enabled &&
            (r.EventTypes.Contains(securityEvent.EventType) || r.EventTypes.Contains("*")) &&
            r.Severities.Contains(securityEvent.Severity)).ToList();

        return applicableRules.Any();
    }

    private List<NotificationChannel> GetApplicableChannels(string eventType, SecurityEventSeverity severity)
    {
        return _config.Channels.Where(c => c.Enabled &&
            (c.EventTypes.Contains(eventType) || c.EventTypes.Contains("*")) &&
            c.Severities.Contains(severity)).ToList();
    }

    private async Task<bool> IsThrottledAsync(string eventType, CancellationToken cancellationToken)
    {
        if (!_config.Throttling.Enabled)
        {
            return false;
        }

        var cacheKey = $"notification_throttle_{eventType}";
        var countStr = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (int.TryParse(countStr, out var count))
        {
            var limit = _config.Throttling.PerEventTypeLimits.GetValueOrDefault(eventType, _config.Throttling.MaxNotifications);
            return count >= limit;
        }

        return false;
    }

    private async Task UpdateThrottlingCacheAsync(string eventType, CancellationToken cancellationToken)
    {
        if (!_config.Throttling.Enabled)
        {
            return;
        }

        var cacheKey = $"notification_throttle_{eventType}";
        var countStr = await _cache.GetStringAsync(cacheKey, cancellationToken);
        var count = int.TryParse(countStr, out var currentCount) ? currentCount + 1 : 1;

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _config.Throttling.Window
        };

        await _cache.SetStringAsync(cacheKey, count.ToString(), cacheOptions, cancellationToken);
    }

    private async Task SendEmailNotificationAsync(SecurityEvent securityEvent, string email, CancellationToken cancellationToken)
    {
        var message = $"Security Event Alert: {securityEvent.EventType}\n\n" +
                     $"Description: {securityEvent.Description}\n" +
                     $"Severity: {securityEvent.Severity}\n" +
                     $"IP Address: {securityEvent.IpAddress}\n" +
                     $"Timestamp: {securityEvent.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}\n";

        await _emailService.SendSecurityAlertAsync(email, message);
    }

    private async Task SendSmsNotificationAsync(SecurityEvent securityEvent, string phoneNumber, CancellationToken cancellationToken)
    {
        var message = $"BlockTicket Security Alert: {securityEvent.EventType} - {securityEvent.Severity} severity. Check security dashboard for details.";
        await _smsService.SendSecurityAlertAsync(phoneNumber, message);
    }

    private async Task SendEmailSuspiciousActivityAsync(SuspiciousActivity activity, string email, CancellationToken cancellationToken)
    {
        var message = $"Suspicious Activity Alert: {activity.ActivityType}\n\n" +
                     $"Description: {activity.Description}\n" +
                     $"Risk Score: {activity.RiskScore:F1}\n" +
                     $"IP Address: {activity.IpAddress}\n" +
                     $"Timestamp: {activity.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}\n";

        await _emailService.SendSecurityAlertAsync(email, message);
    }

    private async Task SendSmsSuspiciousActivityAsync(SuspiciousActivity activity, string phoneNumber, CancellationToken cancellationToken)
    {
        var message = $"BlockTicket Suspicious Activity: {activity.ActivityType} (Risk: {activity.RiskScore:F1}). Review security dashboard.";
        await _smsService.SendSecurityAlertAsync(phoneNumber, message);
    }

    private async Task SendEmailAccountLockoutAsync(AccountLockout lockout, string email, CancellationToken cancellationToken)
    {
        var message = $"Account Lockout Alert\n\n" +
                     $"User ID: {lockout.UserId}\n" +
                     $"Reason: {lockout.Reason}\n" +
                     $"Failed Attempts: {lockout.FailedAttempts}\n" +
                     $"IP Address: {lockout.IpAddress}\n" +
                     $"Locked At: {lockout.LockedAt:yyyy-MM-dd HH:mm:ss UTC}\n";

        await _emailService.SendSecurityAlertAsync(email, message);
    }

    private async Task SendSmsAccountLockoutAsync(AccountLockout lockout, string phoneNumber, CancellationToken cancellationToken)
    {
        var message = $"BlockTicket Account Lockout: User {lockout.UserId} locked due to {lockout.Reason}. Review required.";
        await _smsService.SendSecurityAlertAsync(phoneNumber, message);
    }

    private async Task SendEmailCriticalAlertAsync(string message, string? context, string email, CancellationToken cancellationToken)
    {
        var fullMessage = $"CRITICAL SECURITY ALERT\n\n{message}";
        if (!string.IsNullOrEmpty(context))
        {
            fullMessage += $"\n\nContext: {context}";
        }
        fullMessage += $"\n\nTimestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}";

        await _emailService.SendSecurityAlertAsync(email, fullMessage);
    }

    private async Task SendSmsCriticalAlertAsync(string message, string phoneNumber, CancellationToken cancellationToken)
    {
        var smsMessage = $"CRITICAL: BlockTicket Security Alert - {message}. Immediate attention required.";
        await _smsService.SendSecurityAlertAsync(phoneNumber, smsMessage);
    }

    private static SecurityNotificationConfig LoadConfiguration(IConfiguration configuration)
    {
        var config = new SecurityNotificationConfig();
        configuration.GetSection("Notifications:SecurityEvents").Bind(config);

        // Set default configuration if not specified
        if (!config.Channels.Any())
        {
            config.Channels = new List<NotificationChannel>
            {
                new()
                {
                    Name = "default-discord",
                    Type = "discord",
                    Target = "default-webhook",
                    Severities = new List<SecurityEventSeverity> { SecurityEventSeverity.Medium, SecurityEventSeverity.High, SecurityEventSeverity.Critical },
                    EventTypes = new List<string> { "*" },
                    Enabled = true
                },
                new()
                {
                    Name = "critical-discord",
                    Type = "discord",
                    Target = "critical-webhook",
                    Severities = new List<SecurityEventSeverity> { SecurityEventSeverity.Critical },
                    EventTypes = new List<string> { "*" },
                    Enabled = true
                }
            };
        }

        if (!config.Rules.Any())
        {
            config.Rules = new List<NotificationRule>
            {
                new()
                {
                    Name = "critical-events",
                    EventTypes = new List<string> { "*" },
                    Severities = new List<SecurityEventSeverity> { SecurityEventSeverity.Critical },
                    Channels = new List<string> { "critical-discord" },
                    Enabled = true
                },
                new()
                {
                    Name = "high-medium-events",
                    EventTypes = new List<string> { "*" },
                    Severities = new List<SecurityEventSeverity> { SecurityEventSeverity.High, SecurityEventSeverity.Medium },
                    Channels = new List<string> { "default-discord" },
                    Enabled = true
                }
            };
        }

        return config;
    }
}
