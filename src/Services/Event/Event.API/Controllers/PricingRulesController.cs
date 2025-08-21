using Event.Application.Common.Models;
using Event.Application.Features.PricingRules.Commands.CreatePricingRule;
using Event.Application.Features.PricingRules.Commands.UpdatePricingRule;
using Event.Application.Features.PricingRules.Commands.DeletePricingRule;
using Event.Application.Features.PricingRules.Queries.GetEventPricingRules;
using Event.Application.Features.PricingRules.Queries.TestPricingRule;
using Event.Application.Features.PricingRules.Queries.GetPricingRuleUsage;
using UpdatePricingRuleRequest = Event.Application.Features.PricingRules.Commands.UpdatePricingRule.UpdatePricingRuleRequest;
using Event.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Pricing Rules management API controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class PricingRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PricingRulesController> _logger;

    public PricingRulesController(IMediator mediator, ILogger<PricingRulesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new pricing rule for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="request">Pricing rule creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created pricing rule</returns>
    [HttpPost("events/{eventId:guid}/pricing-rules")]
    [ProducesResponseType(typeof(PricingRuleDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<PricingRuleDto>> CreatePricingRule(
        [FromRoute] Guid eventId,
        [FromBody] CreatePricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating pricing rule: {RuleName} for event {EventId}", 
            request.Name, eventId);

        var command = CreatePricingRuleCommand.FromRequest(eventId, request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetPricingRule),
            new { pricingRuleId = result.Id, version = "1.0" },
            result);
    }

    /// <summary>
    /// Get pricing rule by ID
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing rule details</returns>
    [HttpGet("{pricingRuleId:guid}")]
    [ProducesResponseType(typeof(PricingRuleDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PricingRuleDto>> GetPricingRule(
        [FromRoute] Guid pricingRuleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pricing rule {PricingRuleId}", pricingRuleId);

        // TODO: Implement GetPricingRuleQuery
        // var query = new GetPricingRuleQuery(pricingRuleId);
        // var result = await _mediator.Send(query, cancellationToken);

        // Placeholder implementation
        return NotFound($"Pricing rule with ID '{pricingRuleId}' not found");
    }

    /// <summary>
    /// Get pricing rules for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="includeInactive">Include inactive pricing rules</param>
    /// <param name="type">Filter by pricing rule type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pricing rules</returns>
    [HttpGet("events/{eventId:guid}/pricing-rules")]
    [ProducesResponseType(typeof(IEnumerable<PricingRuleDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IEnumerable<PricingRuleDto>>> GetEventPricingRules(
        [FromRoute] Guid eventId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pricing rules for event {EventId}, Type: {Type}", eventId, type);

        // Convert string to PricingRuleType enum
        PricingRuleType? pricingRuleType = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<PricingRuleType>(type, true, out var parsedType))
        {
            pricingRuleType = parsedType;
        }

        var query = new GetEventPricingRulesQuery
        {
            EventId = eventId,
            IncludeInactive = includeInactive,
            Type = pricingRuleType
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update pricing rule
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="request">Update pricing rule request</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated pricing rule</returns>
    [HttpPut("{pricingRuleId:guid}")]
    [ProducesResponseType(typeof(PricingRuleDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<PricingRuleDto>> UpdatePricingRule(
        [FromRoute] Guid pricingRuleId,
        [FromBody] UpdatePricingRuleRequest request,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating pricing rule {PricingRuleId} with expected version {ExpectedVersion}", 
            pricingRuleId, expectedVersion);

        var command = UpdatePricingRuleCommand.FromRequest(pricingRuleId, request, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        // Set ETag header for optimistic concurrency control (simplified)
        Response.Headers.Add("ETag", result.UpdatedAt?.Ticks.ToString() ?? DateTime.UtcNow.Ticks.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Delete pricing rule
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{pricingRuleId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult> DeletePricingRule(
        [FromRoute] Guid pricingRuleId,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting pricing rule {PricingRuleId}", pricingRuleId);

        var command = new DeletePricingRuleCommand(pricingRuleId, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        if (result)
        {
            return NoContent();
        }
        else
        {
            return NotFound($"Pricing rule with ID '{pricingRuleId}' not found");
        }
    }

    /// <summary>
    /// Activate pricing rule
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated pricing rule</returns>
    [HttpPost("{pricingRuleId:guid}/activate")]
    [ProducesResponseType(typeof(PricingRuleDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PricingRuleDto>> ActivatePricingRule(
        [FromRoute] Guid pricingRuleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating pricing rule {PricingRuleId}", pricingRuleId);

        // TODO: Implement ActivatePricingRuleCommand
        // var command = new ActivatePricingRuleCommand(pricingRuleId);
        // var result = await _mediator.Send(command, cancellationToken);

        // Placeholder implementation
        return BadRequest("Activate pricing rule not yet implemented");
    }

    /// <summary>
    /// Deactivate pricing rule
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated pricing rule</returns>
    [HttpPost("{pricingRuleId:guid}/deactivate")]
    [ProducesResponseType(typeof(PricingRuleDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PricingRuleDto>> DeactivatePricingRule(
        [FromRoute] Guid pricingRuleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating pricing rule {PricingRuleId}", pricingRuleId);

        // TODO: Implement DeactivatePricingRuleCommand
        // var command = new DeactivatePricingRuleCommand(pricingRuleId);
        // var result = await _mediator.Send(command, cancellationToken);

        // Placeholder implementation
        return BadRequest("Deactivate pricing rule not yet implemented");
    }

    /// <summary>
    /// Test pricing rule against a sample order
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="request">Test pricing rule request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing calculation result</returns>
    [HttpPost("{pricingRuleId:guid}/test")]
    [ProducesResponseType(typeof(PricingCalculationDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PricingCalculationDto>> TestPricingRule(
        [FromRoute] Guid pricingRuleId,
        [FromBody] TestPricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing pricing rule {PricingRuleId}", pricingRuleId);

        // Map the order items to the correct type
        var mappedOrderItems = request.OrderItems.Select(item => new Event.Application.Features.PricingRules.Queries.TestPricingRule.TestOrderItemDto
        {
            TicketTypeId = item.TicketTypeId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        var query = new TestPricingRuleQuery
        {
            PricingRuleId = pricingRuleId,
            OrderItems = mappedOrderItems,
            CustomerSegment = request.CustomerSegment,
            DiscountCode = request.DiscountCode
        };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get pricing rule usage statistics
    /// </summary>
    /// <param name="pricingRuleId">Pricing rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("{pricingRuleId:guid}/usage")]
    [ProducesResponseType(typeof(PricingRuleUsageDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PricingRuleUsageDto>> GetPricingRuleUsage(
        [FromRoute] Guid pricingRuleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting usage statistics for pricing rule {PricingRuleId}", pricingRuleId);

        var query = new GetPricingRuleUsageQuery
        {
            PricingRuleId = pricingRuleId
        };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}

/// <summary>
/// Test pricing rule request
/// </summary>
public record TestPricingRuleRequest
{
    public List<TestOrderItemDto> OrderItems { get; init; } = new();
    public string? CustomerSegment { get; init; }
    public string? DiscountCode { get; init; }
}

/// <summary>
/// Test order item DTO
/// </summary>
public record TestOrderItemDto
{
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
}

/// <summary>
/// Pricing calculation DTO
/// </summary>
public record PricingCalculationDto
{
    public MoneyDto OriginalAmount { get; init; } = null!;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto FinalAmount { get; init; } = null!;
    public bool RuleApplied { get; init; }
    public string? RuleAppliedReason { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Pricing rule usage DTO
/// </summary>
public record PricingRuleUsageDto
{
    public Guid PricingRuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public int TotalUses { get; init; }
    public int MaxUses { get; init; }
    public int RemainingUses { get; init; }
    public MoneyDto TotalDiscountGiven { get; init; } = null!;
    public DateTime? FirstUsed { get; init; }
    public DateTime? LastUsed { get; init; }
    public List<PricingRuleUsageDetailDto> RecentUsage { get; init; } = new();
}

/// <summary>
/// Pricing rule usage detail DTO
/// </summary>
public record PricingRuleUsageDetailDto
{
    public DateTime UsedAt { get; init; }
    public Guid UserId { get; init; }
    public Guid ReservationId { get; init; }
    public MoneyDto DiscountAmount { get; init; } = null!;
}
