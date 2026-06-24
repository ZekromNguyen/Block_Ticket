using Event.Application.Common.Models;
using Event.Application.Features.Events.Queries.GetEvent;
using Event.Application.Features.Events.Queries.SearchEvents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Event.Domain.Enums;
namespace Event.API.Controllers;

/// <summary>
/// Public events API controller for catalog and search functionality
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/public/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class PublicEventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PublicEventsController> _logger;

    public PublicEventsController(IMediator mediator, ILogger<PublicEventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Search public events with advanced filtering
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<EventCatalogDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PagedResult<EventCatalogDto>>> SearchPublicEvents(
        [FromQuery] SearchPublicEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching public events with term: {SearchTerm}", request.SearchTerm);

        var searchQuery = new SearchEventsQuery
        {
            SearchTerm = request.SearchTerm,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            City = request.City,
            Categories = request.Categories,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            HasAvailability = true,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var result = await _mediator.Send(searchQuery, cancellationToken);

        var etag = GenerateETag(result);
        if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=30, must-revalidate";
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page-Number"] = result.PageNumber.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();

        return Ok(result);
    }

    /// <summary>
    /// Get public events catalog
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventCatalogDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PagedResult<EventCatalogDto>>> GetPublicEvents(
        [FromQuery] GetPublicEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting public events catalog");

        var searchQuery = new SearchEventsQuery
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            City = request.City,
            Categories = request.Categories,
            HasAvailability = true,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = "EventDate",
            SortDescending = false
        };

        var result = await _mediator.Send(searchQuery, cancellationToken);

        var etag = GenerateETag(result);
        if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=30, must-revalidate";
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page-Number"] = result.PageNumber.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();

        return Ok(result);
    }

    /// <summary>
    /// Get public event details by ID
    /// </summary>
    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventDetailDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetPublicEventDetail(
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting public event detail {EventId}", eventId);

        var query = new GetEventQuery(eventId, true, false);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Event with ID '{eventId}' not found");
        }

        if (result.Status != EventStatus.Published)
        {
            return NotFound($"Event with ID '{eventId}' is not available to the public");
        }

        var eventDetail = MapToEventDetailDto(result);

        var etag = GenerateETag(eventDetail);
        if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=60, must-revalidate";

        return Ok(eventDetail);
    }

    /// <summary>
    /// Get public event details by slug
    /// </summary>
    [HttpGet("by-slug/{organizationId:guid}/{slug}")]
    [ProducesResponseType(typeof(EventDetailDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetPublicEventDetailBySlug(
        [FromRoute] Guid organizationId,
        [FromRoute] string slug,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting public event detail by slug {Slug} for organization {OrganizationId}",
            slug, organizationId);

        var query = new GetEventBySlugQuery(slug, organizationId, true, false);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound($"Event with slug '{slug}' not found for organization '{organizationId}'");
        }

        if (result.Status != EventStatus.Published)
        {
            return NotFound($"Event with slug '{slug}' is not available to the public");
        }

        var eventDetail = MapToEventDetailDto(result);

        var etag = GenerateETag(eventDetail);
        if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=60, must-revalidate";

        return Ok(eventDetail);
    }

    /// <summary>
    /// Get upcoming public events
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(PagedResult<EventCatalogDto>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PagedResult<EventCatalogDto>>> GetUpcomingEvents(
        [FromQuery] int days = 30,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting upcoming public events for {Days} days", days);

        var endDate = DateTime.UtcNow.AddDays(days);

        var searchQuery = new SearchEventsQuery
        {
            StartDate = DateTime.UtcNow,
            EndDate = endDate,
            HasAvailability = true,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = "EventDate",
            SortDescending = false
        };

        var result = await _mediator.Send(searchQuery, cancellationToken);

        var etag = GenerateETag(result);
        if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=30, must-revalidate";
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page-Number"] = result.PageNumber.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();

        return Ok(result);
    }

    /// <summary>
    /// Get recommended events (placeholder implementation)
    /// </summary>
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(List<EventCatalogDto>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<EventCatalogDto>>> GetRecommendedEvents(
        [FromQuery] GetRecommendedEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting recommended events for user {UserId}", request.UserId);

        var searchQuery = new SearchEventsQuery
        {
            StartDate = DateTime.UtcNow,
            Categories = request.PreferredCategories,
            City = request.PreferredCity,
            HasAvailability = true,
            PageNumber = 1,
            PageSize = request.MaxResults,
            SortBy = "EventDate",
            SortDescending = false
        };

        var result = await _mediator.Send(searchQuery, cancellationToken);

        return Ok(result.Items.ToList());
    }

    private static string GenerateETag(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return $"\"{Convert.ToHexString(hashBytes).ToLowerInvariant()}\"";
    }

    private static EventDetailDto MapToEventDetailDto(EventDto eventDto)
    {
        return new EventDetailDto
        {
            Id = eventDto.Id,
            Title = eventDto.Title,
            Description = eventDto.Description,
            Slug = eventDto.Slug,
            EventDate = eventDto.EventDate,
            TimeZone = eventDto.TimeZone,
            ImageUrl = eventDto.ImageUrl,
            BannerUrl = eventDto.BannerUrl,
            Categories = eventDto.Categories,
            Tags = eventDto.Tags,
            Venue = new VenueDto
            {
                Id = eventDto.VenueId,
                Name = "Venue Name", // This would need to be populated from venue data
                // Other venue properties would be populated here
            },
            TicketTypes = eventDto.TicketTypes.Select(tt => new TicketTypePublicDto
            {
                Id = tt.Id,
                Name = tt.Name,
                Description = tt.Description,
                BasePrice = tt.BasePrice,
                ServiceFee = tt.ServiceFee,
                TotalPrice = new MoneyDto
                {
                    Amount = tt.BasePrice.Amount + (tt.ServiceFee?.Amount ?? 0),
                    Currency = tt.BasePrice.Currency
                },
                AvailableQuantity = tt.Capacity.Available,
                MinPurchaseQuantity = tt.MinPurchaseQuantity,
                MaxPurchaseQuantity = tt.MaxPurchaseQuantity,
                MaxPerCustomer = tt.MaxPerCustomer,
                IsOnSale = tt.OnSaleWindows.Any(w => w.StartDate <= DateTime.UtcNow && w.EndDate >= DateTime.UtcNow),
                OnSaleStartDate = tt.OnSaleWindows.FirstOrDefault()?.StartDate,
                OnSaleEndDate = tt.OnSaleWindows.FirstOrDefault()?.EndDate
            }).ToList(),
            Availability = new EventAvailabilityDto
            {
                EventId = eventDto.Id,
                EventName = eventDto.Title,
                TotalCapacity = eventDto.TicketTypes.Sum(tt => tt.Capacity.Total),
                AvailableCapacity = eventDto.TicketTypes.Sum(tt => tt.Capacity.Available),
                ReservedCapacity = eventDto.TicketTypes.Sum(tt => tt.Capacity.Reserved),
                HasAvailability = eventDto.TicketTypes.Any(tt => tt.Capacity.Available > 0),
                TicketTypes = eventDto.TicketTypes.Select(tt => new AvailabilityDto
                {
                    TicketTypeId = tt.Id,
                    TicketTypeName = tt.Name,
                    TotalCapacity = tt.Capacity.Total,
                    AvailableCapacity = tt.Capacity.Available,
                    ReservedCapacity = tt.Capacity.Reserved,
                    IsOnSale = tt.OnSaleWindows.Any(w => w.StartDate <= DateTime.UtcNow && w.EndDate >= DateTime.UtcNow),
                    NextOnSaleDate = tt.OnSaleWindows.Where(w => w.StartDate > DateTime.UtcNow).OrderBy(w => w.StartDate).FirstOrDefault()?.StartDate,
                    OnSaleEndDate = tt.OnSaleWindows.Where(w => w.EndDate > DateTime.UtcNow).OrderBy(w => w.EndDate).FirstOrDefault()?.EndDate
                }).ToList(),
                InventoryETag = Guid.NewGuid().ToString(), // This would be generated properly
                LastUpdated = DateTime.UtcNow
            }
        };
    }
}
