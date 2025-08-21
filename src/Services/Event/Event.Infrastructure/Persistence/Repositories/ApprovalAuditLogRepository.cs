using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Infrastructure.Persistence;
using Event.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for approval audit logs
/// </summary>
public class ApprovalAuditLogRepository : IApprovalAuditLogRepository
{
    private readonly EventDbContext _context;

    public ApprovalAuditLogRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalAuditLog> CreateAsync(
        ApprovalAuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        auditLog.Timestamp = DateTime.UtcNow;
        _context.ApprovalAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<List<ApprovalAuditLog>> GetByWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalAuditLogs
            .Where(a => a.ApprovalWorkflowId == workflowId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ApprovalAuditLog>> GetPagedAsync(
        Guid? workflowId = null,
        Guid? userId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalAuditLogs.AsQueryable();

        // Apply filters
        if (workflowId.HasValue)
        {
            query = query.Where(a => a.ApprovalWorkflowId == workflowId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging and ordering
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApprovalAuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<ApprovalAuditLog>> GetByUserAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalAuditLogs
            .Where(a => a.UserId == userId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupOldLogsAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        var oldLogs = _context.ApprovalAuditLogs
            .Where(a => a.Timestamp < beforeDate);

        _context.ApprovalAuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
