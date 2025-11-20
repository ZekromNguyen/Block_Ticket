using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Event.API.Controllers;

[ApiController]
[Route("api/v1/internal/events")]
public class InternalController : ControllerBase
{
    private readonly IInventorySnapshotService _inventorySnapshotService;
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<InternalController> _logger;

    public InternalController(IInventorySnapshotService inventorySnapshotService, IVenueRepository venueRepository, IEventRepository eventRepository, ILogger<InternalController> logger)
    {
        _inventorySnapshotService = inventorySnapshotService;
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    // GET /internal/inventory-snapshots/{eventId}
    [HttpGet("{eventId}/inventory-snapshot")]
    public async Task<IActionResult> GetInventorySnapshot(Guid eventId, [FromQuery] ConsistencyMode consistency = ConsistencyMode.Consistent)
    {
        try
        {
            var snapshot = await _inventorySnapshotService.GetInventorySnapshotAsync(eventId, consistency, HttpContext.RequestAborted);

            if (snapshot == null)
            {
                return NotFound(new { message = $"Inventory snapshot for event {eventId} not found." });
            }

            var etag = snapshot.ETag;
            if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = new Microsoft.Extensions.Primitives.StringValues(etag);
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching the inventory snapshot for event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }

    // GET /internal/seat-maps/{seatMapId}
    [HttpGet("/api/v1/internal/seat-maps/{seatMapId}")]
    public async Task<IActionResult> GetSeatMap(Guid seatMapId)
    {
        try
        {
            var venue = await _venueRepository.GetWithSeatMapAsync(seatMapId, HttpContext.RequestAborted);

            if (venue == null || !venue.HasSeatMap)
            {
                return NotFound(new { message = $"Seat map with ID {seatMapId} not found." });
            }

            var etag = venue.SeatMapChecksum ?? string.Empty;
            if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = new Microsoft.Extensions.Primitives.StringValues(etag);

            var seatMap = new
            {
                venue.SeatMapMetadata,
                venue.Seats
            };

            return Ok(seatMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching the seat map for ID {SeatMapId}.", seatMapId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }

    // GET /internal/pricing/{eventId}
    [HttpGet("{eventId}/pricing")]
    public async Task<IActionResult> GetPricing(Guid eventId)
    {
        try
        {
            var eventAggregate = await _eventRepository.GetWithFullDetailsAsync(eventId, HttpContext.RequestAborted);

            if (eventAggregate == null)
            {
                return NotFound(new { message = $"Pricing information for event {eventId} not found." });
            }

            var pricingInfo = new PricingInfo
            {
                EventId = eventId,
                TicketTypes = eventAggregate.TicketTypes.Select(tt => new TicketTypePricing
                {
                    TicketTypeId = tt.Id,
                    Name = tt.Name,
                    BasePrice = tt.BasePrice.Amount
                }).ToList(),
                PricingRules = eventAggregate.PricingRules.Select(pr => new PricingRuleInfo
                {
                    RuleId = pr.Id,
                    Name = pr.Name,
                    Adjustment = pr.DiscountValue ?? 0
                }).ToList()
            };

            var etag = GenerateETag(pricingInfo);
            pricingInfo.ETag = etag;

            if (Request.Headers.TryGetValue("If-None-Match", out var requestEtag) && requestEtag == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = new Microsoft.Extensions.Primitives.StringValues(etag);
            return Ok(pricingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching pricing information for event {EventId}.", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
        }
    }

    private string GenerateETag(object data)
    {
        var json = JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

