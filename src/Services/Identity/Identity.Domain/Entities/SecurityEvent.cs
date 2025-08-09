using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class SecurityEvent : BaseAuditableEntity
{
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EventCategory { get; private set; } = string.Empty;
    public SecurityEventSeverity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public string? Location { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public string? SessionId { get; private set; }
    public string? AdditionalData { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? Resolution { get; private set; }

    private SecurityEvent() { } // For EF Core

    public SecurityEvent(
        Guid? userId,
        string eventType,
        string eventCategory,
        SecurityEventSeverity severity,
        string description,
        string ipAddress,
        string? userAgent = null,
        string? location = null,
        string? deviceFingerprint = null,
        string? sessionId = null,
        string? additionalData = null)
    {
        UserId = userId;
        EventType = eventType;
        EventCategory = eventCategory;
        Severity = severity;
        Description = description;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Location = location;
        DeviceFingerprint = deviceFingerprint;
        SessionId = sessionId;
        AdditionalData = additionalData;
        IsResolved = false;
    }

    public void Resolve(string resolvedBy, string resolution)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        Resolution = resolution;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAdditionalData(string additionalData)
    {
        AdditionalData = additionalData;
        UpdatedAt = DateTime.UtcNow;
    }

    // Factory methods for common security events
    public static SecurityEvent CreateLoginAttempt(Guid? userId, string ipAddress, string? userAgent, bool successful, string? reason = null)
    {
        return new SecurityEvent(
            userId,
            successful ? SecurityEventTypes.LoginSuccess : SecurityEventTypes.LoginFailure,
            SecurityEventCategories.Authentication,
            successful ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium,
            successful ? "User login successful" : $"User login failed: {reason}",
            ipAddress,
            userAgent);
    }

    public static SecurityEvent CreateAccountLockout(Guid userId, string ipAddress, string reason)
    {
        return new SecurityEvent(
            userId,
            SecurityEventTypes.AccountLocked,
            SecurityEventCategories.Security,
            SecurityEventSeverity.High,
            $"Account locked: {reason}",
            ipAddress);
    }

    public static SecurityEvent CreateSuspiciousActivity(Guid? userId, string eventType, string description, string ipAddress, string? userAgent = null)
    {
        return new SecurityEvent(
            userId,
            eventType,
            SecurityEventCategories.Security,
            SecurityEventSeverity.High,
            description,
            ipAddress,
            userAgent);
    }

    public static SecurityEvent CreatePasswordChange(Guid userId, string ipAddress, string? userAgent)
    {
        return new SecurityEvent(
            userId,
            SecurityEventTypes.PasswordChanged,
            SecurityEventCategories.Account,
            SecurityEventSeverity.Medium,
            "User password changed",
            ipAddress,
            userAgent);
    }

    public static SecurityEvent CreateMfaEvent(Guid userId, string eventType, string description, string ipAddress, string? userAgent = null)
    {
        return new SecurityEvent(
            userId,
            eventType,
            SecurityEventCategories.MFA,
            SecurityEventSeverity.Medium,
            description,
            ipAddress,
            userAgent);
    }

    public static SecurityEvent CreatePermissionViolation(Guid? userId, string resource, string action, string ipAddress, string? userAgent = null)
    {
        return new SecurityEvent(
            userId,
            SecurityEventTypes.PermissionDenied,
            SecurityEventCategories.Authorization,
            SecurityEventSeverity.High,
            $"Permission denied for {action} on {resource}",
            ipAddress,
            userAgent);
    }
}

public class AccountLockout : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime LockedAt { get; private set; }
    public DateTime? UnlockedAt { get; private set; }
    public int FailedAttempts { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? UnlockedBy { get; private set; }

    private AccountLockout() { } // For EF Core

    public AccountLockout(Guid userId, string reason, int failedAttempts, string ipAddress)
    {
        UserId = userId;
        Reason = reason;
        FailedAttempts = failedAttempts;
        IpAddress = ipAddress;
        LockedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Unlock(string unlockedBy)
    {
        IsActive = false;
        UnlockedAt = DateTime.UtcNow;
        UnlockedBy = unlockedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(TimeSpan lockoutDuration)
    {
        return DateTime.UtcNow > LockedAt.Add(lockoutDuration);
    }
}

public class SuspiciousActivity : BaseAuditableEntity
{
    public Guid? UserId { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserAgent { get; private set; }
    public string? Location { get; private set; }
    public double RiskScore { get; private set; }
    public SuspiciousActivityStatus Status { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }

    private SuspiciousActivity() { } // For EF Core

    public SuspiciousActivity(
        Guid? userId,
        string activityType,
        string description,
        string ipAddress,
        double riskScore,
        string? userAgent = null,
        string? location = null)
    {
        UserId = userId;
        ActivityType = activityType;
        Description = description;
        IpAddress = ipAddress;
        RiskScore = riskScore;
        UserAgent = userAgent;
        Location = location;
        Status = SuspiciousActivityStatus.Detected;
    }

    public void MarkAsInvestigating()
    {
        Status = SuspiciousActivityStatus.Investigating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve(string resolution, string resolvedBy)
    {
        Status = SuspiciousActivityStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFalsePositive(string resolvedBy)
    {
        Status = SuspiciousActivityStatus.FalsePositive;
        Resolution = "Marked as false positive";
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SecurityEventSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum SuspiciousActivityStatus
{
    Detected = 1,
    Investigating = 2,
    Resolved = 3,
    FalsePositive = 4
}

public static class SecurityEventTypes
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailure = "LOGIN_FAILURE";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountUnlocked = "ACCOUNT_UNLOCKED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string PasswordResetCompleted = "PASSWORD_RESET_COMPLETED";
    public const string MfaEnabled = "MFA_ENABLED";
    public const string MfaDisabled = "MFA_DISABLED";
    public const string MfaSuccess = "MFA_SUCCESS";
    public const string MfaFailure = "MFA_FAILURE";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string SuspiciousLogin = "SUSPICIOUS_LOGIN";
    public const string MultipleFailedLogins = "MULTIPLE_FAILED_LOGINS";
    public const string UnusualLocation = "UNUSUAL_LOCATION";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string SessionTerminated = "SESSION_TERMINATED";
}

public static class SecurityEventCategories
{
    public const string Authentication = "AUTHENTICATION";
    public const string Authorization = "AUTHORIZATION";
    public const string Account = "ACCOUNT";
    public const string MFA = "MFA";
    public const string Security = "SECURITY";
    public const string Session = "SESSION";
    public const string Token = "TOKEN";
}
