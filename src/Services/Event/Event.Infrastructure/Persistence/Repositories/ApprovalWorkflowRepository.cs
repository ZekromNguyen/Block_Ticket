using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for approval workflows
/// </summary>
public class ApprovalWorkflowRepository : IApprovalWorkflowRepository
{
    private readonly EventDbContext _context;

    public ApprovalWorkflowRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalWorkflow> CreateAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken = default)
    {
        _context.ApprovalWorkflows.Add(workflow);
        await _context.SaveChangesAsync(cancellationToken);
        return workflow;
    }

    public async Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<ApprovalWorkflow?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<ApprovalWorkflow> UpdateAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken = default)
    {
        _context.ApprovalWorkflows.Update(workflow);
        await _context.SaveChangesAsync(cancellationToken);
        return workflow;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workflow = await GetByIdAsync(id, cancellationToken);
        if (workflow != null)
        {
            _context.ApprovalWorkflows.Remove(workflow);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PagedResult<ApprovalWorkflow>> GetPagedAsync(
        Guid organizationId,
        ApprovalWorkflowFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .Where(w => w.OrganizationId == organizationId);

        // Apply filters
        query = ApplyFilters(query, filter);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging and ordering
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApprovalWorkflow>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<ApprovalWorkflow>> GetPendingForUserAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        // This would typically involve a user-role mapping table
        // For now, return workflows that are pending and don't have a decision from this user
        return await _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .Where(w => w.OrganizationId == organizationId && 
                       w.Status == ApprovalStatus.Pending &&
                       !w.ApprovalSteps.Any(s => s.ApproverId == userId))
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalWorkflow>> GetByStatusAsync(
        ApprovalStatus status,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .Where(w => w.Status == status);

        if (organizationId.HasValue)
        {
            query = query.Where(w => w.OrganizationId == organizationId.Value);
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalWorkflow>> GetExpiredWorkflowsAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .Where(w => w.Status == ApprovalStatus.Pending && w.ExpiresAt <= beforeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalWorkflow>> GetWorkflowsNeedingEscalationAsync(
        CancellationToken cancellationToken = default)
    {
        // This would involve checking escalation rules against workflow creation time
        // For now, return workflows pending for more than 24 hours
        var escalationThreshold = DateTime.UtcNow.AddHours(-24);
        
        return await _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .Where(w => w.Status == ApprovalStatus.Pending && w.CreatedAt <= escalationThreshold)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ApprovalWorkflow>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .Include(w => w.ApprovalSteps)
            .Where(w => w.EntityType == entityType && w.EntityId == entityId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalWorkflowStatistics> GetStatisticsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApprovalWorkflows
            .Where(w => w.OrganizationId == organizationId);

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= toDate.Value);
        }

        var workflows = await query.ToListAsync(cancellationToken);

        var statistics = new ApprovalWorkflowStatistics
        {
            TotalWorkflows = workflows.Count,
            PendingApprovals = workflows.Count(w => w.Status == ApprovalStatus.Pending),
            ApprovedWorkflows = workflows.Count(w => w.Status == ApprovalStatus.Approved),
            RejectedWorkflows = workflows.Count(w => w.Status == ApprovalStatus.Rejected),
            ExpiredWorkflows = workflows.Count(w => w.Status == ApprovalStatus.Expired),
            
            WorkflowsByType = workflows
                .GroupBy(w => w.OperationType)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            WorkflowsByRiskLevel = workflows
                .GroupBy(w => w.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate average approval time for completed workflows
        var completedWorkflows = workflows.Where(w => w.CompletedAt.HasValue).ToList();
        if (completedWorkflows.Any())
        {
            statistics.AverageApprovalTime = completedWorkflows
                .Average(w => (w.CompletedAt!.Value - w.CreatedAt).TotalHours);
        }

        return statistics;
    }

    public async Task<bool> HasPendingWorkflowsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflows
            .AnyAsync(w => w.EntityType == entityType && 
                          w.EntityId == entityId && 
                          w.Status == ApprovalStatus.Pending, 
                     cancellationToken);
    }

    public async Task<ApprovalStep> AddApprovalStepAsync(
        Guid workflowId,
        ApprovalStep step,
        CancellationToken cancellationToken = default)
    {
        step.ApprovalWorkflowId = workflowId;
        _context.ApprovalSteps.Add(step);
        await _context.SaveChangesAsync(cancellationToken);
        return step;
    }

    public async Task<List<ApprovalStep>> GetApprovalStepsAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalSteps
            .Where(s => s.ApprovalWorkflowId == workflowId)
            .OrderBy(s => s.DecisionAt)
            .ToListAsync(cancellationToken);
    }

    #region Private Helper Methods

    private IQueryable<ApprovalWorkflow> ApplyFilters(IQueryable<ApprovalWorkflow> query, ApprovalWorkflowFilter filter)
    {
        if (filter.Status.HasValue)
        {
            query = query.Where(w => w.Status == filter.Status.Value);
        }

        if (filter.OperationType.HasValue)
        {
            query = query.Where(w => w.OperationType == filter.OperationType.Value);
        }

        if (filter.RiskLevel.HasValue)
        {
            query = query.Where(w => w.RiskLevel == filter.RiskLevel.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(w => w.Priority == filter.Priority.Value);
        }

        if (filter.RequesterId.HasValue)
        {
            query = query.Where(w => w.RequesterId == filter.RequesterId.Value);
        }

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            query = query.Where(w => w.EntityType == filter.EntityType);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(w => w.EntityId == filter.EntityId.Value);
        }

        if (filter.CreatedAfter.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= filter.CreatedAfter.Value);
        }

        if (filter.CreatedBefore.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= filter.CreatedBefore.Value);
        }

        if (filter.ExpiresAfter.HasValue)
        {
            query = query.Where(w => w.ExpiresAt >= filter.ExpiresAfter.Value);
        }

        if (filter.ExpiresBefore.HasValue)
        {
            query = query.Where(w => w.ExpiresAt <= filter.ExpiresBefore.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(w => w.OperationDescription.Contains(filter.SearchTerm) ||
                                    w.BusinessJustification.Contains(filter.SearchTerm) ||
                                    w.RequesterName.Contains(filter.SearchTerm));
        }

        if (filter.Tags.Any())
        {
            // This would require JSON querying capabilities for PostgreSQL
            // For now, we'll skip tag filtering in the database query
        }

        return query;
    }

    #endregion
}
