using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface ISecurityEventRepository
{
    Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetEventsAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetUnresolvedEventsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetEventsByTypeAsync(string eventType, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetEventsBySeverityAsync(SecurityEventSeverity severity, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetRecentLocationEventsAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetRecentDeviceEventsAsync(Guid userId, string deviceFingerprint, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<int> GetEventCountAsync(Guid? userId = null, string? eventType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task DeleteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task CleanupOldEventsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}

public interface IAccountLockoutRepository
{
    Task<AccountLockout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountLockout?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountLockout>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountLockout>> GetActiveLockoutsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountLockout>> GetExpiredLockoutsAsync(TimeSpan lockoutDuration, CancellationToken cancellationToken = default);
    Task AddAsync(AccountLockout lockout, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountLockout lockout, CancellationToken cancellationToken = default);
    Task DeleteAsync(AccountLockout lockout, CancellationToken cancellationToken = default);
    Task CleanupExpiredAsync(TimeSpan lockoutDuration, CancellationToken cancellationToken = default);
}

public interface ISuspiciousActivityRepository
{
    Task<SuspiciousActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousActivity>> GetActivitiesAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousActivity>> GetUnresolvedActivitiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousActivity>> GetActivitiesByRiskScoreAsync(double minRiskScore, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousActivity>> GetActivitiesByStatusAsync(SuspiciousActivityStatus status, CancellationToken cancellationToken = default);
    Task<int> GetActivityCountAsync(Guid? userId = null, string? activityType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task AddAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default);
    Task UpdateAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default);
    Task DeleteAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default);
    Task CleanupOldActivitiesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}
