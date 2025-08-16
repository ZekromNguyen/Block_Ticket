using Event.API.Middleware;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Event.API.Controllers;

/// <summary>
/// Controller for ticket reservations with ETag-based optimistic concurrency
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/reservations")]
[ApiVersion("1.0")]
[SupportsETag(GenerateForGet = true, SupportConditionalUpdates = true)]
public class ReservationController : ControllerBase
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        ILogger<ReservationController> logger)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new ticket reservation with optimistic concurrency control
    /// </summary>
    [HttpPost]
    [RequireETag]
    public async Task<ActionResult<ReservationResponse>> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the expected ETag from the If-Match header
            var expectedETag = HttpContext.GetIfMatchETagObject("EventAggregate", request.EventId.ToString());
            if (expectedETag == null)
            {
                return BadRequest(new
                {
                    error = "Missing ETag",
                    message = "If-Match header with event ETag is required for reservation creation"
                });
            }

            _logger.LogInformation("Creating reservation for event {EventId} with ETag {ETag}", 
                request.EventId, expectedETag.Value);

            // Attempt to reserve tickets atomically with ETag validation
            var reservationSuccessful = await _eventRepository.TryReserveTicketsWithETagAsync(
                request.EventId,
                request.TicketTypeId,
                request.Quantity,
                expectedETag,
                cancellationToken);

            if (!reservationSuccessful)
            {
                // Get current event state for comparison
                var (currentEvent, currentETag) = await _eventRepository.GetWithETagAsync(request.EventId, cancellationToken);
                
                if (currentEvent == null)
                {
                    return NotFound($"Event {request.EventId} not found");
                }

                if (currentETag != null && !currentETag.Matches(expectedETag))
                {
                    _logger.LogWarning("ETag mismatch during reservation for event {EventId}. Expected: {Expected}, Current: {Current}",
                        request.EventId, expectedETag.Value, currentETag.Value);

                    // Get updated inventory for client
                    var inventorySummary = await _eventRepository.GetInventorySummaryWithETagAsync(request.EventId, cancellationToken);
                    
                    return StatusCode((int)HttpStatusCode.PreconditionFailed, new
                    {
                        error = "Inventory Changed",
                        message = "The event inventory has been modified since your last request. Please refresh and try again.",
                        expectedETag = expectedETag.Value,
                        currentETag = currentETag.Value,
                        currentInventory = inventorySummary != null ? new
                        {
                            available = inventorySummary.Value.Available,
                            sold = inventorySummary.Value.Sold,
                            etag = inventorySummary.Value.ETag.Value
                        } : null
                    });
                }

                // Reservation failed due to insufficient inventory
                var ticketType = currentEvent.TicketTypes.FirstOrDefault(tt => tt.Id == request.TicketTypeId);
                if (ticketType == null)
                {
                    return BadRequest($"Ticket type {request.TicketTypeId} not found for event {request.EventId}");
                }

                return BadRequest(new
                {
                    error = "Insufficient Inventory",
                    message = $"Only {ticketType.GetAvailableQuantity()} tickets available, requested {request.Quantity}",
                    available = ticketType.GetAvailableQuantity(),
                    requested = request.Quantity
                });
            }

            // Create the reservation record
            var reservation = Domain.Entities.Reservation.Create(
                request.EventId,
                request.TicketTypeId,
                request.Quantity,
                request.CustomerEmail,
                TimeSpan.FromMinutes(15) // 15 minute reservation timeout
            );

            await _reservationRepository.AddAsync(reservation, cancellationToken);

            // Get updated event state after successful reservation
            var (updatedEvent, updatedETag) = await _eventRepository.GetWithETagAsync(request.EventId, cancellationToken);
            
            // Set the new ETag in response
            if (updatedETag != null)
            {
                Response.SetETag(updatedETag);
            }

            _logger.LogInformation("Successfully created reservation {ReservationId} for event {EventId}",
                reservation.Id, request.EventId);

            return CreatedAtAction(
                nameof(GetReservation),
                new { id = reservation.Id },
                new ReservationResponse
                {
                    Id = reservation.Id,
                    EventId = reservation.EventId,
                    TicketTypeId = reservation.TicketTypeId,
                    Quantity = reservation.Quantity,
                    CustomerEmail = reservation.CustomerEmail,
                    Status = reservation.Status.ToString(),
                    ExpiresAt = reservation.ExpiresAt,
                    CreatedAt = reservation.Created ?? DateTime.UtcNow,
                    ETag = updatedETag?.Value
                });
        }
        catch (ETagMismatchException ex)
        {
            _logger.LogWarning(ex, "ETag mismatch during reservation creation");
            
            return StatusCode((int)HttpStatusCode.PreconditionFailed, new
            {
                error = "Precondition Failed",
                message = ex.Message,
                expectedETag = ex.ExpectedETag,
                actualETag = ex.ActualETag
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation for event {EventId}", request.EventId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a reservation by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationResponse>> GetReservation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
        
        if (reservation == null)
        {
            return NotFound($"Reservation {id} not found");
        }

        // Get event ETag for inventory state
        var eventETag = await _eventRepository.GetETagAsync(reservation.EventId, cancellationToken);
        
        if (eventETag != null)
        {
            Response.SetETag(eventETag);
        }

        return Ok(new ReservationResponse
        {
            Id = reservation.Id,
            EventId = reservation.EventId,
            TicketTypeId = reservation.TicketTypeId,
            Quantity = reservation.Quantity,
            CustomerEmail = reservation.CustomerEmail,
            Status = reservation.Status.ToString(),
            ExpiresAt = reservation.ExpiresAt,
            CreatedAt = reservation.Created ?? DateTime.UtcNow,
            ETag = eventETag?.Value
        });
    }

    /// <summary>
    /// Cancels a reservation and releases the tickets
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireETag]
    public async Task<ActionResult> CancelReservation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
            if (reservation == null)
            {
                return NotFound($"Reservation {id} not found");
            }

            // Get the expected ETag from the If-Match header
            var expectedETag = HttpContext.GetIfMatchETagObject("EventAggregate", reservation.EventId.ToString());
            if (expectedETag == null)
            {
                return BadRequest(new
                {
                    error = "Missing ETag",
                    message = "If-Match header with event ETag is required for reservation cancellation"
                });
            }

            // Release tickets atomically with ETag validation
            var releaseSuccessful = await _eventRepository.TryReleaseTicketsWithETagAsync(
                reservation.EventId,
                reservation.TicketTypeId,
                reservation.Quantity,
                expectedETag,
                cancellationToken);

            if (!releaseSuccessful)
            {
                var currentETag = await _eventRepository.GetETagAsync(reservation.EventId, cancellationToken);
                
                return StatusCode((int)HttpStatusCode.PreconditionFailed, new
                {
                    error = "Inventory State Changed",
                    message = "The event inventory has been modified. Please refresh and try again.",
                    expectedETag = expectedETag.Value,
                    currentETag = currentETag?.Value
                });
            }

            // Mark reservation as cancelled
            reservation.Cancel();
            await _reservationRepository.UpdateAsync(reservation, cancellationToken);

            // Get updated ETag
            var updatedETag = await _eventRepository.GetETagAsync(reservation.EventId, cancellationToken);
            if (updatedETag != null)
            {
                Response.SetETag(updatedETag);
            }

            _logger.LogInformation("Successfully cancelled reservation {ReservationId}", id);
            
            return NoContent();
        }
        catch (ETagMismatchException ex)
        {
            _logger.LogWarning(ex, "ETag mismatch during reservation cancellation");
            
            return StatusCode((int)HttpStatusCode.PreconditionFailed, new
            {
                error = "Precondition Failed",
                message = ex.Message,
                expectedETag = ex.ExpectedETag,
                actualETag = ex.ActualETag
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the current inventory status for an event with ETag
    /// </summary>
    [HttpGet("events/{eventId:guid}/inventory")]
    public async Task<ActionResult<InventoryStatusResponse>> GetEventInventoryStatus(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var inventorySummary = await _eventRepository.GetInventorySummaryWithETagAsync(eventId, cancellationToken);
        
        if (inventorySummary == null)
        {
            return NotFound($"Event {eventId} not found");
        }

        var (available, sold, etag) = inventorySummary.Value;

        // Set ETag header for conditional requests
        Response.SetETag(etag);

        // Check for If-None-Match header (304 Not Modified scenario)
        var ifNoneMatch = Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(ifNoneMatch) && etag.Matches(ifNoneMatch))
        {
            return StatusCode((int)HttpStatusCode.NotModified);
        }

        return Ok(new InventoryStatusResponse
        {
            EventId = eventId,
            Available = available,
            Sold = sold,
            Total = available + sold,
            LastUpdated = etag.GeneratedAt,
            ETag = etag.Value
        });
    }

    /// <summary>
    /// Bulk inventory check for multiple events
    /// </summary>
    [HttpPost("inventory/bulk-check")]
    public async Task<ActionResult<Dictionary<Guid, InventoryStatusResponse>>> BulkInventoryCheck(
        [FromBody] BulkInventoryCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<Guid, InventoryStatusResponse>();

        foreach (var eventId in request.EventIds)
        {
            var inventorySummary = await _eventRepository.GetInventorySummaryWithETagAsync(eventId, cancellationToken);
            
            if (inventorySummary != null)
            {
                var (available, sold, etag) = inventorySummary.Value;
                
                results[eventId] = new InventoryStatusResponse
                {
                    EventId = eventId,
                    Available = available,
                    Sold = sold,
                    Total = available + sold,
                    LastUpdated = etag.GeneratedAt,
                    ETag = etag.Value
                };
            }
        }

        return Ok(results);
    }
}

#region Request/Response Models

public class CreateReservationRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid TicketTypeId { get; set; }

    [Required]
    [Range(1, 10)]
    public int Quantity { get; set; }

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
}

public class ReservationResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TicketTypeId { get; set; }
    public int Quantity { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ETag { get; set; }
}

public class InventoryStatusResponse
{
    public Guid EventId { get; set; }
    public int Available { get; set; }
    public int Sold { get; set; }
    public int Total { get; set; }
    public DateTime LastUpdated { get; set; }
    public string ETag { get; set; } = string.Empty;
}

public class BulkInventoryCheckRequest
{
    [Required]
    public List<Guid> EventIds { get; set; } = new();
}

#endregion
