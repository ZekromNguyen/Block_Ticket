using System.Text.Json;
using Identity.Domain.Entities;
using Identity.Domain.Models;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Service for sending notifications to Discord webhooks
/// </summary>
public class DiscordNotificationService : IDiscordNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _defaultWebhookUrl;
    private readonly string? _criticalWebhookUrl;

    public DiscordNotificationService(
        HttpClient httpClient,
        ILogger<DiscordNotificationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        _defaultWebhookUrl = configuration["Notifications:Discord:DefaultWebhookUrl"];
        _criticalWebhookUrl = configuration["Notifications:Discord:CriticalWebhookUrl"];
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task SendMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        var payload = new DiscordMessage
        {
            Content = content,
            Username = "BlockTicket Security Bot",
            AvatarUrl = "https://i.imgur.com/4M34hi2.png" // Generic security shield icon
        };

        await SendMessageAsync(payload, false, cancellationToken);
    }

    public async Task SendMessageAsync(DiscordMessage message, bool isCritical = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookUrl = isCritical ? _criticalWebhookUrl : _defaultWebhookUrl;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("No Discord webhook URL configured for {Type} notifications", 
                    isCritical ? "critical" : "default");
                return;
            }

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent Discord notification ({Type})", 
                    isCritical ? "critical" : "default");
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send Discord notification ({Type}). Status: {StatusCode}, Response: {Response}",
                    isCritical ? "critical" : "default", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Discord notification ({Type})", 
                isCritical ? "critical" : "default");
        }
    }

    public async Task SendSecurityAlertAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        var color = GetColorForSeverity(securityEvent.Severity);
        var isCritical = securityEvent.Severity == SecurityEventSeverity.Critical;

        var embed = new DiscordEmbed
        {
            Title = $"🚨 Security Event: {securityEvent.EventType}",
            Description = securityEvent.Description,
            Color = color,
            Fields = new List<DiscordEmbedField>
            {
                new() { Name = "Event Type", Value = securityEvent.EventType, Inline = true },
                new() { Name = "Category", Value = securityEvent.EventCategory, Inline = true },
                new() { Name = "Severity", Value = securityEvent.Severity.ToString(), Inline = true },
                new() { Name = "IP Address", Value = securityEvent.IpAddress, Inline = true },
                new() { Name = "User ID", Value = securityEvent.UserId?.ToString() ?? "N/A", Inline = true },
                new() { Name = "Timestamp", Value = securityEvent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true }
            },
            Footer = new DiscordEmbedFooter 
            { 
                Text = "BlockTicket Security System",
                IconUrl = "https://i.imgur.com/4M34hi2.png"
            },
            Timestamp = securityEvent.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        if (!string.IsNullOrEmpty(securityEvent.UserAgent))
        {
            embed.Fields.Add(new DiscordEmbedField { Name = "User Agent", Value = securityEvent.UserAgent, Inline = false });
        }

        if (!string.IsNullOrEmpty(securityEvent.Location))
        {
            embed.Fields.Add(new DiscordEmbedField { Name = "Location", Value = securityEvent.Location, Inline = true });
        }

        var message = new DiscordMessage
        {
            Username = "Security Alert Bot",
            AvatarUrl = GetAvatarForSeverity(securityEvent.Severity),
            Embeds = new List<DiscordEmbed> { embed }
        };

        await SendMessageAsync(message, isCritical, cancellationToken);
    }

    public async Task SendSuspiciousActivityAlertAsync(SuspiciousActivity suspiciousActivity, CancellationToken cancellationToken = default)
    {
        var color = GetColorForRiskScore(suspiciousActivity.RiskScore);
        var isCritical = suspiciousActivity.RiskScore >= 80;

        var embed = new DiscordEmbed
        {
            Title = $"⚠️ Suspicious Activity: {suspiciousActivity.ActivityType}",
            Description = suspiciousActivity.Description,
            Color = color,
            Fields = new List<DiscordEmbedField>
            {
                new() { Name = "Activity Type", Value = suspiciousActivity.ActivityType, Inline = true },
                new() { Name = "Risk Score", Value = $"{suspiciousActivity.RiskScore:F1}", Inline = true },
                new() { Name = "IP Address", Value = suspiciousActivity.IpAddress, Inline = true },
                new() { Name = "Status", Value = suspiciousActivity.Status.ToString(), Inline = true },
                new() { Name = "User ID", Value = suspiciousActivity.UserId?.ToString() ?? "N/A", Inline = true },
                new() { Name = "Timestamp", Value = suspiciousActivity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true }
            },
            Footer = new DiscordEmbedFooter 
            { 
                Text = "BlockTicket Security System",
                IconUrl = "https://i.imgur.com/4M34hi2.png"
            },
            Timestamp = suspiciousActivity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        if (!string.IsNullOrEmpty(suspiciousActivity.UserAgent))
        {
            embed.Fields.Add(new DiscordEmbedField { Name = "User Agent", Value = suspiciousActivity.UserAgent, Inline = false });
        }

        var message = new DiscordMessage
        {
            Username = "Security Alert Bot",
            AvatarUrl = GetAvatarForRiskScore(suspiciousActivity.RiskScore),
            Embeds = new List<DiscordEmbed> { embed }
        };

        await SendMessageAsync(message, isCritical, cancellationToken);
    }

    public async Task SendAccountLockoutAlertAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default)
    {
        var embed = new DiscordEmbed
        {
            Title = "🔒 Account Lockout",
            Description = $"Account has been locked: {accountLockout.Reason}",
            Color = 0xFFA500, // Orange color
            Fields = new List<DiscordEmbedField>
            {
                new() { Name = "User ID", Value = accountLockout.UserId.ToString(), Inline = true },
                new() { Name = "Reason", Value = accountLockout.Reason, Inline = true },
                new() { Name = "Failed Attempts", Value = accountLockout.FailedAttempts.ToString(), Inline = true },
                new() { Name = "IP Address", Value = accountLockout.IpAddress, Inline = true },
                new() { Name = "Locked At", Value = accountLockout.LockedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true },
                new() { Name = "Unlocked At", Value = accountLockout.UnlockedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Still locked", Inline = true }
            },
            Footer = new DiscordEmbedFooter 
            { 
                Text = "BlockTicket Security System",
                IconUrl = "https://i.imgur.com/4M34hi2.png"
            },
            Timestamp = accountLockout.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        var message = new DiscordMessage
        {
            Username = "Security Alert Bot",
            AvatarUrl = "https://i.imgur.com/lock-icon.png",
            Embeds = new List<DiscordEmbed> { embed }
        };

        await SendMessageAsync(message, false, cancellationToken);
    }

    public async Task SendCriticalAlertAsync(string title, string messageContent, string? context = null, CancellationToken cancellationToken = default)
    {
        var embed = new DiscordEmbed
        {
            Title = $"🚨 CRITICAL ALERT: {title}",
            Description = messageContent,
            Color = 0xFF0000, // Red color
            Fields = new List<DiscordEmbedField>
            {
                new() { Name = "Timestamp", Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true },
                new() { Name = "System", Value = "BlockTicket Identity Service", Inline = true }
            },
            Footer = new DiscordEmbedFooter 
            { 
                Text = "BlockTicket Security System",
                IconUrl = "https://i.imgur.com/4M34hi2.png"
            },
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        if (!string.IsNullOrEmpty(context))
        {
            embed.Fields.Add(new DiscordEmbedField { Name = "Context", Value = context, Inline = false });
        }

        var message = new DiscordMessage
        {
            Username = "Critical Alert Bot",
            AvatarUrl = "https://i.imgur.com/emergency-icon.png",
            Embeds = new List<DiscordEmbed> { embed }
        };

        await SendMessageAsync(message, true, cancellationToken);
    }

    public async Task SendDailySummaryAsync(SecuritySummary summary, CancellationToken cancellationToken = default)
    {
        var color = summary.CriticalEvents > 0 ? 0xFF0000 : 
                   summary.HighSeverityEvents > 0 ? 0xFFA500 : 0x00FF00;

        var embed = new DiscordEmbed
        {
            Title = $"📊 Daily Security Summary - {summary.FromDate:yyyy-MM-dd}",
            Description = "Security events summary for the past 24 hours",
            Color = color,
            Fields = new List<DiscordEmbedField>
            {
                new() { Name = "Total Events", Value = summary.TotalEvents.ToString(), Inline = true },
                new() { Name = "Critical", Value = summary.CriticalEvents.ToString(), Inline = true },
                new() { Name = "High Severity", Value = summary.HighSeverityEvents.ToString(), Inline = true },
                new() { Name = "Medium Severity", Value = summary.MediumSeverityEvents.ToString(), Inline = true },
                new() { Name = "Low Severity", Value = summary.LowSeverityEvents.ToString(), Inline = true },
                new() { Name = "Unresolved", Value = summary.UnresolvedEvents.ToString(), Inline = true },
                new() { Name = "Account Lockouts", Value = summary.AccountLockouts.ToString(), Inline = true },
                new() { Name = "Suspicious Activities", Value = summary.SuspiciousActivities.ToString(), Inline = true },
                new() { Name = "Failed Logins", Value = summary.FailedLogins.ToString(), Inline = true },
                new() { Name = "Successful Logins", Value = summary.SuccessfulLogins.ToString(), Inline = true }
            },
            Footer = new DiscordEmbedFooter 
            { 
                Text = "BlockTicket Security System",
                IconUrl = "https://i.imgur.com/4M34hi2.png"
            },
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        if (summary.TopEventTypes.Any())
        {
            embed.Fields.Add(new DiscordEmbedField 
            { 
                Name = "Top Event Types", 
                Value = string.Join(", ", summary.TopEventTypes.Take(5)), 
                Inline = false 
            });
        }

        if (summary.TopSourceIps.Any())
        {
            embed.Fields.Add(new DiscordEmbedField 
            { 
                Name = "Top Source IPs", 
                Value = string.Join(", ", summary.TopSourceIps.Take(5)), 
                Inline = false 
            });
        }

        var message = new DiscordMessage
        {
            Username = "Security Summary Bot",
            AvatarUrl = "https://i.imgur.com/chart-icon.png",
            Embeds = new List<DiscordEmbed> { embed }
        };

        await SendMessageAsync(message, false, cancellationToken);
    }

    private static int GetColorForSeverity(SecurityEventSeverity severity)
    {
        return severity switch
        {
            SecurityEventSeverity.Critical => 0xFF0000, // Red
            SecurityEventSeverity.High => 0xFFA500,     // Orange
            SecurityEventSeverity.Medium => 0xFFFF00,   // Yellow
            SecurityEventSeverity.Low => 0x00FF00,      // Green
            _ => 0x808080                               // Gray
        };
    }

    private static string GetAvatarForSeverity(SecurityEventSeverity severity)
    {
        return severity switch
        {
            SecurityEventSeverity.Critical => "https://i.imgur.com/emergency-icon.png",
            SecurityEventSeverity.High => "https://i.imgur.com/warning-icon.png",
            SecurityEventSeverity.Medium => "https://i.imgur.com/caution-icon.png",
            SecurityEventSeverity.Low => "https://i.imgur.com/info-icon.png",
            _ => "https://i.imgur.com/4M34hi2.png"
        };
    }

    private static int GetColorForRiskScore(double riskScore)
    {
        return riskScore switch
        {
            >= 80 => 0xFF0000, // Red
            >= 60 => 0xFFA500, // Orange
            >= 40 => 0xFFFF00, // Yellow
            _ => 0x00FF00      // Green
        };
    }

    private static string GetAvatarForRiskScore(double riskScore)
    {
        return riskScore switch
        {
            >= 80 => "https://i.imgur.com/emergency-icon.png",
            >= 60 => "https://i.imgur.com/warning-icon.png",
            >= 40 => "https://i.imgur.com/caution-icon.png",
            _ => "https://i.imgur.com/search-icon.png"
        };
    }
}
