using Identity.Domain.Entities;

namespace Identity.Domain.Models;

/// <summary>
/// Discord message payload structure
/// </summary>
public class DiscordMessage
{
    public string? Content { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? Tts { get; set; } = false;
    public List<DiscordEmbed>? Embeds { get; set; }
}

/// <summary>
/// Discord embed for rich formatting
/// </summary>
public class DiscordEmbed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public int? Color { get; set; }
    public DiscordEmbedFooter? Footer { get; set; }
    public DiscordEmbedImage? Image { get; set; }
    public DiscordEmbedThumbnail? Thumbnail { get; set; }
    public DiscordEmbedAuthor? Author { get; set; }
    public List<DiscordEmbedField>? Fields { get; set; }
    public string? Timestamp { get; set; }
}

/// <summary>
/// Discord embed field for structured data
/// </summary>
public class DiscordEmbedField
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public bool? Inline { get; set; } = false;
}

/// <summary>
/// Discord embed footer
/// </summary>
public class DiscordEmbedFooter
{
    public string? Text { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// Discord embed image
/// </summary>
public class DiscordEmbedImage
{
    public string? Url { get; set; }
}

/// <summary>
/// Discord embed thumbnail
/// </summary>
public class DiscordEmbedThumbnail
{
    public string? Url { get; set; }
}

/// <summary>
/// Discord embed author
/// </summary>
public class DiscordEmbedAuthor
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// Security event notification configuration
/// </summary>
public class SecurityNotificationConfig
{
    public bool Enabled { get; set; } = true;
    public List<NotificationChannel> Channels { get; set; } = new();
    public List<NotificationRule> Rules { get; set; } = new();
    public NotificationThrottling Throttling { get; set; } = new();
}

/// <summary>
/// Notification channel configuration
/// </summary>
public class NotificationChannel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "discord", "email", "sms"
    public string Target { get; set; } = string.Empty; // Webhook URL, email, phone
    public List<SecurityEventSeverity> Severities { get; set; } = new();
    public List<string> EventTypes { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Notification rule for conditional sending
/// </summary>
public class NotificationRule
{
    public string Name { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new();
    public List<SecurityEventSeverity> Severities { get; set; } = new();
    public List<string> Channels { get; set; } = new();
    public NotificationCondition? Condition { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Condition for notification rules
/// </summary>
public class NotificationCondition
{
    public int? MinOccurrences { get; set; }
    public TimeSpan? TimeWindow { get; set; }
    public List<string>? IpAddressFilters { get; set; }
    public List<string>? UserIdFilters { get; set; }
    public string? CustomCondition { get; set; }
}

/// <summary>
/// Notification throttling configuration
/// </summary>
public class NotificationThrottling
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxNotifications { get; set; } = 10;
    public Dictionary<string, int> PerEventTypeLimits { get; set; } = new();
}
