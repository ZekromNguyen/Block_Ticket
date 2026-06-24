using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Dtos;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly TicketingDbContext _db;

    public AnalyticsController(TicketingDbContext db) => _db = db;

    [HttpGet("events/{eventId:guid}")]
    [ProducesResponseType(typeof(EventAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventAnalytics(Guid eventId, CancellationToken ct)
    {
        var analytics = await _db.EventAnalytics
            .Include(a => a.TicketTypeBreakdown)
            .Include(a => a.DailySales)
            .FirstOrDefaultAsync(a => a.EventId == eventId, ct);

        if (analytics is null)
        {
            return NotFound(new { message = $"Analytics for event {eventId} not found" });
        }

        var response = new EventAnalyticsDto(
            analytics.EventId,
            analytics.EventName,
            analytics.UpdatedAt ?? analytics.CreatedAt,
            analytics.TotalTicketsSold,
            analytics.TotalTicketsRefunded,
            analytics.TotalTicketsCancelled,
            analytics.TotalRevenue,
            analytics.TotalRefundAmount,
            analytics.TotalResales,
            analytics.TotalResaleVolume,
            analytics.TotalScans,
            analytics.SuccessfulScans,
            analytics.FailedScans,
            analytics.AttendanceRate,
            analytics.TicketTypeBreakdown.Select(t => new TicketTypeAnalytics(
                Guid.Empty, t.TicketTypeName, t.Sold, t.Refunded, 0, t.Revenue, t.AveragePrice)).ToArray(),
            analytics.DailySales.OrderByDescending(d => d.Date).Select(d => new DailySalesPoint(
                d.Date, d.TicketsSold, d.Revenue, d.Refunds)).ToArray(),
            Array.Empty<RevenueBySource>());

        return Ok(response);
    }

    [HttpGet("platform")]
    [ProducesResponseType(typeof(PlatformAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformAnalytics(CancellationToken ct)
    {
        var allAnalytics = await _db.EventAnalytics.ToListAsync(ct);

        var topEvents = allAnalytics
            .OrderByDescending(a => a.TotalRevenue)
            .Take(10)
            .Select(a => new TopEventAnalytics(
                a.EventId, a.EventName, a.TotalTicketsSold, a.TotalRevenue, a.AttendanceRate))
            .ToArray();

        var response = new PlatformAnalyticsDto(
            DateTime.UtcNow,
            allAnalytics.Count,
            0,
            allAnalytics.Sum(a => a.TotalTicketsSold),
            allAnalytics.Sum(a => a.TotalRevenue),
            allAnalytics.Sum(a => a.TotalResales),
            allAnalytics.Sum(a => a.TotalScans),
            0m,
            topEvents);

        return Ok(response);
    }
}
