namespace Identity.Domain.Configuration;

public class PasswordConfiguration
{
    /// <summary>
    /// Number of previous passwords to remember and prevent reuse.
    /// Default is 5 passwords.
    /// </summary>
    public int PasswordHistoryCount { get; set; } = 5;
    
    /// <summary>
    /// Whether password history enforcement is enabled.
    /// Default is true for security compliance.
    /// </summary>
    public bool EnablePasswordHistory { get; set; } = true;
    
    /// <summary>
    /// Maximum age in days for password history entries to be considered.
    /// Older entries are automatically cleaned up. Default is 365 days (1 year).
    /// </summary>
    public int PasswordHistoryRetentionDays { get; set; } = 365;
}
