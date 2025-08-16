using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of approval workflow service
/// </summary>
public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly IApprovalWorkflowTemplateRepository _templateRepository;
    private readonly IApprovalAuditLogRepository _auditRepository;
    private readonly IApprovalNotificationService _notificationService;
    private readonly ILogger<ApprovalWorkflowService> _logger;

    public ApprovalWorkflowService(
        IApprovalWorkflowRepository workflowRepository,
        IApprovalWorkflowTemplateRepository templateRepository,
        IApprovalAuditLogRepository auditRepository,
        IApprovalNotificationService notificationService,
        ILogger<ApprovalWorkflowService> logger)
    {
        _workflowRepository = workflowRepository;
        _templateRepository = templateRepository;
        _auditRepository = auditRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApprovalWorkflowResult> CreateWorkflowAsync(
        CreateApprovalWorkflowRequest request,
        Guid requesterId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating approval workflow for operation {OperationType} on entity {EntityType}:{EntityId}",
            request.OperationType, request.EntityType, request.EntityId);

        try
        {
            // Check if operation requires approval
            var requiresApproval = await RequiresApprovalAsync(request.OperationType, organizationId, request.OperationData, cancellationToken);
            if (!requiresApproval)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = ApprovalStatus.Rejected,
                    Messages = { "This operation does not require approval" }
                };
            }

            // Check for existing pending workflows for the same entity
            var hasPendingWorkflows = await _workflowRepository.HasPendingWorkflowsAsync(
                request.EntityType, request.EntityId, cancellationToken);
            
            if (hasPendingWorkflows)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = ApprovalStatus.Rejected,
                    Messages = { "There are already pending approval workflows for this entity" }
                };
            }

            // Get workflow template for operation type
            var template = await _templateRepository.GetByOperationTypeAsync(
                request.OperationType, organizationId, cancellationToken);

            if (template == null)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = ApprovalStatus.Rejected,
                    Messages = { "No approval workflow template found for this operation type" }
                };
            }

            // Check auto-approval conditions
            if (await ShouldAutoApproveAsync(request, template))
            {
                // Auto-approve low-risk operations
                return new ApprovalWorkflowResult
                {
                    Success = true,
                    Status = ApprovalStatus.Approved,
                    Messages = { "Operation auto-approved based on predefined criteria" }
                };
            }

            // Create workflow
            var workflow = new ApprovalWorkflow
            {
                OrganizationId = organizationId,
                RequesterId = requesterId,
                OperationType = request.OperationType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                OperationDescription = request.OperationDescription,
                BusinessJustification = request.BusinessJustification,
                ExpectedImpact = request.ExpectedImpact,
                RiskLevel = request.RiskLevel,
                Priority = request.Priority,
                RequiredApprovals = template.RequiredApprovals,
                ExpiresAt = DateTime.UtcNow.Add(template.DefaultExpirationTime),
                Tags = request.Tags,
                Metadata = request.Metadata
            };

            // Set operation data
            if (request.OperationData != null)
            {
                workflow.SetOperationData(request.OperationData);
            }

            // Save workflow
            workflow = await _workflowRepository.CreateAsync(workflow, cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(workflow.Id, requesterId, "WorkflowCreated", 
                $"Created approval workflow for {request.OperationType}", cancellationToken);

            // Send notifications to approvers
            var approverEmails = await GetApproverEmailsAsync(organizationId, template, cancellationToken);
            if (approverEmails.Any())
            {
                await _notificationService.SendApprovalRequestNotificationAsync(
                    workflow, approverEmails, cancellationToken);
            }

            _logger.LogInformation("Successfully created approval workflow {WorkflowId} for operation {OperationType}",
                workflow.Id, request.OperationType);

            return new ApprovalWorkflowResult
            {
                Success = true,
                WorkflowId = workflow.Id,
                Status = ApprovalStatus.Pending,
                Messages = { "Approval workflow created successfully" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval workflow for operation {OperationType}", request.OperationType);
            return new ApprovalWorkflowResult
            {
                Success = false,
                Status = ApprovalStatus.Rejected,
                Errors = { $"Failed to create approval workflow: {ex.Message}" }
            };
        }
    }

    public async Task<ApprovalWorkflow?> GetWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _workflowRepository.GetWithDetailsAsync(workflowId, cancellationToken);
    }

    public async Task<PagedResult<ApprovalWorkflow>> GetWorkflowsAsync(
        Guid organizationId,
        ApprovalWorkflowFilter filter,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await _workflowRepository.GetPagedAsync(organizationId, filter, pageNumber, pageSize, cancellationToken);
    }

    public async Task<List<ApprovalWorkflow>> GetPendingApprovalsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _workflowRepository.GetPendingForUserAsync(userId, organizationId, cancellationToken);
    }

    public async Task<ApprovalWorkflowResult> SubmitApprovalAsync(
        Guid workflowId,
        ApprovalDecisionRequest decision,
        Guid approverId,
        string approverName,
        string approverEmail,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing approval decision {Decision} for workflow {WorkflowId} by user {ApproverId}",
            decision.Decision, workflowId, approverId);

        try
        {
            var workflow = await _workflowRepository.GetWithDetailsAsync(workflowId, cancellationToken);
            if (workflow == null)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = ApprovalStatus.Rejected,
                    Errors = { "Approval workflow not found" }
                };
            }

            // Validate workflow can be approved
            if (!workflow.CanBeApproved)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = workflow.Status,
                    Errors = { $"Workflow cannot be approved in current status: {workflow.Status}" }
                };
            }

            // Check if user has already approved/rejected this workflow
            var existingDecision = workflow.ApprovalSteps.FirstOrDefault(s => s.ApproverId == approverId);
            if (existingDecision != null)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = workflow.Status,
                    Errors = { "User has already submitted a decision for this workflow" }
                };
            }

            // Create approval step
            var approvalStep = new ApprovalStep
            {
                ApprovalWorkflowId = workflowId,
                ApproverId = approverId,
                ApproverName = approverName,
                ApproverEmail = approverEmail,
                Decision = decision.Decision,
                Comments = decision.Comments,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DecisionMetadata = decision.DecisionMetadata
            };

            await _workflowRepository.AddApprovalStepAsync(workflowId, approvalStep, cancellationToken);

            // Update workflow based on decision
            var result = await ProcessApprovalDecisionAsync(workflow, approvalStep, cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(workflowId, approverId, "ApprovalDecision", 
                $"Submitted {decision.Decision} decision", cancellationToken);

            // Send notifications
            await _notificationService.SendApprovalDecisionNotificationAsync(workflow, approvalStep, cancellationToken);

            if (workflow.IsComplete)
            {
                await _notificationService.SendWorkflowCompletionNotificationAsync(workflow, cancellationToken);
            }

            _logger.LogInformation("Successfully processed approval decision {Decision} for workflow {WorkflowId}",
                decision.Decision, workflowId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval decision for workflow {WorkflowId}", workflowId);
            return new ApprovalWorkflowResult
            {
                Success = false,
                Status = ApprovalStatus.Pending,
                Errors = { $"Failed to process approval decision: {ex.Message}" }
            };
        }
    }

    public async Task<ApprovalWorkflowResult> ExecuteApprovedWorkflowAsync(
        Guid workflowId,
        Guid executorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing approved workflow {WorkflowId} by user {ExecutorId}", workflowId, executorId);

        try
        {
            var workflow = await _workflowRepository.GetWithDetailsAsync(workflowId, cancellationToken);
            if (workflow == null)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Errors = { "Approval workflow not found" }
                };
            }

            if (workflow.Status != ApprovalStatus.Approved)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = workflow.Status,
                    Errors = { "Workflow is not approved and cannot be executed" }
                };
            }

            // Execute the operation (this would delegate to specific operation executors)
            // For now, we'll mark it as executed
            await CreateAuditLogAsync(workflowId, executorId, "WorkflowExecuted", 
                "Workflow operation executed successfully", cancellationToken);

            _logger.LogInformation("Successfully executed approved workflow {WorkflowId}", workflowId);

            return new ApprovalWorkflowResult
            {
                Success = true,
                WorkflowId = workflowId,
                Status = ApprovalStatus.Approved,
                Messages = { "Workflow executed successfully" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing approved workflow {WorkflowId}", workflowId);
            return new ApprovalWorkflowResult
            {
                Success = false,
                Errors = { $"Failed to execute workflow: {ex.Message}" }
            };
        }
    }

    public async Task<ApprovalWorkflowResult> CancelWorkflowAsync(
        Guid workflowId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling workflow {WorkflowId} by user {UserId}", workflowId, userId);

        try
        {
            var workflow = await _workflowRepository.GetWithDetailsAsync(workflowId, cancellationToken);
            if (workflow == null)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Errors = { "Approval workflow not found" }
                };
            }

            if (workflow.IsComplete)
            {
                return new ApprovalWorkflowResult
                {
                    Success = false,
                    Status = workflow.Status,
                    Errors = { "Cannot cancel completed workflow" }
                };
            }

            workflow.Status = ApprovalStatus.Cancelled;
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.CompletionReason = reason;
            workflow.UpdatedAt = DateTime.UtcNow;

            await _workflowRepository.UpdateAsync(workflow, cancellationToken);

            await CreateAuditLogAsync(workflowId, userId, "WorkflowCancelled", 
                $"Workflow cancelled: {reason}", cancellationToken);

            return new ApprovalWorkflowResult
            {
                Success = true,
                WorkflowId = workflowId,
                Status = ApprovalStatus.Cancelled,
                Messages = { "Workflow cancelled successfully" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow {WorkflowId}", workflowId);
            return new ApprovalWorkflowResult
            {
                Success = false,
                Errors = { $"Failed to cancel workflow: {ex.Message}" }
            };
        }
    }

    public async Task<bool> RequiresApprovalAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        object? operationData = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepository.GetByOperationTypeAsync(operationType, organizationId, cancellationToken);
        return template != null && template.IsActive;
    }

    public async Task<ApprovalWorkflowTemplate?> GetWorkflowTemplateAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _templateRepository.GetByOperationTypeAsync(operationType, organizationId, cancellationToken);
    }

    public async Task<ApprovalWorkflowTemplate> UpsertWorkflowTemplateAsync(
        ApprovalWorkflowTemplate template,
        CancellationToken cancellationToken = default)
    {
        return await _templateRepository.UpsertAsync(template, cancellationToken);
    }

    public async Task ProcessExpiredWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing expired workflows");

        try
        {
            var expiredWorkflows = await _workflowRepository.GetExpiredWorkflowsAsync(DateTime.UtcNow, cancellationToken);

            foreach (var workflow in expiredWorkflows)
            {
                workflow.Status = ApprovalStatus.Expired;
                workflow.CompletedAt = DateTime.UtcNow;
                workflow.CompletionReason = "Approval workflow expired";
                workflow.UpdatedAt = DateTime.UtcNow;

                await _workflowRepository.UpdateAsync(workflow, cancellationToken);

                await CreateAuditLogAsync(workflow.Id, Guid.Empty, "WorkflowExpired", 
                    "Workflow expired due to timeout", cancellationToken);
            }

            _logger.LogInformation("Processed {Count} expired workflows", expiredWorkflows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired workflows");
        }
    }

    public async Task ProcessEscalationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing workflow escalations");

        try
        {
            var workflowsNeedingEscalation = await _workflowRepository.GetWorkflowsNeedingEscalationAsync(cancellationToken);

            foreach (var workflow in workflowsNeedingEscalation)
            {
                var template = await _templateRepository.GetByOperationTypeAsync(
                    workflow.OperationType, workflow.OrganizationId, cancellationToken);

                if (template?.EscalationRules?.Any() == true)
                {
                    await ProcessWorkflowEscalationAsync(workflow, template.EscalationRules, cancellationToken);
                }
            }

            _logger.LogInformation("Processed escalations for {Count} workflows", workflowsNeedingEscalation.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflow escalations");
        }
    }

    public async Task<List<ApprovalAuditLog>> GetWorkflowAuditHistoryAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _auditRepository.GetByWorkflowAsync(workflowId, cancellationToken);
    }

    public async Task<ApprovalWorkflowStatistics> GetWorkflowStatisticsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return await _workflowRepository.GetStatisticsAsync(organizationId, fromDate, toDate, cancellationToken);
    }

    #region Private Helper Methods

    private async Task<bool> ShouldAutoApproveAsync(CreateApprovalWorkflowRequest request, ApprovalWorkflowTemplate template)
    {
        if (template.AutoApprovalConditions == null)
            return false;

        // Check risk level
        if (request.RiskLevel > RiskLevel.Low)
            return false;

        // Check impact value if specified
        if (template.AutoApprovalConditions.MaxImpactValue.HasValue)
        {
            // This would need to be implemented based on operation data
            // For now, assume low-risk operations can be auto-approved
        }

        // Check time constraints
        if (template.AutoApprovalConditions.TimeConstraints != null)
        {
            return await CheckTimeConstraintsAsync(request, template.AutoApprovalConditions.TimeConstraints);
        }

        return false;
    }

    private async Task<bool> CheckTimeConstraintsAsync(CreateApprovalWorkflowRequest request, TimeConstraints constraints)
    {
        // Check day of week constraints
        if (constraints.AllowedDaysOfWeek.Any() && !constraints.AllowedDaysOfWeek.Contains(DateTime.UtcNow.DayOfWeek))
            return false;

        // Check time window constraints
        if (constraints.AllowedTimeWindow != null)
        {
            var currentTime = DateTime.UtcNow.TimeOfDay;
            if (currentTime < constraints.AllowedTimeWindow.StartTime || currentTime > constraints.AllowedTimeWindow.EndTime)
                return false;
        }

        return true;
    }

    private async Task<ApprovalWorkflowResult> ProcessApprovalDecisionAsync(
        ApprovalWorkflow workflow,
        ApprovalStep approvalStep,
        CancellationToken cancellationToken)
    {
        if (approvalStep.Decision == ApprovalDecision.Rejected)
        {
            // If any approver rejects, the entire workflow is rejected
            workflow.Status = ApprovalStatus.Rejected;
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.CompletionReason = "Workflow rejected by approver";
        }
        else if (approvalStep.Decision == ApprovalDecision.Approved)
        {
            workflow.CurrentApprovals++;
            
            if (workflow.CurrentApprovals >= workflow.RequiredApprovals)
            {
                // Workflow is fully approved
                workflow.Status = ApprovalStatus.Approved;
                workflow.CompletedAt = DateTime.UtcNow;
                workflow.CompletionReason = "Workflow approved by required number of approvers";
            }
            else
            {
                // Still waiting for more approvals
                workflow.Status = ApprovalStatus.UnderReview;
            }
        }

        workflow.UpdatedAt = DateTime.UtcNow;
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);

        return new ApprovalWorkflowResult
        {
            Success = true,
            WorkflowId = workflow.Id,
            Status = workflow.Status,
            Messages = { $"Approval decision processed. Current status: {workflow.Status}" }
        };
    }

    private async Task ProcessWorkflowEscalationAsync(
        ApprovalWorkflow workflow,
        List<EscalationRule> escalationRules,
        CancellationToken cancellationToken)
    {
        var workflowAge = DateTime.UtcNow - workflow.CreatedAt;

        foreach (var rule in escalationRules.OrderBy(r => r.TriggerAfter))
        {
            if (workflowAge >= rule.TriggerAfter)
            {
                var escalationEmails = await GetEscalationEmailsAsync(workflow.OrganizationId, rule, cancellationToken);
                
                if (escalationEmails.Any())
                {
                    await _notificationService.SendEscalationNotificationAsync(workflow, rule, escalationEmails, cancellationToken);
                }

                await CreateAuditLogAsync(workflow.Id, Guid.Empty, "WorkflowEscalated", 
                    $"Workflow escalated after {workflowAge.TotalHours:F1} hours", cancellationToken);
            }
        }
    }

    private async Task<List<string>> GetApproverEmailsAsync(
        Guid organizationId,
        ApprovalWorkflowTemplate template,
        CancellationToken cancellationToken)
    {
        // This would typically query a user management service or database
        // For now, return placeholder emails
        return new List<string>
        {
            "admin@example.com",
            "manager@example.com"
        };
    }

    private async Task<List<string>> GetEscalationEmailsAsync(
        Guid organizationId,
        EscalationRule rule,
        CancellationToken cancellationToken)
    {
        // This would typically query a user management service or database
        // For now, return placeholder emails
        return new List<string>
        {
            "director@example.com",
            "senior-manager@example.com"
        };
    }

    private async Task CreateAuditLogAsync(
        Guid workflowId,
        Guid userId,
        string action,
        string details,
        CancellationToken cancellationToken)
    {
        var auditLog = new ApprovalAuditLog
        {
            ApprovalWorkflowId = workflowId,
            UserId = userId,
            Action = action,
            Details = details
        };

        await _auditRepository.CreateAsync(auditLog, cancellationToken);
    }

    #endregion
}
