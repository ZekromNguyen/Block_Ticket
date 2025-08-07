using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Event.Api.Data;
using Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;

namespace Event.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventDbContext _context;
    private readonly ILogger<EventsController> _logger;

    public EventsController(EventDbContext context, ILogger<EventsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Models.Event>>>> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<Models.Event>>.SuccessResult(events));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Models.Event>>> GetEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (eventEntity == null)
        {
            return NotFound(ApiResponse<Models.Event>.ErrorResult("Event not found"));
        }

        return Ok(ApiResponse<Models.Event>.SuccessResult(eventEntity));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Models.Event>>> CreateEvent(CreateEventRequest request)
    {
        var eventEntity = new Models.Event
        {
            Name = request.Name,
            Description = request.Description,
            Venue = request.Venue,
            EventDate = request.EventDate,
            SaleStartDate = request.SaleStartDate,
            SaleEndDate = request.SaleEndDate,
            TotalTickets = request.TotalTickets,
            AvailableTickets = request.TotalTickets,
            TicketPrice = request.TicketPrice,
            ImageUrl = request.ImageUrl,
            PromoterId = request.PromoterId,
            Status = Models.EventStatus.Draft
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event {EventName} created successfully", eventEntity.Name);
        return CreatedAtAction(nameof(GetEvent), new { id = eventEntity.Id }, 
            ApiResponse<Models.Event>.SuccessResult(eventEntity, "Event created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Models.Event>>> UpdateEvent(Guid id, UpdateEventRequest request)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return NotFound(ApiResponse<Models.Event>.ErrorResult("Event not found"));
        }

        eventEntity.Name = request.Name;
        eventEntity.Description = request.Description;
        eventEntity.Venue = request.Venue;
        eventEntity.EventDate = request.EventDate;
        eventEntity.SaleStartDate = request.SaleStartDate;
        eventEntity.SaleEndDate = request.SaleEndDate;
        eventEntity.TicketPrice = request.TicketPrice;
        eventEntity.ImageUrl = request.ImageUrl;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<Models.Event>.SuccessResult(eventEntity, "Event updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteEvent(Guid id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return NotFound(ApiResponse.ErrorResult("Event not found"));
        }

        eventEntity.IsDeleted = true;
        eventEntity.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResult("Event deleted successfully"));
    }
}

public record CreateEventRequest(
    string Name,
    string Description,
    string Venue,
    DateTime EventDate,
    DateTime SaleStartDate,
    DateTime SaleEndDate,
    int TotalTickets,
    decimal TicketPrice,
    string ImageUrl,
    Guid PromoterId
);

public record UpdateEventRequest(
    string Name,
    string Description,
    string Venue,
    DateTime EventDate,
    DateTime SaleStartDate,
    DateTime SaleEndDate,
    decimal TicketPrice,
    string ImageUrl
);
