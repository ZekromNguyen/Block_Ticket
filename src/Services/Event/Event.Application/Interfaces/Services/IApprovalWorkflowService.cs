using Event.Domain.Models;
using Event.Application.Common.Models;

namespace Event.Application.Interfaces.Services;

/// <summary>
/// Service for managing approval workflows for sensitive operations
/// </summary>
public interface IApprovalWorkflowService
{
    /// <summary>
    /// Create a new approval workflow for a sensitive operation
    /// </summary>
    Task<ApprovalWorkflowResult> CreateWorkflowAsync(
        CreateApprovalWorkflowRequest request,
        Guid requesterId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflow by ID
    /// </summary>
    Task<ApprovalWorkflow?> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all approval workflows for an organization
    /// </summary>
    Task<PagedResult<ApprovalWorkflow>> GetWorkflowsAsync(
        Guid organizationId,
        ApprovalWorkflowFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflows pending approval for a specific user
    /// </summary>
    Task<List<ApprovalWorkflow>> GetPendingApprovalsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit an approval decision
    /// </summary>
    Task<ApprovalWorkflowResult> SubmitApprovalAsync(
        Guid workflowId,
        ApprovalDecisionRequest decision,
        Guid approverId,
        string approverName,
        string approverEmail,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an approved workflow
    /// </summary>
    Task<ApprovalWorkflowResult> ExecuteApprovedWorkflowAsync(
        Guid workflowId,
        Guid executorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a pending approval workflow
    /// </summary>
    Task<ApprovalWorkflowResult> CancelWorkflowAsync(
        Guid workflowId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an operation requires approval
    /// </summary>
    Task<bool> RequiresApprovalAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        object? operationData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval workflow template for operation type
    /// </summary>
    Task<ApprovalWorkflowTemplate?> GetWorkflowTemplateAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update approval workflow template
    /// </summary>
    Task<ApprovalWorkflowTemplate> UpsertWorkflowTemplateAsync(
        ApprovalWorkflowTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process expired workflows
    /// </summary>
    Task ProcessExpiredWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process escalations for delayed workflows
    /// </summary>
    Task ProcessEscalationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow audit history
    /// </summary>
    Task<List<ApprovalAuditLog>> GetWorkflowAuditHistoryAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow statistics for dashboard
    /// </summary>
    Task<ApprovalWorkflowStatistics> GetWorkflowStatisticsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for executing approved operations
/// </summary>
public interface IApprovalOperationExecutor
{
    /// <summary>
    /// Execute a specific type of approved operation
    /// </summary>
    Task<OperationExecutionResult> ExecuteOperationAsync(
        ApprovalOperationType operationType,
        string entityType,
        Guid entityId,
        object operationData,
        Guid executorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if an operation can be executed
    /// </summary>
    Task<OperationValidationResult> ValidateOperationAsync(
        ApprovalOperationType operationType,
        string entityType,
        Guid entityId,
        object operationData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported operation types
    /// </summary>
    List<ApprovalOperationType> GetSupportedOperationTypes();
}

/// <summary>
/// Service for managing approval notifications
/// </summary>
public interface IApprovalNotificationService
{
    /// <summary>
    /// Send notification for new approval request
    /// </summary>
    Task SendApprovalRequestNotificationAsync(
        ApprovalWorkflow workflow,
        List<string> approverEmails,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification for approval decision
    /// </summary>
    Task SendApprovalDecisionNotificationAsync(
        ApprovalWorkflow workflow,
        ApprovalStep decision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification for workflow completion
    /// </summary>
    Task SendWorkflowCompletionNotificationAsync(
        ApprovalWorkflow workflow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send escalation notification
    /// </summary>
    Task SendEscalationNotificationAsync(
        ApprovalWorkflow workflow,
        EscalationRule escalationRule,
        List<string> escalationEmails,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send expiration warning notification
    /// </summary>
    Task SendExpirationWarningNotificationAsync(
        ApprovalWorkflow workflow,
        TimeSpan timeUntilExpiration,
        CancellationToken cancellationToken = default);
}
