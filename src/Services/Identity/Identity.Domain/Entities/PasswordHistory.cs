using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class PasswordHistory : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;

    // Navigation property
    public User User { get; private set; } = null!;

    private PasswordHistory() { } // For EF Core

    public PasswordHistory(Guid userId, string passwordHash)
    {
        UserId = userId;
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Checks if this password history entry is still within the retention period
    /// </summary>
    /// <param name="retentionDays">Number of days to retain password history</param>
    /// <returns>True if the entry should be retained, false if it can be cleaned up</returns>
    public bool IsWithinRetentionPeriod(int retentionDays)
    {
        return CreatedAt.AddDays(retentionDays) > DateTime.UtcNow;
    }
}
