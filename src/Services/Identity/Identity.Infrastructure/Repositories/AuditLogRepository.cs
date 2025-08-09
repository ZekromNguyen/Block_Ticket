using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(IdentityDbContext context, ILogger<AuditLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetFailedAttemptsAsync(TimeSpan timeWindow, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        return await _context.AuditLogs
            .Where(a => !a.Success && a.CreatedAt >= cutoffTime)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetSuspiciousActivitiesAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        // Define suspicious activities: multiple failed logins, MFA failures, etc.
        var suspiciousActions = new[] { "LOGIN_ATTEMPT", "MFA_VERIFICATION", "PASSWORD_CHANGE" };
        var cutoffTime = DateTime.UtcNow.AddHours(-24); // Last 24 hours
        
        return await _context.AuditLogs
            .Where(a => !a.Success && 
                       suspiciousActions.Contains(a.Action) && 
                       a.CreatedAt >= cutoffTime)
            .GroupBy(a => new { a.IpAddress, a.UserId })
            .Where(g => g.Count() >= 3) // 3 or more failed attempts
            .SelectMany(g => g)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Audit log {AuditLogId} added successfully", auditLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding audit log {AuditLogId}", auditLog.Id);
            // Don't rethrow - audit logging should not break the main flow
        }
    }

    public async Task AddRangeAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Added {Count} audit logs successfully", auditLogs.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding audit logs batch");
            // Don't rethrow - audit logging should not break the main flow
        }
    }

    public async Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.LongCountAsync(cancellationToken);
    }

    public async Task<long> GetCountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .LongCountAsync(cancellationToken);
    }
}
