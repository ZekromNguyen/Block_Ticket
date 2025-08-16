using Event.Domain.Models;

namespace Event.Domain.Services;

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

/// <summary>
/// Filter for approval workflow queries
/// </summary>
public class ApprovalWorkflowFilter
{
    public ApprovalStatus? Status { get; set; }
    public ApprovalOperationType? OperationType { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public Priority? Priority { get; set; }
    public Guid? RequesterId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? ExpiresAfter { get; set; }
    public DateTime? ExpiresBefore { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Statistics for approval workflows
/// </summary>
public class ApprovalWorkflowStatistics
{
    public int TotalWorkflows { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedWorkflows { get; set; }
    public int RejectedWorkflows { get; set; }
    public int ExpiredWorkflows { get; set; }
    public double AverageApprovalTime { get; set; }
    public Dictionary<ApprovalOperationType, int> WorkflowsByType { get; set; } = new();
    public Dictionary<RiskLevel, int> WorkflowsByRiskLevel { get; set; } = new();
    public Dictionary<string, int> WorkflowsByApprover { get; set; } = new();
    public List<ApprovalWorkflowTrend> ApprovalTrends { get; set; } = new();
}

/// <summary>
/// Approval workflow trend data
/// </summary>
public class ApprovalWorkflowTrend
{
    public DateTime Date { get; set; }
    public int Created { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Expired { get; set; }
}

/// <summary>
/// Result of operation execution
/// </summary>
public class OperationExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> ResultData { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of operation validation
/// </summary>
public class OperationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationData { get; set; } = new();
}

/// <summary>
/// Paged result container
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
