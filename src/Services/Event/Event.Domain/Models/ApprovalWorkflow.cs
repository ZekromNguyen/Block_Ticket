using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Event.Domain.Models;

/// <summary>
/// Represents an approval workflow for sensitive operations
/// </summary>
public class ApprovalWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of operation requiring approval
    /// </summary>
    public ApprovalOperationType OperationType { get; set; }
    
    /// <summary>
    /// Entity being operated on (Event, Venue, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity being operated on
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// Human-readable description of the operation
    /// </summary>
    public string OperationDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the approval workflow
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// Required number of approvals for this operation
    /// </summary>
    public int RequiredApprovals { get; set; } = 2;
    
    /// <summary>
    /// Current number of approvals received
    /// </summary>
    public int CurrentApprovals { get; set; } = 0;
    
    /// <summary>
    /// List of approval steps/decisions
    /// </summary>
    public List<ApprovalStep> ApprovalSteps { get; set; } = new();
    
    /// <summary>
    /// Serialized data of the operation being requested
    /// </summary>
    public string OperationData { get; set; } = string.Empty;
    
    /// <summary>
    /// Business justification for the operation
    /// </summary>
    public string BusinessJustification { get; set; } = string.Empty;
    
    /// <summary>
    /// Risk level assessment
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    
    /// <summary>
    /// Expected impact of the operation
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;
    
    /// <summary>
    /// When the approval workflow was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the approval workflow was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the approval expires if not completed
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When the approval was completed (approved/rejected)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Final decision reason
    /// </summary>
    public string? CompletionReason { get; set; }
    
    /// <summary>
    /// Priority level for processing
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;
    
    /// <summary>
    /// Tags for categorization and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Any additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Check if the workflow is complete (approved or rejected)
    /// </summary>
    public bool IsComplete => Status == ApprovalStatus.Approved || Status == ApprovalStatus.Rejected;

    /// <summary>
    /// Check if the workflow has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt && !IsComplete;

    /// <summary>
    /// Check if the workflow can be approved
    /// </summary>
    public bool CanBeApproved => Status == ApprovalStatus.Pending && !IsExpired;

    /// <summary>
    /// Get serialized operation data as type T
    /// </summary>
    public T? GetOperationData<T>()
    {
        if (string.IsNullOrEmpty(OperationData))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(OperationData);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Set operation data from object
    /// </summary>
    public void SetOperationData<T>(T data)
    {
        OperationData = JsonSerializer.Serialize(data);
    }
}

/// <summary>
/// Represents an individual approval step/decision
/// </summary>
public class ApprovalStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowId { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public string ApproverEmail { get; set; } = string.Empty;
    public string ApproverRole { get; set; } = string.Empty;
    
    /// <summary>
    /// Decision made by the approver
    /// </summary>
    public ApprovalDecision Decision { get; set; }
    
    /// <summary>
    /// Comments from the approver
    /// </summary>
    public string Comments { get; set; } = string.Empty;
    
    /// <summary>
    /// When the decision was made
    /// </summary>
    public DateTime DecisionAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// IP address of the approver
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent of the approver
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Any additional decision metadata
    /// </summary>
    public Dictionary<string, object> DecisionMetadata { get; set; } = new();
}

/// <summary>
/// Template for approval workflows based on operation type
/// </summary>
public class ApprovalWorkflowTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ApprovalOperationType OperationType { get; set; }
    public int RequiredApprovals { get; set; } = 2;
    public List<string> RequiredRoles { get; set; } = new();
    public RiskLevel DefaultRiskLevel { get; set; } = RiskLevel.Medium;
    public TimeSpan DefaultExpirationTime { get; set; } = TimeSpan.FromDays(7);
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Conditions that must be met for auto-approval
    /// </summary>
    public AutoApprovalConditions? AutoApprovalConditions { get; set; }
    
    /// <summary>
    /// Escalation rules if approval takes too long
    /// </summary>
    public List<EscalationRule> EscalationRules { get; set; } = new();
}

