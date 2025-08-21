using Event.Domain.Models;
using Event.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Event.API.Controllers;

/// <summary>
/// Controller for managing approval workflow templates
/// </summary>
[ApiController]
[Route("api/v1/approval-templates")]
[Produces("application/json")]
//[Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
public class ApprovalTemplateController : ControllerBase
{
    private readonly IApprovalWorkflowService _approvalService;
    private readonly ILogger<ApprovalTemplateController> _logger;

    public ApprovalTemplateController(
        IApprovalWorkflowService approvalService,
        ILogger<ApprovalTemplateController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Create or update an approval workflow template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApprovalWorkflowTemplate), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ApprovalWorkflowTemplate), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<ApprovalWorkflowTemplate>> UpsertTemplate(
        [FromBody] UpsertApprovalTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upserting approval workflow template for operation type {OperationType}",
            request.OperationType);

        try
        {
            var organizationId = GetCurrentOrganizationId();
            var template = MapToTemplate(request, organizationId);

            var result = await _approvalService.UpsertWorkflowTemplateAsync(template, cancellationToken);

            var statusCode = request.Id.HasValue ? HttpStatusCode.OK : HttpStatusCode.Created;
            return StatusCode((int)statusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting approval workflow template");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while saving the approval workflow template",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get approval workflow template by operation type
    /// </summary>
    [HttpGet("by-operation/{operationType}")]
    [ProducesResponseType(typeof(ApprovalWorkflowTemplate), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ApprovalWorkflowTemplate>> GetTemplateByOperationType(
        ApprovalOperationType operationType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflow template for operation type {OperationType}", operationType);

        var organizationId = GetCurrentOrganizationId();
        var template = await _approvalService.GetWorkflowTemplateAsync(operationType, organizationId, cancellationToken);

        if (template == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Template not found",
                Detail = $"Approval workflow template for operation type {operationType} was not found",
                Status = (int)HttpStatusCode.NotFound
            });
        }

        return Ok(template);
    }

    /// <summary>
    /// Get all available operation types that can have approval workflows
    /// </summary>
    [HttpGet("operation-types")]
    [ProducesResponseType(typeof(List<OperationTypeInfo>), (int)HttpStatusCode.OK)]
    public ActionResult<List<OperationTypeInfo>> GetOperationTypes()
    {
        _logger.LogInformation("Getting available operation types for approval workflows");

        var operationTypes = Enum.GetValues<ApprovalOperationType>()
            .Select(ot => new OperationTypeInfo
            {
                Type = ot,
                Name = ot.ToString(),
                Description = GetOperationTypeDescription(ot),
                DefaultRiskLevel = GetDefaultRiskLevel(ot),
                DefaultRequiredApprovals = GetDefaultRequiredApprovals(ot)
            })
            .ToList();

        return Ok(operationTypes);
    }

    /// <summary>
    /// Create default approval templates for common operations
    /// </summary>
    [HttpPost("create-defaults")]
    [ProducesResponseType(typeof(List<ApprovalWorkflowTemplate>), (int)HttpStatusCode.Created)]
    public async Task<ActionResult<List<ApprovalWorkflowTemplate>>> CreateDefaultTemplates(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating default approval workflow templates");

        try
        {
            var organizationId = GetCurrentOrganizationId();
            var defaultTemplates = CreateDefaultTemplatesForOrganization(organizationId);
            var createdTemplates = new List<ApprovalWorkflowTemplate>();

            foreach (var template in defaultTemplates)
            {
                var result = await _approvalService.UpsertWorkflowTemplateAsync(template, cancellationToken);
                createdTemplates.Add(result);
            }

            _logger.LogInformation("Created {Count} default approval workflow templates", createdTemplates.Count);
            return StatusCode((int)HttpStatusCode.Created, createdTemplates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default approval workflow templates");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while creating default templates",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Test approval workflow template configuration
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TemplateTestResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<TemplateTestResult>> TestTemplate(
        [FromBody] TestTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing approval workflow template configuration");

        try
        {
            var organizationId = GetCurrentOrganizationId();
            var template = MapToTemplate(request.Template, organizationId);

            // Validate template configuration
            var validationResult = ValidateTemplate(template);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid template configuration",
                    Detail = string.Join(", ", validationResult.Errors),
                    Status = (int)HttpStatusCode.BadRequest
                });
            }

            // Test auto-approval conditions if present
            var autoApprovalTest = TestAutoApprovalConditions(template, request.TestData);

            var result = new TemplateTestResult
            {
                IsValid = true,
                RequiredApprovals = template.RequiredApprovals,
                ExpirationTime = template.DefaultExpirationTime,
                WouldAutoApprove = autoApprovalTest.WouldAutoApprove,
                AutoApprovalReason = autoApprovalTest.Reason,
                ValidationMessages = validationResult.Messages
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing approval workflow template");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while testing the template",
                Status = 500
            });
        }
    }

    #region Private Helper Methods

    private Guid GetCurrentOrganizationId()
    {
        // In a real implementation, this would extract the organization ID from the JWT token or user context
        var orgIdClaim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.NewGuid(); // Default for demo
    }

    private ApprovalWorkflowTemplate MapToTemplate(UpsertApprovalTemplateRequest request, Guid organizationId)
    {
        return new ApprovalWorkflowTemplate
        {
            Id = request.Id ?? Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name,
            Description = request.Description,
            OperationType = request.OperationType,
            RequiredApprovals = request.RequiredApprovals,
            RequiredRoles = request.RequiredRoles,
            DefaultRiskLevel = request.DefaultRiskLevel,
            DefaultExpirationTime = TimeSpan.FromDays(request.DefaultExpirationDays),
            IsActive = request.IsActive,
            UpdatedAt = DateTime.UtcNow,
            AutoApprovalConditions = request.AutoApprovalConditions,
            EscalationRules = request.EscalationRules
        };
    }

    private string GetOperationTypeDescription(ApprovalOperationType operationType)
    {
        return operationType switch
        {
            ApprovalOperationType.EventPublish => "Publishing an event to make it visible to the public",
            ApprovalOperationType.EventCancel => "Cancelling a published event",
            ApprovalOperationType.EventPriceChange => "Changing ticket prices for an event",
            ApprovalOperationType.EventDateChange => "Changing the date or time of an event",
            ApprovalOperationType.EventCapacityIncrease => "Increasing the capacity of an event",
            ApprovalOperationType.EventCapacityDecrease => "Decreasing the capacity of an event",
            ApprovalOperationType.VenueModification => "Modifying venue details or configuration",
            ApprovalOperationType.SeatMapImport => "Importing a new seat map for a venue",
            ApprovalOperationType.SeatMapBulkOperation => "Performing bulk operations on seats",
            ApprovalOperationType.PricingRuleCreation => "Creating new pricing rules or discount codes",
            ApprovalOperationType.PricingRuleModification => "Modifying existing pricing rules",
            ApprovalOperationType.BulkRefund => "Processing bulk refunds for multiple tickets",
            ApprovalOperationType.EventArchive => "Archiving completed or cancelled events",
            ApprovalOperationType.VenueDeactivation => "Deactivating a venue",
            ApprovalOperationType.TicketTypeCreation => "Creating new ticket types",
            ApprovalOperationType.TicketTypeModification => "Modifying existing ticket types",
            ApprovalOperationType.ReservationOverride => "Overriding reservation limits or constraints",
            ApprovalOperationType.AdminOverride => "Administrative override of business rules",
            ApprovalOperationType.DataExport => "Exporting sensitive customer or business data",
            ApprovalOperationType.SecurityRoleChange => "Changing user security roles or permissions",
            _ => "Unknown operation type"
        };
    }

    private RiskLevel GetDefaultRiskLevel(ApprovalOperationType operationType)
    {
        return operationType switch
        {
            ApprovalOperationType.EventPublish => RiskLevel.Medium,
            ApprovalOperationType.EventCancel => RiskLevel.High,
            ApprovalOperationType.EventPriceChange => RiskLevel.Medium,
            ApprovalOperationType.EventDateChange => RiskLevel.High,
            ApprovalOperationType.EventCapacityIncrease => RiskLevel.Low,
            ApprovalOperationType.EventCapacityDecrease => RiskLevel.Medium,
            ApprovalOperationType.VenueModification => RiskLevel.Medium,
            ApprovalOperationType.SeatMapImport => RiskLevel.Medium,
            ApprovalOperationType.SeatMapBulkOperation => RiskLevel.Medium,
            ApprovalOperationType.PricingRuleCreation => RiskLevel.Low,
            ApprovalOperationType.PricingRuleModification => RiskLevel.Medium,
            ApprovalOperationType.BulkRefund => RiskLevel.High,
            ApprovalOperationType.EventArchive => RiskLevel.Low,
            ApprovalOperationType.VenueDeactivation => RiskLevel.High,
            ApprovalOperationType.TicketTypeCreation => RiskLevel.Low,
            ApprovalOperationType.TicketTypeModification => RiskLevel.Medium,
            ApprovalOperationType.ReservationOverride => RiskLevel.High,
            ApprovalOperationType.AdminOverride => RiskLevel.Critical,
            ApprovalOperationType.DataExport => RiskLevel.High,
            ApprovalOperationType.SecurityRoleChange => RiskLevel.Critical,
            _ => RiskLevel.Medium
        };
    }

    private int GetDefaultRequiredApprovals(ApprovalOperationType operationType)
    {
        return operationType switch
        {
            ApprovalOperationType.EventPublish => 1,
            ApprovalOperationType.EventCancel => 2,
            ApprovalOperationType.EventPriceChange => 1,
            ApprovalOperationType.EventDateChange => 2,
            ApprovalOperationType.EventCapacityIncrease => 1,
            ApprovalOperationType.EventCapacityDecrease => 1,
            ApprovalOperationType.VenueModification => 1,
            ApprovalOperationType.SeatMapImport => 1,
            ApprovalOperationType.SeatMapBulkOperation => 1,
            ApprovalOperationType.PricingRuleCreation => 1,
            ApprovalOperationType.PricingRuleModification => 1,
            ApprovalOperationType.BulkRefund => 2,
            ApprovalOperationType.EventArchive => 1,
            ApprovalOperationType.VenueDeactivation => 2,
            ApprovalOperationType.TicketTypeCreation => 1,
            ApprovalOperationType.TicketTypeModification => 1,
            ApprovalOperationType.ReservationOverride => 2,
            ApprovalOperationType.AdminOverride => 3,
            ApprovalOperationType.DataExport => 2,
            ApprovalOperationType.SecurityRoleChange => 3,
            _ => 2
        };
    }

    private List<ApprovalWorkflowTemplate> CreateDefaultTemplatesForOrganization(Guid organizationId)
    {
        var templates = new List<ApprovalWorkflowTemplate>();

        // High-priority operations that commonly need approval
        var highPriorityOperations = new[]
        {
            ApprovalOperationType.EventPublish,
            ApprovalOperationType.EventCancel,
            ApprovalOperationType.EventPriceChange,
            ApprovalOperationType.SeatMapImport,
            ApprovalOperationType.BulkRefund
        };

        foreach (var operationType in highPriorityOperations)
        {
            templates.Add(new ApprovalWorkflowTemplate
            {
                OrganizationId = organizationId,
                Name = $"Default {operationType} Approval",
                Description = GetOperationTypeDescription(operationType),
                OperationType = operationType,
                RequiredApprovals = GetDefaultRequiredApprovals(operationType),
                RequiredRoles = new List<string> { "Manager", "Admin" },
                DefaultRiskLevel = GetDefaultRiskLevel(operationType),
                DefaultExpirationTime = TimeSpan.FromDays(7),
                IsActive = true
            });
        }

        return templates;
    }

    private TemplateValidationResult ValidateTemplate(ApprovalWorkflowTemplate template)
    {
        var result = new TemplateValidationResult { IsValid = true };

        if (template.RequiredApprovals < 1)
        {
            result.IsValid = false;
            result.Errors.Add("Required approvals must be at least 1");
        }

        if (template.RequiredApprovals > 10)
        {
            result.Warnings.Add("Required approvals greater than 10 may cause significant delays");
        }

        if (template.DefaultExpirationTime < TimeSpan.FromHours(1))
        {
            result.IsValid = false;
            result.Errors.Add("Expiration time must be at least 1 hour");
        }

        if (template.DefaultExpirationTime > TimeSpan.FromDays(30))
        {
            result.Warnings.Add("Expiration time greater than 30 days may result in stale approvals");
        }

        if (string.IsNullOrEmpty(template.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Template name is required");
        }

        if (!template.RequiredRoles.Any())
        {
            result.Warnings.Add("No required roles specified - any user may approve");
        }

        return result;
    }

    private AutoApprovalTestResult TestAutoApprovalConditions(ApprovalWorkflowTemplate template, object? testData)
    {
        if (template.AutoApprovalConditions == null)
        {
            return new AutoApprovalTestResult
            {
                WouldAutoApprove = false,
                Reason = "No auto-approval conditions configured"
            };
        }

        // Simple test - in a real implementation, this would check the actual conditions
        if (template.DefaultRiskLevel == RiskLevel.Low)
        {
            return new AutoApprovalTestResult
            {
                WouldAutoApprove = true,
                Reason = "Low risk operation with auto-approval enabled"
            };
        }

        return new AutoApprovalTestResult
        {
            WouldAutoApprove = false,
            Reason = "Risk level too high for auto-approval"
        };
    }

    #endregion
}

#region Request/Response Models

/// <summary>
/// Request model for creating or updating approval templates
/// </summary>
public class UpsertApprovalTemplateRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ApprovalOperationType OperationType { get; set; }
    public int RequiredApprovals { get; set; } = 2;
    public List<string> RequiredRoles { get; set; } = new();
    public RiskLevel DefaultRiskLevel { get; set; } = RiskLevel.Medium;
    public int DefaultExpirationDays { get; set; } = 7;
    public bool IsActive { get; set; } = true;
    public AutoApprovalConditions? AutoApprovalConditions { get; set; }
    public List<EscalationRule> EscalationRules { get; set; } = new();
}

/// <summary>
/// Information about operation types
/// </summary>
public class OperationTypeInfo
{
    public ApprovalOperationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel DefaultRiskLevel { get; set; }
    public int DefaultRequiredApprovals { get; set; }
}

/// <summary>
/// Request model for testing templates
/// </summary>
public class TestTemplateRequest
{
    public UpsertApprovalTemplateRequest Template { get; set; } = new();
    public object? TestData { get; set; }
}

/// <summary>
/// Result of template test
/// </summary>
public class TemplateTestResult
{
    public bool IsValid { get; set; }
    public int RequiredApprovals { get; set; }
    public TimeSpan ExpirationTime { get; set; }
    public bool WouldAutoApprove { get; set; }
    public string AutoApprovalReason { get; set; } = string.Empty;
    public List<string> ValidationMessages { get; set; } = new();
}

/// <summary>
/// Result of template validation
/// </summary>
public class TemplateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Result of auto-approval test
/// </summary>
public class AutoApprovalTestResult
{
    public bool WouldAutoApprove { get; set; }
    public string Reason { get; set; } = string.Empty;
}

#endregion
