namespace Identity.Domain.Configuration;

public class SessionConfiguration
{
    public int MaxConcurrentSessions { get; set; } = 5;
    public SessionLimitBehavior SessionLimitBehavior { get; set; } = SessionLimitBehavior.RevokeOldest;
    public bool EnableSessionLimits { get; set; } = true;
}

public enum SessionLimitBehavior
{
    /// <summary>
    /// Revoke the oldest sessions when limit is exceeded
    /// </summary>
    RevokeOldest,
    
    /// <summary>
    /// Reject new login when limit is exceeded
    /// </summary>
    RejectNew,
    
    /// <summary>
    /// Allow unlimited sessions (effectively disables limits)
    /// </summary>
    Unlimited
}
