using Event.Application.Common.Models;
using Event.Application.Features.Events.Commands.CancelEvent;
using Event.Application.Features.Events.Commands.CreateEvent;
using Event.Application.Features.Events.Commands.PublishEvent;
using Event.Application.Features.Events.Commands.UpdateEvent;
using Event.Application.Features.Events.Queries.GetEvent;
using Event.Application.Features.Events.Queries.SearchEvents;
using Event.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Events management API controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IMediator mediator, ILogger<EventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <param name="request">Event creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating event with slug: {Slug}", request.Slug);

        var command = CreateEventCommand.FromRequest(request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetEvent),
            new { eventId = result.Id, version = "1.0" },
            result);
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="includeTicketTypes">Include ticket types</param>
    /// <param name="includePricingRules">Include pricing rules</param>
    /// <param name="includeAllocations">Include allocations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event details</returns>
    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(
        [FromRoute] Guid eventId,
        [FromQuery] bool includeTicketTypes = true,
        [FromQuery] bool includePricingRules = true,
        [FromQuery] bool includeAllocations = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting event {EventId}", eventId);

        var query = new GetEventQuery(eventId, includeTicketTypes, includePricingRules, includeAllocations);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Event with ID '{eventId}' not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get event by slug
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="slug">Event slug</param>
    /// <param name="includeTicketTypes">Include ticket types</param>
    /// <param name="includePricingRules">Include pricing rules</param>
    /// <param name="includeAllocations">Include allocations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event details</returns>
    [HttpGet("by-slug/{organizationId:guid}/{slug}")]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventDto>> GetEventBySlug(
        [FromRoute] Guid organizationId,
        [FromRoute] string slug,
        [FromQuery] bool includeTicketTypes = true,
        [FromQuery] bool includePricingRules = true,
        [FromQuery] bool includeAllocations = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting event by slug {Slug} for organization {OrganizationId}", slug, organizationId);

        var query = new GetEventBySlugQuery(slug, organizationId, includeTicketTypes, includePricingRules, includeAllocations);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Event with slug '{slug}' not found for organization '{organizationId}'");
        }

        return Ok(result);
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="request">Event update request</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event</returns>
    [HttpPut("{eventId:guid}")]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventDto>> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] UpdateEventRequest request,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating event {EventId} with expected version {ExpectedVersion}", eventId, expectedVersion);

        var command = UpdateEventCommand.FromRequest(eventId, request, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        // Set ETag header for optimistic concurrency control
        Response.Headers.Add("ETag", result.Version.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Publish an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published event</returns>
    [HttpPost("{eventId:guid}/publish")]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventDto>> PublishEvent(
        [FromRoute] Guid eventId,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing event {EventId}", eventId);

        var command = new PublishEventCommand(eventId, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        // Set ETag header for optimistic concurrency control
        Response.Headers.Add("ETag", result.Version.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Cancel an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="request">Cancellation request</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled event</returns>
    [HttpPost("{eventId:guid}/cancel")]
    [ProducesResponseType(typeof(EventDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventDto>> CancelEvent(
        [FromRoute] Guid eventId,
        [FromBody] CancelEventRequest request,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling event {EventId} with reason: {Reason}", eventId, request.Reason);

        var command = new CancelEventCommand(eventId, request.Reason, expectedVersion);
        var result = await _mediator.Send(command, cancellationToken);

        // Set ETag header for optimistic concurrency control
        Response.Headers.Add("ETag", result.Version.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Search events with advanced filtering
    /// </summary>
    /// <param name="request">Search request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<EventCatalogDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PagedResult<EventCatalogDto>>> SearchEvents(
        [FromQuery] SearchEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching events with term: {SearchTerm}", request.SearchTerm);

        var query = SearchEventsQuery.FromRequest(request);
        var result = await _mediator.Send(query, cancellationToken);

        // Add pagination headers
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Page-Number", result.PageNumber.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Search for events with cursor-based pagination (recommended for large datasets)
    /// </summary>
    /// <param name="request">Cursor-based search request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cursor-paginated search results</returns>
    [HttpGet("search/cursor")]
    [ProducesResponseType(typeof(CursorPagedResult<EventCatalogDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<CursorPagedResult<EventCatalogDto>>> SearchEventsCursor(
        [FromQuery] SearchEventsCursorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cursor-based search for events with term: {SearchTerm}", request.SearchTerm);

        var query = SearchEventsCursorQuery.FromCursorRequest(request);
        var result = await _mediator.Send(query, cancellationToken);

        // Add cursor pagination headers
        if (result.NextCursor != null)
        {
            Response.Headers.Add("X-Next-Cursor", result.NextCursor);
        }
        if (result.PreviousCursor != null)
        {
            Response.Headers.Add("X-Previous-Cursor", result.PreviousCursor);
        }
        Response.Headers.Add("X-Has-Next-Page", result.HasNextPage.ToString());
        Response.Headers.Add("X-Has-Previous-Page", result.HasPreviousPage.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        if (result.TotalCount.HasValue)
        {
            Response.Headers.Add("X-Total-Count", result.TotalCount.Value.ToString());
        }

        return Ok(result);
    }

    /// <summary>
    /// Get events with basic filtering (admin/management view)
    /// </summary>
    /// <param name="request">Get events request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated events list</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] GetEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting events with filters - Promoter: {PromoterId}, Venue: {VenueId}", 
            request.PromoterId, request.VenueId);

        var query = GetEventsQuery.FromRequest(request);
        var result = await _mediator.Send(query, cancellationToken);

        // Add pagination headers
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Page-Number", result.PageNumber.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Get events with cursor-based pagination (admin/management view)
    /// </summary>
    /// <param name="request">Cursor-based get events request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cursor-paginated events list</returns>
    [HttpGet("cursor")]
    [ProducesResponseType(typeof(CursorPagedResult<EventDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<CursorPagedResult<EventDto>>> GetEventsCursor(
        [FromQuery] GetEventsCursorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cursor-based get events - Promoter: {PromoterId}, Venue: {VenueId}", 
            request.PromoterId, request.VenueId);

        var query = GetEventsCursorQuery.FromCursorRequest(request);
        var result = await _mediator.Send(query, cancellationToken);

        // Add cursor pagination headers
        if (result.NextCursor != null)
        {
            Response.Headers.Add("X-Next-Cursor", result.NextCursor);
        }
        if (result.PreviousCursor != null)
        {
            Response.Headers.Add("X-Previous-Cursor", result.PreviousCursor);
        }
        Response.Headers.Add("X-Has-Next-Page", result.HasNextPage.ToString());
        Response.Headers.Add("X-Has-Previous-Page", result.HasPreviousPage.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        if (result.TotalCount.HasValue)
        {
            Response.Headers.Add("X-Total-Count", result.TotalCount.Value.ToString());
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if event slug is available
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="slug">Slug to check</param>
    /// <param name="excludeEventId">Event ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Availability status</returns>
    [HttpGet("check-slug/{organizationId:guid}/{slug}")]
    [ProducesResponseType(typeof(SlugAvailabilityResponse), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<SlugAvailabilityResponse>> CheckSlugAvailability(
        [FromRoute] Guid organizationId,
        [FromRoute] string slug,
        [FromQuery] Guid? excludeEventId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking slug availability: {Slug} for organization {OrganizationId}", slug, organizationId);

        // This would need to be implemented as a query
        // For now, return a placeholder response
        return Ok(new SlugAvailabilityResponse
        {
            Slug = slug,
            IsAvailable = true, // This should be implemented with actual logic
            SuggestedAlternatives = new List<string>()
        });
    }
}

/// <summary>
/// Cancel event request
/// </summary>
public record CancelEventRequest
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Slug availability response
/// </summary>
public record SlugAvailabilityResponse
{
    public string Slug { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public List<string> SuggestedAlternatives { get; init; } = new();
}
