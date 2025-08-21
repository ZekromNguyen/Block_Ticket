using Event.API.Attributes;
using Event.Application.Interfaces.Infrastructure;
using Event.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Event.API.Controllers;

/// <summary>
/// Example controller demonstrating idempotent reservation operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
public class IdempotentReservationsController : ControllerBase
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotentReservationsController> _logger;

    public IdempotentReservationsController(
        IIdempotencyService idempotencyService,
        ILogger<IdempotentReservationsController> logger)
    {
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a reservation with automatic idempotency handling via middleware
    /// </summary>
    [HttpPost]
    [Idempotent(TTLHours = 24)] // 24 hours TTL
    public async Task<ActionResult<ReservationDto>> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating reservation for event {EventId}", request.EventId);

        // Simulate reservation creation logic
        await Task.Delay(100, cancellationToken); // Simulate some processing time

        var reservation = new ReservationDto
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            UserId = request.UserId,
            TicketTypeIds = request.TicketTypeIds,
            Quantity = request.Quantity,
            Status = "Confirmed",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Reservation created with ID {ReservationId}", reservation.Id);
        return Ok(reservation);
    }

    /// <summary>
    /// Creates a reservation with manual idempotency handling
    /// </summary>
    [HttpPost("manual")]
    [NoIdempotency] // Exclude from automatic middleware handling
    public async Task<ActionResult<ReservationDto>> CreateReservationManual(
        [FromBody] CreateReservationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header is required");
        }

        if (!_idempotencyService.IsValidIdempotencyKey(idempotencyKey))
        {
            return BadRequest("Invalid Idempotency-Key format");
        }

        var result = await _idempotencyService.ProcessRequestAsync<ReservationDto>(
            idempotencyKey,
            Request.Path,
            Request.Method,
            await ReadRequestBodyAsync(),
            SerializeHeaders(Request.Headers),
            async (ct) =>
            {
                _logger.LogInformation("Creating reservation for event {EventId}", request.EventId);

                // Simulate reservation creation logic
                await Task.Delay(100, ct);

                return new ReservationDto
                {
                    Id = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = request.UserId,
                    TicketTypeIds = request.TicketTypeIds,
                    Quantity = request.Quantity,
                    Status = "Confirmed",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow
                };
            },
            userId: request.UserId,
            requestId: HttpContext.TraceIdentifier,
            cancellationToken: cancellationToken);

        if (result.IsNewRequest)
        {
            _logger.LogInformation("New reservation created with ID {ReservationId}", result.Response.Id);
        }
        else
        {
            _logger.LogInformation("Returning cached reservation with ID {ReservationId}", result.Response.Id);
        }

        return Ok(result.Response);
    }

    /// <summary>
    /// Updates a reservation (PUT operations should be idempotent by nature)
    /// </summary>
    [HttpPut("{id}")]
    [Idempotent(TTLHours = 12)] // 12 hours TTL for updates
    public async Task<ActionResult<ReservationDto>> UpdateReservation(
        Guid id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating reservation {ReservationId}", id);

        // Simulate update logic
        await Task.Delay(50, cancellationToken);

        var reservation = new ReservationDto
        {
            Id = id,
            EventId = request.EventId,
            UserId = request.UserId,
            TicketTypeIds = request.TicketTypeIds,
            Quantity = request.Quantity,
            Status = request.Status,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow.AddHours(-1), // Simulate existing reservation
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(reservation);
    }

    /// <summary>
    /// Gets idempotency records for debugging/admin purposes
    /// </summary>
    [HttpGet("idempotency/{key}")]
    public async Task<ActionResult<object>> GetIdempotencyRecord(
        string key,
        CancellationToken cancellationToken = default)
    {
        var response = await _idempotencyService.GetStoredResponseAsync<object>(key, cancellationToken);
        
        if (response == null)
        {
            return NotFound("Idempotency record not found or expired");
        }

        return Ok(response);
    }

    /// <summary>
    /// Admin endpoint to cleanup expired idempotency records
    /// </summary>
    [HttpPost("idempotency/cleanup")]
    public async Task<ActionResult<object>> CleanupExpiredRecords(
        CancellationToken cancellationToken = default)
    {
        var removedCount = await _idempotencyService.CleanupExpiredRecordsAsync(cancellationToken);
        
        return Ok(new { RemovedCount = removedCount, CleanedAt = DateTime.UtcNow });
    }

    private async Task<string?> ReadRequestBodyAsync()
    {
        if (Request.Body == null || !Request.Body.CanRead)
            return null;

        Request.EnableBuffering();
        Request.Body.Position = 0;

        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        
        Request.Body.Position = 0;
        return body;
    }

    private static string SerializeHeaders(IHeaderDictionary headers)
    {
        var relevantHeaders = headers
            .Where(h => !h.Key.StartsWith("Idempotency", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return System.Text.Json.JsonSerializer.Serialize(relevantHeaders);
    }
}

/// <summary>
/// Request model for creating reservations
/// </summary>
public class CreateReservationRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<Guid> TicketTypeIds { get; set; } = new();

    [Required]
    [Range(1, 10)]
    public int Quantity { get; set; }
}

/// <summary>
/// Request model for updating reservations
/// </summary>
public class UpdateReservationRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<Guid> TicketTypeIds { get; set; } = new();

    [Required]
    [Range(1, 10)]
    public int Quantity { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Reservation DTO for responses
/// </summary>
public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<Guid> TicketTypeIds { get; set; } = new();
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
