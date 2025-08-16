using Event.Domain.Models;
using Event.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Event.API.Controllers;

/// <summary>
/// Controller for managing approval workflows
/// </summary>
[ApiController]
[Route("api/v1/approvals")]
[Produces("application/json")]
//[Authorize] // Uncomment when authentication is implemented
public class ApprovalController : ControllerBase
{
    private readonly IApprovalWorkflowService _approvalService;
    private readonly IApprovalOperationExecutor _operationExecutor;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        IApprovalWorkflowService approvalService,
        IApprovalOperationExecutor operationExecutor,
        ILogger<ApprovalController> logger)
    {
        _approvalService = approvalService;
        _operationExecutor = operationExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Create a new approval workflow
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApprovalWorkflowResult), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<ApprovalWorkflowResult>> CreateWorkflow(
        [FromBody] CreateApprovalWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating approval workflow for operation {OperationType} on {EntityType}:{EntityId}",
            request.OperationType, request.EntityType, request.EntityId);

        try
        {
            var userId = GetCurrentUserId();
            var organizationId = GetCurrentOrganizationId();

            var result = await _approvalService.CreateWorkflowAsync(
                request, userId, organizationId, cancellationToken);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetWorkflow), new { id = result.WorkflowId }, result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to create approval workflow",
                Detail = string.Join(", ", result.Errors),
                Status = (int)HttpStatusCode.BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval workflow");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while creating the approval workflow",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get approval workflow by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApprovalWorkflow), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ApprovalWorkflow>> GetWorkflow(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflow {WorkflowId}", id);

        var workflow = await _approvalService.GetWorkflowAsync(id, cancellationToken);
        if (workflow == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Workflow not found",
                Detail = $"Approval workflow with ID {id} was not found",
                Status = (int)HttpStatusCode.NotFound
            });
        }

        return Ok(workflow);
    }

    /// <summary>
    /// Get approval workflows with filtering and paging
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApprovalWorkflow>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PagedResult<ApprovalWorkflow>>> GetWorkflows(
        [FromQuery] ApprovalWorkflowFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflows with filters");

        var organizationId = GetCurrentOrganizationId();
        var filter = MapToFilter(request);

        var result = await _approvalService.GetWorkflowsAsync(
            organizationId, filter, request.PageNumber, request.PageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get pending approval workflows for current user
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ApprovalWorkflow>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<ApprovalWorkflow>>> GetPendingApprovals(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pending approvals for current user");

        var userId = GetCurrentUserId();
        var organizationId = GetCurrentOrganizationId();

        var workflows = await _approvalService.GetPendingApprovalsAsync(
            userId, organizationId, cancellationToken);

        return Ok(workflows);
    }

    /// <summary>
    /// Submit an approval decision
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApprovalWorkflowResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ApprovalWorkflowResult>> SubmitApproval(
        Guid id,
        [FromBody] ApprovalDecisionRequest decision,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting approval decision {Decision} for workflow {WorkflowId}",
            decision.Decision, id);

        try
        {
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();
            var userEmail = GetCurrentUserEmail();
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var result = await _approvalService.SubmitApprovalAsync(
                id, decision, userId, userName, userEmail, ipAddress, userAgent, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to submit approval decision",
                Detail = string.Join(", ", result.Errors),
                Status = (int)HttpStatusCode.BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting approval decision for workflow {WorkflowId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while submitting the approval decision",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Execute an approved workflow
    /// </summary>
    [HttpPost("{id:guid}/execute")]
    [ProducesResponseType(typeof(ApprovalWorkflowResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ApprovalWorkflowResult>> ExecuteWorkflow(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing approved workflow {WorkflowId}", id);

        try
        {
            var userId = GetCurrentUserId();

            var result = await _approvalService.ExecuteApprovedWorkflowAsync(
                id, userId, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to execute workflow",
                Detail = string.Join(", ", result.Errors),
                Status = (int)HttpStatusCode.BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while executing the workflow",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Cancel a pending workflow
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApprovalWorkflowResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ApprovalWorkflowResult>> CancelWorkflow(
        Guid id,
        [FromBody] CancelWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling workflow {WorkflowId}", id);

        try
        {
            var userId = GetCurrentUserId();

            var result = await _approvalService.CancelWorkflowAsync(
                id, userId, request.Reason, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Failed to cancel workflow",
                Detail = string.Join(", ", result.Errors),
                Status = (int)HttpStatusCode.BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow {WorkflowId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while cancelling the workflow",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get workflow audit history
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(List<ApprovalAuditLog>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<List<ApprovalAuditLog>>> GetWorkflowAuditHistory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit history for workflow {WorkflowId}", id);

        var auditHistory = await _approvalService.GetWorkflowAuditHistoryAsync(id, cancellationToken);
        return Ok(auditHistory);
    }

    /// <summary>
    /// Get approval workflow statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApprovalWorkflowStatistics), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<ApprovalWorkflowStatistics>> GetStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflow statistics");

        var organizationId = GetCurrentOrganizationId();
        var statistics = await _approvalService.GetWorkflowStatisticsAsync(
            organizationId, fromDate, toDate, cancellationToken);

        return Ok(statistics);
    }

    /// <summary>
    /// Check if operation requires approval
    /// </summary>
    [HttpPost("check-requirement")]
    [ProducesResponseType(typeof(RequiresApprovalResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<RequiresApprovalResponse>> CheckApprovalRequirement(
        [FromBody] CheckApprovalRequirementRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking approval requirement for operation {OperationType}", request.OperationType);

        var organizationId = GetCurrentOrganizationId();
        var requiresApproval = await _approvalService.RequiresApprovalAsync(
            request.OperationType, organizationId, request.OperationData, cancellationToken);

        ApprovalWorkflowTemplate? template = null;
        if (requiresApproval)
        {
            template = await _approvalService.GetWorkflowTemplateAsync(
                request.OperationType, organizationId, cancellationToken);
        }

        return Ok(new RequiresApprovalResponse
        {
            RequiresApproval = requiresApproval,
            Template = template
        });
    }

    /// <summary>
    /// Validate operation before submission
    /// </summary>
    [HttpPost("validate-operation")]
    [ProducesResponseType(typeof(OperationValidationResult), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<OperationValidationResult>> ValidateOperation(
        [FromBody] ValidateOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating operation {OperationType} on {EntityType}:{EntityId}",
            request.OperationType, request.EntityType, request.EntityId);

        var result = await _operationExecutor.ValidateOperationAsync(
            request.OperationType, request.EntityType, request.EntityId, 
            request.OperationData, cancellationToken);

        return Ok(result);
    }

    #region Private Helper Methods

    private Guid GetCurrentUserId()
    {
        // In a real implementation, this would extract the user ID from the JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private Guid GetCurrentOrganizationId()
    {
        // In a real implementation, this would extract the organization ID from the JWT token or user context
        var orgIdClaim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.NewGuid(); // Default for demo
    }

    private string GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
    }

    private string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    private ApprovalWorkflowFilter MapToFilter(ApprovalWorkflowFilterRequest request)
    {
        return new ApprovalWorkflowFilter
        {
            Status = request.Status,
            OperationType = request.OperationType,
            RiskLevel = request.RiskLevel,
            Priority = request.Priority,
            RequesterId = request.RequesterId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CreatedAfter = request.CreatedAfter,
            CreatedBefore = request.CreatedBefore,
            ExpiresAfter = request.ExpiresAfter,
            ExpiresBefore = request.ExpiresBefore,
            Tags = request.Tags ?? new List<string>(),
            SearchTerm = request.SearchTerm
        };
    }

    #endregion
}

#region Request/Response Models

/// <summary>
/// Request model for filtering approval workflows
/// </summary>
public class ApprovalWorkflowFilterRequest
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
    public List<string>? Tags { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request model for cancelling a workflow
/// </summary>
public class CancelWorkflowRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for checking approval requirement
/// </summary>
public class CheckApprovalRequirementRequest
{
    public ApprovalOperationType OperationType { get; set; }
    public object? OperationData { get; set; }
}

/// <summary>
/// Response model for approval requirement check
/// </summary>
public class RequiresApprovalResponse
{
    public bool RequiresApproval { get; set; }
    public ApprovalWorkflowTemplate? Template { get; set; }
}

/// <summary>
/// Request model for validating operations
/// </summary>
public class ValidateOperationRequest
{
    public ApprovalOperationType OperationType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public object OperationData { get; set; } = new();
}

#endregion
