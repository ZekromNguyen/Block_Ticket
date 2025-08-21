using Event.Domain.Models;
using Event.Domain.Common;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for approval workflows
/// </summary>
public interface IApprovalWorkflowRepository
{
    /// <summary>
    /// Create a new approval workflow
    /// </summary>
    Task<ApprovalWorkflow> CreateAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflow by ID
    /// </summary>
    Task<ApprovalWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflow with all related data
    /// </summary>
    Task<ApprovalWorkflow?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an approval workflow
    /// </summary>
    Task<ApprovalWorkflow> UpdateAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an approval workflow
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflows with filtering and paging
    /// </summary>
    Task<PagedResult<ApprovalWorkflow>> GetPagedAsync(
        Guid organizationId,
        ApprovalWorkflowFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending approval workflows for a specific user
    /// </summary>
    Task<List<ApprovalWorkflow>> GetPendingForUserAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflows by status
    /// </summary>
    Task<List<ApprovalWorkflow>> GetByStatusAsync(
        ApprovalStatus status,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired workflows
    /// </summary>
    Task<List<ApprovalWorkflow>> GetExpiredWorkflowsAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflows that need escalation
    /// </summary>
    Task<List<ApprovalWorkflow>> GetWorkflowsNeedingEscalationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflows by entity
    /// </summary>
    Task<List<ApprovalWorkflow>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow statistics
    /// </summary>
    Task<ApprovalWorkflowStatistics> GetStatisticsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if entity has pending workflows
    /// </summary>
    Task<bool> HasPendingWorkflowsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add approval step to workflow
    /// </summary>
    Task<ApprovalStep> AddApprovalStepAsync(
        Guid workflowId,
        ApprovalStep step,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval steps for workflow
    /// </summary>
    Task<List<ApprovalStep>> GetApprovalStepsAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for approval workflow templates
/// </summary>
public interface IApprovalWorkflowTemplateRepository
{
    /// <summary>
    /// Create or update approval workflow template
    /// </summary>
    Task<ApprovalWorkflowTemplate> UpsertAsync(
        ApprovalWorkflowTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflow template by operation type
    /// </summary>
    Task<ApprovalWorkflowTemplate?> GetByOperationTypeAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates for organization
    /// </summary>
    Task<List<ApprovalWorkflowTemplate>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template by ID
    /// </summary>
    Task<ApprovalWorkflowTemplate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete template
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active templates
    /// </summary>
    Task<List<ApprovalWorkflowTemplate>> GetActiveTemplatesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for approval audit logs
/// </summary>
public interface IApprovalAuditLogRepository
{
    /// <summary>
    /// Create audit log entry
    /// </summary>
    Task<ApprovalAuditLog> CreateAsync(
        ApprovalAuditLog auditLog,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for workflow
    /// </summary>
    Task<List<ApprovalAuditLog>> GetByWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs with filtering
    /// </summary>
    Task<PagedResult<ApprovalAuditLog>> GetPagedAsync(
        Guid? workflowId = null,
        Guid? userId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by user
    /// </summary>
    Task<List<ApprovalAuditLog>> GetByUserAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old audit logs
    /// </summary>
    Task CleanupOldLogsAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default);
}
