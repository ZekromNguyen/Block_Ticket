using Event.Application.Common.Models;
using Event.Application.Features.EventSeries.Commands.CreateEventSeries;
using Event.Application.Features.EventSeries.Queries.GetEventSeries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Event Series management API controller
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class EventSeriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventSeriesController> _logger;

    public EventSeriesController(IMediator mediator, ILogger<EventSeriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new event series
    /// </summary>
    /// <param name="request">Event series creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event series</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventSeriesDto>> CreateEventSeries(
        [FromBody] CreateEventSeriesRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating event series with slug: {Slug}", request.Slug);

        var command = CreateEventSeriesCommand.FromRequest(request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetEventSeries),
            new { seriesId = result.Id, version = "1.0" },
            result);
    }

    /// <summary>
    /// Get event series by ID
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="includeEvents">Include events in the series</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event series details</returns>
    [HttpGet("{seriesId:guid}")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventSeriesDto>> GetEventSeries(
        [FromRoute] Guid seriesId,
        [FromQuery] bool includeEvents = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting event series {SeriesId}", seriesId);

        var query = new GetEventSeriesQuery(seriesId, includeEvents);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Event series with ID '{seriesId}' not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get event series list with filtering and pagination
    /// </summary>
    /// <param name="request">Get event series request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated event series list</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventSeriesDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PagedResult<EventSeriesDto>>> GetEventSeriesList(
        [FromQuery] GetEventSeriesRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting event series list - Promoter: {PromoterId}, Active: {IsActive}", 
            request.PromoterId, request.IsActive);

        var query = GetEventSeriesListQuery.FromRequest(request);
        var result = await _mediator.Send(query, cancellationToken);

        // Add pagination headers
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Page-Number", result.PageNumber.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Update an existing event series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="request">Event series update request</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event series</returns>
    [HttpPut("{seriesId:guid}")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventSeriesDto>> UpdateEventSeries(
        [FromRoute] Guid seriesId,
        [FromBody] UpdateEventSeriesRequest request,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating event series {SeriesId} with expected version {ExpectedVersion}", 
            seriesId, expectedVersion);

        // This would need to be implemented as an UpdateEventSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Update event series not yet implemented");
    }

    /// <summary>
    /// Delete an event series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{seriesId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult> DeleteEventSeries(
        [FromRoute] Guid seriesId,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting event series {SeriesId}", seriesId);

        // This would need to be implemented as a DeleteEventSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Delete event series not yet implemented");
    }

    /// <summary>
    /// Add an event to a series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="request">Add event to series request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event series</returns>
    [HttpPost("{seriesId:guid}/events")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventSeriesDto>> AddEventToSeries(
        [FromRoute] Guid seriesId,
        [FromBody] AddEventToSeriesRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding event {EventId} to series {SeriesId}", request.EventId, seriesId);

        // This would need to be implemented as an AddEventToSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Add event to series not yet implemented");
    }

    /// <summary>
    /// Remove an event from a series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="eventId">Event ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event series</returns>
    [HttpDelete("{seriesId:guid}/events/{eventId:guid}")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventSeriesDto>> RemoveEventFromSeries(
        [FromRoute] Guid seriesId,
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing event {EventId} from series {SeriesId}", eventId, seriesId);

        // This would need to be implemented as a RemoveEventFromSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Remove event from series not yet implemented");
    }

    /// <summary>
    /// Get events in a series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated events in the series</returns>
    [HttpGet("{seriesId:guid}/events")]
    [ProducesResponseType(typeof(PagedResult<EventDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEventsInSeries(
        [FromRoute] Guid seriesId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting events in series {SeriesId}", seriesId);

        // This would need to be implemented as a GetEventsInSeriesQuery
        // For now, return a placeholder response
        return BadRequest("Get events in series not yet implemented");
    }

    /// <summary>
    /// Activate an event series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event series</returns>
    [HttpPost("{seriesId:guid}/activate")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventSeriesDto>> ActivateEventSeries(
        [FromRoute] Guid seriesId,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating event series {SeriesId}", seriesId);

        // This would need to be implemented as an ActivateEventSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Activate event series not yet implemented");
    }

    /// <summary>
    /// Deactivate an event series
    /// </summary>
    /// <param name="seriesId">Event series ID</param>
    /// <param name="expectedVersion">Expected version for optimistic concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event series</returns>
    [HttpPost("{seriesId:guid}/deactivate")]
    [ProducesResponseType(typeof(EventSeriesDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult<EventSeriesDto>> DeactivateEventSeries(
        [FromRoute] Guid seriesId,
        [FromHeader(Name = "If-Match")] int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating event series {SeriesId}", seriesId);

        // This would need to be implemented as a DeactivateEventSeriesCommand
        // For now, return a placeholder response
        return BadRequest("Deactivate event series not yet implemented");
    }
}

/// <summary>
/// Add event to series request
/// </summary>
public record AddEventToSeriesRequest
{
    public Guid EventId { get; init; }
}
