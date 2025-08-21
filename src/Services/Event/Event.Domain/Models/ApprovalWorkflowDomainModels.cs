using Event.Domain.Models;

namespace Event.Domain.Models;

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
