using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class SecurityEventRepository : ISecurityEventRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<SecurityEventRepository> _logger;

    public SecurityEventRepository(IdentityDbContext context, ILogger<SecurityEventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetEventsAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents.AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetUnresolvedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SecurityEvents
            .Where(e => !e.IsResolved && e.Severity >= SecurityEventSeverity.Medium)
            .OrderByDescending(e => e.Severity)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetEventsByTypeAsync(string eventType, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents
            .Where(e => e.EventType == eventType);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetEventsBySeverityAsync(SecurityEventSeverity severity, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents
            .Where(e => e.Severity >= severity);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetRecentLocationEventsAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.SecurityEvents
            .Where(e => e.UserId == userId && e.CreatedAt >= from && !string.IsNullOrEmpty(e.Location))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> GetRecentDeviceEventsAsync(Guid userId, string deviceFingerprint, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Subtract(timeWindow);
        return await _context.SecurityEvents
            .Where(e => e.UserId == userId && e.CreatedAt >= from && e.DeviceFingerprint == deviceFingerprint)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetEventCountAsync(Guid? userId = null, string? eventType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SecurityEvents.AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SecurityEvents.AddAsync(securityEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Security event added: {EventType}", securityEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding security event: {EventType}", securityEvent.EventType);
            throw;
        }
    }

    public async Task UpdateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SecurityEvents.Update(securityEvent);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Security event updated: {EventId}", securityEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security event: {EventId}", securityEvent.Id);
            throw;
        }
    }

    public async Task DeleteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SecurityEvents.Remove(securityEvent);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Security event deleted: {EventId}", securityEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting security event: {EventId}", securityEvent.Id);
            throw;
        }
    }

    public async Task CleanupOldEventsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
            var oldEvents = await _context.SecurityEvents
                .Where(e => e.CreatedAt < cutoffDate && e.IsResolved)
                .ToListAsync(cancellationToken);

            if (oldEvents.Any())
            {
                _context.SecurityEvents.RemoveRange(oldEvents);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} old security events", oldEvents.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old security events");
            throw;
        }
    }
}

public class AccountLockoutRepository : IAccountLockoutRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<AccountLockoutRepository> _logger;

    public AccountLockoutRepository(IdentityDbContext context, ILogger<AccountLockoutRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountLockout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccountLockouts
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<AccountLockout?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountLockouts
            .Where(l => l.UserId == userId && l.IsActive)
            .OrderByDescending(l => l.LockedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountLockout>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountLockouts
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LockedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountLockout>> GetActiveLockoutsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccountLockouts
            .Where(l => l.IsActive)
            .OrderByDescending(l => l.LockedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccountLockout>> GetExpiredLockoutsAsync(TimeSpan lockoutDuration, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(lockoutDuration);
        return await _context.AccountLockouts
            .Where(l => l.IsActive && l.LockedAt < cutoffTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountLockout lockout, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AccountLockouts.AddAsync(lockout, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Account lockout added for user: {UserId}", lockout.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding account lockout for user: {UserId}", lockout.UserId);
            throw;
        }
    }

    public async Task UpdateAsync(AccountLockout lockout, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.AccountLockouts.Update(lockout);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Account lockout updated: {LockoutId}", lockout.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account lockout: {LockoutId}", lockout.Id);
            throw;
        }
    }

    public async Task DeleteAsync(AccountLockout lockout, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.AccountLockouts.Remove(lockout);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Account lockout deleted: {LockoutId}", lockout.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account lockout: {LockoutId}", lockout.Id);
            throw;
        }
    }

    public async Task CleanupExpiredAsync(TimeSpan lockoutDuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredLockouts = await GetExpiredLockoutsAsync(lockoutDuration, cancellationToken);
            foreach (var lockout in expiredLockouts)
            {
                lockout.Unlock("System - Expired");
            }

            if (expiredLockouts.Any())
            {
                _context.AccountLockouts.UpdateRange(expiredLockouts);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} expired account lockouts", expiredLockouts.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired account lockouts");
            throw;
        }
    }
}

public class SuspiciousActivityRepository : ISuspiciousActivityRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<SuspiciousActivityRepository> _logger;

    public SuspiciousActivityRepository(IdentityDbContext context, ILogger<SuspiciousActivityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SuspiciousActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousActivities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetActivitiesAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SuspiciousActivities.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(a => a.RiskScore)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetUnresolvedActivitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousActivities
            .Where(a => a.Status == SuspiciousActivityStatus.Detected || a.Status == SuspiciousActivityStatus.Investigating)
            .OrderByDescending(a => a.RiskScore)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetActivitiesByRiskScoreAsync(double minRiskScore, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SuspiciousActivities
            .Where(a => a.RiskScore >= minRiskScore);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(a => a.RiskScore)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetActivitiesByStatusAsync(SuspiciousActivityStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.SuspiciousActivities
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActivityCountAsync(Guid? userId = null, string? activityType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SuspiciousActivities.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(activityType))
            query = query.Where(a => a.ActivityType == activityType);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SuspiciousActivities.AddAsync(activity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Suspicious activity added: {ActivityType}", activity.ActivityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding suspicious activity: {ActivityType}", activity.ActivityType);
            throw;
        }
    }

    public async Task UpdateAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SuspiciousActivities.Update(activity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Suspicious activity updated: {ActivityId}", activity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating suspicious activity: {ActivityId}", activity.Id);
            throw;
        }
    }

    public async Task DeleteAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.SuspiciousActivities.Remove(activity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Suspicious activity deleted: {ActivityId}", activity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting suspicious activity: {ActivityId}", activity.Id);
            throw;
        }
    }

    public async Task CleanupOldActivitiesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
            var oldActivities = await _context.SuspiciousActivities
                .Where(a => a.CreatedAt < cutoffDate && (a.Status == SuspiciousActivityStatus.Resolved || a.Status == SuspiciousActivityStatus.FalsePositive))
                .ToListAsync(cancellationToken);

            if (oldActivities.Any())
            {
                _context.SuspiciousActivities.RemoveRange(oldActivities);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} old suspicious activities", oldActivities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old suspicious activities");
            throw;
        }
    }
}
