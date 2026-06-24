using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Infrastructure.Services;
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

    // POST /internal/pricing/evaluate
    [HttpPost("/api/v1/internal/pricing/evaluate")]
    [ProducesResponseType(typeof(PricingEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluatePricing([FromBody] PricingEvaluationRequest request, CancellationToken ct)
    {
        try
        {
            if (request.EventId == Guid.Empty)
                return BadRequest(new { message = "EventId is required" });
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "At least one item is required" });

            var eventAggregate = await _eventRepository.GetWithFullDetailsAsync(request.EventId, ct);
            if (eventAggregate == null)
                return NotFound(new { message = $"Event {request.EventId} not found" });

            var currency = request.Currency ?? "USD";
            var subtotal = 0m;
            var discountTotal = 0m;
            var pricedItems = new List<PricedLineItemResponse>();
            var appliedRules = new List<AppliedPricingRuleResponse>();

            foreach (var item in request.Items)
            {
                var ticketType = eventAggregate.TicketTypes.FirstOrDefault(t => t.Id == item.TicketTypeId);
                var basePrice = ticketType?.BasePrice.Amount ?? item.BaseUnitPrice;
                var lineTotal = basePrice * item.Quantity;
                subtotal += lineTotal;
                pricedItems.Add(new PricedLineItemResponse(
                    item.TicketTypeId, item.TicketTypeName, basePrice, basePrice, item.Quantity, lineTotal, 0m));
            }

            foreach (var rule in eventAggregate.PricingRules.Where(r => r.IsActive))
            {
                if (!rule.CanBeUsed()) continue;
                if (rule.Type == PricingRuleType.DiscountCode &&
                    (string.IsNullOrWhiteSpace(request.DiscountCode) ||
                     !string.Equals(rule.DiscountCode, request.DiscountCode, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var original = new Domain.ValueObjects.Money(subtotal, currency);
                var discount = rule.CalculateDiscount(original, request.Items.Sum(i => i.Quantity));
                if (discount.Amount > 0)
                {
                    discountTotal += discount.Amount;
                    appliedRules.Add(new AppliedPricingRuleResponse(
                        rule.Id, rule.Name, rule.Type.ToString(),
                        rule.DiscountType?.ToString() ?? "Unknown",
                        rule.DiscountValue ?? 0, discount.Amount));
                }
            }

            var totalAfterDiscount = Math.Max(0, subtotal - discountTotal);
            var serviceFee = Math.Round(totalAfterDiscount * 0.05m, 2);
            var processingFee = 0.30m;
            var totalAmount = totalAfterDiscount + serviceFee + processingFee;

            var response = new PricingEvaluationResponse(
                request.EventId, currency,
                pricedItems, subtotal, discountTotal, serviceFee, processingFee, totalAmount, appliedRules);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating pricing for event {EventId}", request.EventId);
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    // GET /internal/events/{eventId}/currency-policy
    [HttpGet("{eventId:guid}/currency-policy")]
    [ProducesResponseType(typeof(CurrencyPolicyResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrencyPolicy(Guid eventId, CancellationToken ct)
    {
        try
        {
            var eventAggregate = await _eventRepository.GetWithFullDetailsAsync(eventId, ct);
            if (eventAggregate == null)
                return NotFound(new { message = $"Event {eventId} not found" });

            var defaultCurrency = eventAggregate.TicketTypes.FirstOrDefault()?.BasePrice.Currency ?? "USD";
            var policy = new CurrencyPolicyResponse(
                eventId, defaultCurrency,
                new[] { new AllowedCurrencyResponse(defaultCurrency, defaultCurrency, null, true) },
                5.0m, 0.30m, Array.Empty<CurrencyFeeResponse>());

            return Ok(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching currency policy for event {EventId}", eventId);
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    // POST /internal/risk/assess
    [HttpPost("/api/v1/internal/risk/assess")]
    [ProducesResponseType(typeof(RiskAssessmentResponse), StatusCodes.Status200OK)]
    public IActionResult AssessRisk([FromBody] RiskAssessmentRequest request)
    {
        var signals = new List<RiskSignalResponse>();
        var score = 0m;

        if (request.TicketQuantity > 10)
        {
            signals.Add(new RiskSignalResponse("HighQuantity", $"Quantity {request.TicketQuantity} exceeds normal", 0.3m, "Medium"));
            score += 0.3m;
        }

        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            signals.Add(new RiskSignalResponse("DiscountUsed", "Discount code applied", 0.1m, "Low"));
            score += 0.1m;
        }

        var riskLevel = score >= 0.5m ? "High" : score >= 0.2m ? "Medium" : "Low";
        var approved = score < 0.7m;

        return Ok(new RiskAssessmentResponse(
            approved, riskLevel, score, signals,
            approved ? null : "Transaction requires manual review"));
    }

    private string GenerateETag(object data)
    {
        var json = JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

// ── Internal request/response models for pricing, currency, and risk endpoints ──

public sealed record PricingEvaluationRequest(
    Guid EventId,
    IReadOnlyCollection<PricingEvalLineItem> Items,
    string? DiscountCode,
    Guid? UserId,
    string Currency);

public sealed record PricingEvalLineItem(
    Guid TicketTypeId,
    string TicketTypeName,
    decimal BaseUnitPrice,
    int Quantity);

public sealed record PricingEvaluationResponse(
    Guid EventId,
    string Currency,
    IReadOnlyCollection<PricedLineItemResponse> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ServiceFee,
    decimal ProcessingFee,
    decimal TotalAmount,
    IReadOnlyCollection<AppliedPricingRuleResponse> AppliedRules);

public sealed record PricedLineItemResponse(
    Guid TicketTypeId,
    string TicketTypeName,
    decimal OriginalUnitPrice,
    decimal FinalUnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal DiscountAmount);

public sealed record AppliedPricingRuleResponse(
    Guid RuleId,
    string Name,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal EffectiveDiscount);

public sealed record CurrencyPolicyResponse(
    Guid EventId,
    string DefaultCurrency,
    IReadOnlyCollection<AllowedCurrencyResponse> AllowedCurrencies,
    decimal ServiceFeePercent,
    decimal ProcessingFeeFixed,
    IReadOnlyCollection<CurrencyFeeResponse> CurrencyFees);

public sealed record AllowedCurrencyResponse(string Code, string Name, decimal? ConversionRateToDefault, bool IsEnabled);
public sealed record CurrencyFeeResponse(string CurrencyCode, decimal ServiceFeePercent, decimal ProcessingFeeFixed);

public sealed record RiskAssessmentRequest(
    Guid UserId,
    Guid EventId,
    decimal TotalAmount,
    string Currency,
    string PaymentMethod,
    string? UserIpAddress,
    int TicketQuantity,
    string? DiscountCode);

public sealed record RiskAssessmentResponse(
    bool Approved,
    string RiskLevel,
    decimal RiskScore,
    IReadOnlyCollection<RiskSignalResponse> Signals,
    string? ReviewReason);

public sealed record RiskSignalResponse(string Type, string Description, decimal Score, string Severity);

