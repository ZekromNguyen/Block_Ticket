using Event.Domain.Models;
using Event.Application.Interfaces.Infrastructure;
using Event.Application.Interfaces.Services;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Event.Infrastructure.Services;

/// <summary>
/// Service for sending approval workflow notifications
/// </summary>
public class ApprovalNotificationService : IApprovalNotificationService
{
    private readonly Configuration.IEmailService _emailService;
    private readonly ILogger<ApprovalNotificationService> _logger;

    public ApprovalNotificationService(
        Configuration.IEmailService emailService,
        ILogger<ApprovalNotificationService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendApprovalRequestNotificationAsync(
        ApprovalWorkflow workflow,
        List<string> approverEmails,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending approval request notification for workflow {WorkflowId} to {ApproverCount} approvers",
            workflow.Id, approverEmails.Count);

        try
        {
            var subject = $"Approval Required: {workflow.OperationType} - {workflow.OperationDescription}";
            var body = GenerateApprovalRequestEmail(workflow);

            var emailTasks = approverEmails.Select(email => 
                _emailService.SendAsync(email, subject, body, cancellationToken));

            await Task.WhenAll(emailTasks);

            _logger.LogInformation("Successfully sent approval request notifications for workflow {WorkflowId}", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval request notifications for workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }

    public async Task SendApprovalDecisionNotificationAsync(
        ApprovalWorkflow workflow,
        ApprovalStep decision,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending approval decision notification for workflow {WorkflowId}, decision: {Decision}",
            workflow.Id, decision.Decision);

        try
        {
            var subject = $"Approval Decision: {decision.Decision} - {workflow.OperationDescription}";
            var body = GenerateApprovalDecisionEmail(workflow, decision);

            // Notify the requester
            if (!string.IsNullOrEmpty(workflow.RequesterEmail))
            {
                await _emailService.SendAsync(workflow.RequesterEmail, subject, body, cancellationToken);
            }

            // Notify other approvers if the workflow is rejected
            if (decision.Decision == ApprovalDecision.Rejected)
            {
                var otherApprovers = workflow.ApprovalSteps
                    .Where(s => s.ApproverId != decision.ApproverId && !string.IsNullOrEmpty(s.ApproverEmail))
                    .Select(s => s.ApproverEmail)
                    .Distinct()
                    .ToList();

                var emailTasks = otherApprovers.Select(email => 
                    _emailService.SendAsync(email!, subject, body, cancellationToken));

                await Task.WhenAll(emailTasks);
            }

            _logger.LogInformation("Successfully sent approval decision notifications for workflow {WorkflowId}", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval decision notifications for workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }

    public async Task SendWorkflowCompletionNotificationAsync(
        ApprovalWorkflow workflow,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending workflow completion notification for workflow {WorkflowId}, status: {Status}",
            workflow.Id, workflow.Status);

        try
        {
            var subject = $"Workflow {workflow.Status}: {workflow.OperationDescription}";
            var body = GenerateWorkflowCompletionEmail(workflow);

            var emailRecipients = new List<string>();

            // Add requester
            if (!string.IsNullOrEmpty(workflow.RequesterEmail))
            {
                emailRecipients.Add(workflow.RequesterEmail);
            }

            // Add all approvers
            var approverEmails = workflow.ApprovalSteps
                .Where(s => !string.IsNullOrEmpty(s.ApproverEmail))
                .Select(s => s.ApproverEmail)
                .Distinct()
                .ToList();

            emailRecipients.AddRange(approverEmails!);

            var emailTasks = emailRecipients.Select(email => 
                _emailService.SendAsync(email, subject, body, cancellationToken));

            await Task.WhenAll(emailTasks);

            _logger.LogInformation("Successfully sent workflow completion notifications for workflow {WorkflowId}", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending workflow completion notifications for workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }

    public async Task SendEscalationNotificationAsync(
        ApprovalWorkflow workflow,
        EscalationRule escalationRule,
        List<string> escalationEmails,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending escalation notification for workflow {WorkflowId} to {RecipientCount} recipients",
            workflow.Id, escalationEmails.Count);

        try
        {
            var subject = $"ESCALATION: Pending Approval - {workflow.OperationDescription}";
            var body = GenerateEscalationEmail(workflow, escalationRule);

            var emailTasks = escalationEmails.Select(email => 
                _emailService.SendAsync(email, subject, body, cancellationToken));

            await Task.WhenAll(emailTasks);

            _logger.LogInformation("Successfully sent escalation notifications for workflow {WorkflowId}", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation notifications for workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }

    public async Task SendExpirationWarningNotificationAsync(
        ApprovalWorkflow workflow,
        TimeSpan timeUntilExpiration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending expiration warning notification for workflow {WorkflowId}, expires in {TimeUntilExpiration}",
            workflow.Id, timeUntilExpiration);

        try
        {
            var subject = $"EXPIRING SOON: Approval Required - {workflow.OperationDescription}";
            var body = GenerateExpirationWarningEmail(workflow, timeUntilExpiration);

            var emailRecipients = new List<string>();

            // Add requester
            if (!string.IsNullOrEmpty(workflow.RequesterEmail))
            {
                emailRecipients.Add(workflow.RequesterEmail);
            }

            // Add pending approvers (those who haven't submitted a decision yet)
            var pendingApprovers = workflow.ApprovalSteps
                .Where(s => s.Decision == ApprovalDecision.Pending && !string.IsNullOrEmpty(s.ApproverEmail))
                .Select(s => s.ApproverEmail)
                .Distinct()
                .ToList();

            emailRecipients.AddRange(pendingApprovers!);

            var emailTasks = emailRecipients.Select(email => 
                _emailService.SendAsync(email, subject, body, cancellationToken));

            await Task.WhenAll(emailTasks);

            _logger.LogInformation("Successfully sent expiration warning notifications for workflow {WorkflowId}", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiration warning notifications for workflow {WorkflowId}", workflow.Id);
            throw;
        }
    }

    #region Email Template Generation

    private string GenerateApprovalRequestEmail(ApprovalWorkflow workflow)
    {
        return $@"
<html>
<body>
    <h2>Approval Required</h2>
    
    <p><strong>Operation:</strong> {workflow.OperationType}</p>
    <p><strong>Description:</strong> {workflow.OperationDescription}</p>
    <p><strong>Risk Level:</strong> {workflow.RiskLevel}</p>
    <p><strong>Priority:</strong> {workflow.Priority}</p>
    
    <h3>Business Justification</h3>
    <p>{workflow.BusinessJustification}</p>
    
    <h3>Expected Impact</h3>
    <p>{workflow.ExpectedImpact}</p>
    
    <h3>Details</h3>
    <ul>
        <li><strong>Requested by:</strong> {workflow.RequesterName} ({workflow.RequesterEmail})</li>
        <li><strong>Entity Type:</strong> {workflow.EntityType}</li>
        <li><strong>Entity ID:</strong> {workflow.EntityId}</li>
        <li><strong>Required Approvals:</strong> {workflow.RequiredApprovals}</li>
        <li><strong>Expires:</strong> {workflow.ExpiresAt:yyyy-MM-dd HH:mm} UTC</li>
    </ul>
    
    <h3>Action Required</h3>
    <p>Please review this request and provide your approval decision through the administrative interface.</p>
    
    <p><em>This is an automated notification. Please do not reply to this email.</em></p>
</body>
</html>";
    }

    private string GenerateApprovalDecisionEmail(ApprovalWorkflow workflow, ApprovalStep decision)
    {
        return $@"
<html>
<body>
    <h2>Approval Decision: {decision.Decision}</h2>
    
    <p><strong>Operation:</strong> {workflow.OperationType}</p>
    <p><strong>Description:</strong> {workflow.OperationDescription}</p>
    
    <h3>Decision Details</h3>
    <ul>
        <li><strong>Decision:</strong> {decision.Decision}</li>
        <li><strong>Approver:</strong> {decision.ApproverName} ({decision.ApproverEmail})</li>
        <li><strong>Decision Date:</strong> {decision.DecisionAt:yyyy-MM-dd HH:mm} UTC</li>
    </ul>
    
    {(string.IsNullOrEmpty(decision.Comments) ? "" : $@"
    <h3>Comments</h3>
    <p>{decision.Comments}</p>")}
    
    <h3>Workflow Status</h3>
    <p><strong>Current Status:</strong> {workflow.Status}</p>
    <p><strong>Approvals Received:</strong> {workflow.CurrentApprovals} of {workflow.RequiredApprovals}</p>
    
    {(workflow.Status == ApprovalStatus.Approved ? 
        "<p><strong>Next Step:</strong> The approved operation will be executed shortly.</p>" :
        workflow.Status == ApprovalStatus.Rejected ?
        "<p><strong>Result:</strong> The operation has been rejected and will not be executed.</p>" :
        "<p><strong>Next Step:</strong> Waiting for additional approvals.</p>")}
    
    <p><em>This is an automated notification. Please do not reply to this email.</em></p>
</body>
</html>";
    }

    private string GenerateWorkflowCompletionEmail(ApprovalWorkflow workflow)
    {
        return $@"
<html>
<body>
    <h2>Workflow Complete: {workflow.Status}</h2>
    
    <p><strong>Operation:</strong> {workflow.OperationType}</p>
    <p><strong>Description:</strong> {workflow.OperationDescription}</p>
    
    <h3>Final Status</h3>
    <p><strong>Status:</strong> {workflow.Status}</p>
    <p><strong>Completed:</strong> {workflow.CompletedAt:yyyy-MM-dd HH:mm} UTC</p>
    
    {(string.IsNullOrEmpty(workflow.CompletionReason) ? "" : $@"
    <h3>Completion Reason</h3>
    <p>{workflow.CompletionReason}</p>")}
    
    <h3>Approval Summary</h3>
    <p><strong>Total Approvals:</strong> {workflow.CurrentApprovals} of {workflow.RequiredApprovals}</p>
    
    <table border='1' style='border-collapse: collapse; width: 100%;'>
        <tr>
            <th>Approver</th>
            <th>Decision</th>
            <th>Date</th>
            <th>Comments</th>
        </tr>
        {string.Join("", workflow.ApprovalSteps.Select(step => $@"
        <tr>
            <td>{step.ApproverName}</td>
            <td>{step.Decision}</td>
            <td>{step.DecisionAt:yyyy-MM-dd HH:mm}</td>
            <td>{step.Comments}</td>
        </tr>"))}
    </table>
    
    <p><em>This is an automated notification. Please do not reply to this email.</em></p>
</body>
</html>";
    }

    private string GenerateEscalationEmail(ApprovalWorkflow workflow, EscalationRule escalationRule)
    {
        var workflowAge = DateTime.UtcNow - workflow.CreatedAt;
        
        return $@"
<html>
<body>
    <h2>🚨 ESCALATION: Pending Approval</h2>
    
    <p><strong>Operation:</strong> {workflow.OperationType}</p>
    <p><strong>Description:</strong> {workflow.OperationDescription}</p>
    <p><strong>Risk Level:</strong> {workflow.RiskLevel}</p>
    <p><strong>Priority:</strong> {workflow.Priority}</p>
    
    <h3>⏰ Time-Sensitive Alert</h3>
    <ul>
        <li><strong>Pending Duration:</strong> {workflowAge.TotalHours:F1} hours</li>
        <li><strong>Expires:</strong> {workflow.ExpiresAt:yyyy-MM-dd HH:mm} UTC</li>
        <li><strong>Escalation Trigger:</strong> {escalationRule.TriggerAfter.TotalHours} hours</li>
    </ul>
    
    <h3>Business Impact</h3>
    <p><strong>Justification:</strong> {workflow.BusinessJustification}</p>
    <p><strong>Expected Impact:</strong> {workflow.ExpectedImpact}</p>
    
    <h3>Approval Status</h3>
    <p><strong>Current Approvals:</strong> {workflow.CurrentApprovals} of {workflow.RequiredApprovals}</p>
    <p><strong>Requested by:</strong> {workflow.RequesterName} ({workflow.RequesterEmail})</p>
    
    <h3>🔴 Action Required</h3>
    <p>{escalationRule.EscalationMessage}</p>
    <p>Please review this high-priority approval request immediately.</p>
    
    <p><em>This is an automated escalation notification. Please do not reply to this email.</em></p>
</body>
</html>";
    }

    private string GenerateExpirationWarningEmail(ApprovalWorkflow workflow, TimeSpan timeUntilExpiration)
    {
        return $@"
<html>
<body>
    <h2>⚠️ Approval Expiring Soon</h2>
    
    <p><strong>Operation:</strong> {workflow.OperationType}</p>
    <p><strong>Description:</strong> {workflow.OperationDescription}</p>
    
    <h3>⏰ Expiration Warning</h3>
    <ul>
        <li><strong>Time Remaining:</strong> {timeUntilExpiration.TotalHours:F1} hours</li>
        <li><strong>Expires:</strong> {workflow.ExpiresAt:yyyy-MM-dd HH:mm} UTC</li>
        <li><strong>Current Status:</strong> {workflow.Status}</li>
    </ul>
    
    <h3>Approval Progress</h3>
    <p><strong>Approvals Received:</strong> {workflow.CurrentApprovals} of {workflow.RequiredApprovals}</p>
    
    <h3>⚡ Urgent Action Required</h3>
    <p>This approval request will expire soon. If you need to provide a decision, please do so immediately through the administrative interface.</p>
    
    <p><strong>Business Justification:</strong> {workflow.BusinessJustification}</p>
    
    <p><em>This is an automated expiration warning. Please do not reply to this email.</em></p>
</body>
</html>";
    }

    #endregion
}
