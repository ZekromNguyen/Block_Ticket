using Identity.Domain.Entities;

namespace Identity.Domain.Services;

/// <summary>
/// Service for sending security event notifications through various channels
/// </summary>
public interface ISecurityNotificationService
{
    Task SendSecurityEventNotificationAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task SendSuspiciousActivityNotificationAsync(SuspiciousActivity suspiciousActivity, CancellationToken cancellationToken = default);
    Task SendAccountLockoutNotificationAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default);
    Task SendCriticalSecurityAlertAsync(string message, string? context = null, CancellationToken cancellationToken = default);
    Task SendSecuritySummaryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for sending notifications to Discord channels
/// </summary>
public interface IDiscordNotificationService
{
    Task SendMessageAsync(string content, CancellationToken cancellationToken = default);
    Task SendMessageAsync(Models.DiscordMessage message, bool isCritical = false, CancellationToken cancellationToken = default);
    Task SendSecurityAlertAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task SendSuspiciousActivityAlertAsync(SuspiciousActivity suspiciousActivity, CancellationToken cancellationToken = default);
    Task SendAccountLockoutAlertAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default);
    Task SendCriticalAlertAsync(string title, string message, string? context = null, CancellationToken cancellationToken = default);
    Task SendDailySummaryAsync(SecuritySummary summary, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for notification channels and thresholds
/// </summary>
public interface INotificationConfiguration
{
    bool IsDiscordEnabled { get; }
    bool IsEmailNotificationEnabled { get; }
    bool IsSmsNotificationEnabled { get; }
    string DefaultDiscordWebhook { get; }
    string CriticalDiscordWebhook { get; }
    List<SecurityEventSeverity> NotificationSeverities { get; }
    List<string> NotifiableEventTypes { get; }
    TimeSpan NotificationThrottleWindow { get; }
    int MaxNotificationsPerWindow { get; }
}

/// <summary>
/// Summary of security events for reporting
/// </summary>
public class SecuritySummary
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int HighSeverityEvents { get; set; }
    public int MediumSeverityEvents { get; set; }
    public int LowSeverityEvents { get; set; }
    public int UnresolvedEvents { get; set; }
    public int AccountLockouts { get; set; }
    public int SuspiciousActivities { get; set; }
    public int FailedLogins { get; set; }
    public int SuccessfulLogins { get; set; }
    public List<string> TopEventTypes { get; set; } = new();
    public List<string> TopSourceIps { get; set; } = new();
    public List<SecurityEventSummaryItem> EventBreakdown { get; set; } = new();
}

/// <summary>
/// Summary item for specific event types
/// </summary>
public class SecurityEventSummaryItem
{
    public string EventType { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public int Count { get; set; }
    public SecurityEventSeverity MaxSeverity { get; set; }
    public int UnresolvedCount { get; set; }
}