/// <summary>
/// Conditions for auto-approval of low-risk operations
/// </summary>
public class AutoApprovalConditions
{
    /// <summary>
    /// Maximum value/impact threshold for auto-approval
    /// </summary>
    public decimal? MaxImpactValue { get; set; }
    
    /// <summary>
    /// Time constraints for auto-approval
    /// </summary>
    public TimeConstraints? TimeConstraints { get; set; }
    
    /// <summary>
    /// Additional criteria that must be met
    /// </summary>
    public Dictionary<string, object> Criteria { get; set; } = new();
}

/// <summary>
/// Time-based constraints for operations
/// </summary>
public class TimeConstraints
{
    /// <summary>
    /// Minimum time before event for the operation
    /// </summary>
    public TimeSpan? MinTimeBeforeEvent { get; set; }
    
    /// <summary>
    /// Maximum time before event for the operation
    /// </summary>
    public TimeSpan? MaxTimeBeforeEvent { get; set; }
    
    /// <summary>
    /// Allowed days of week for the operation
    /// </summary>
    public List<DayOfWeek> AllowedDaysOfWeek { get; set; } = new();
    
    /// <summary>
    /// Allowed time window during the day
    /// </summary>
    public TimeWindow? AllowedTimeWindow { get; set; }
}

/// <summary>
/// Time window during the day
/// </summary>
public class TimeWindow
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Escalation rule when approval is delayed
/// </summary>
public class EscalationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TimeSpan TriggerAfter { get; set; }
    public List<string> EscalateToRoles { get; set; } = new();
    public List<Guid> EscalateToUsers { get; set; } = new();
    public string EscalationMessage { get; set; } = string.Empty;
    public bool NotifyOriginalApprovers { get; set; } = true;
}

/// <summary>
/// Audit log entry for approval workflow events
/// </summary>
public class ApprovalAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of operations that require approval
/// </summary>
public enum ApprovalOperationType
{
    EventPublish,
    EventCancel,
    EventPriceChange,
    EventDateChange,
    EventCapacityIncrease,
    EventCapacityDecrease,
    VenueModification,
    SeatMapImport,
    SeatMapBulkOperation,
    PricingRuleCreation,
    PricingRuleModification,
    BulkRefund,
    EventArchive,
    VenueDeactivation,
    TicketTypeCreation,
    TicketTypeModification,
    ReservationOverride,
    AdminOverride,
    DataExport,
    SecurityRoleChange
}

/// <summary>
/// Status of approval workflow
/// </summary>
public enum ApprovalStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Expired,
    Cancelled
}

/// <summary>
/// Individual approval decision
/// </summary>
public enum ApprovalDecision
{
    Pending,
    Approved,
    Rejected,
    RequestMoreInfo
}

/// <summary>
/// Risk level assessment
/// </summary>
public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Priority level for processing
/// </summary>
public enum Priority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Result of an approval workflow operation
/// </summary>
public class ApprovalWorkflowResult
{
    public bool Success { get; set; }
    public Guid? WorkflowId { get; set; }
    public ApprovalStatus Status { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request to create an approval workflow
/// </summary>
public class CreateApprovalWorkflowRequest
{
    [Required]
    public ApprovalOperationType OperationType { get; set; }
    
    [Required]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    public Guid EntityId { get; set; }
    
    [Required]
    public string OperationDescription { get; set; } = string.Empty;
    
    [Required]
    public string BusinessJustification { get; set; } = string.Empty;
    
    public string ExpectedImpact { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    public Priority Priority { get; set; } = Priority.Normal;
    public object? OperationData { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request to approve or reject a workflow
/// </summary>
public class ApprovalDecisionRequest
{
    [Required]
    public ApprovalDecision Decision { get; set; }
    
    public string Comments { get; set; } = string.Empty;
    public Dictionary<string, object> DecisionMetadata { get; set; } = new();
}
