using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

/// <summary>
/// Read-model projection that aggregates ticketing analytics per event.
/// Updated asynchronously by MassTransit consumers that project purchase,
/// refund, transfer, resale, and verification events.
/// </summary>
public class EventAnalytics : BaseAuditableEntity
{
    public Guid EventId { get; private set; }
    public string EventName { get; private set; } = string.Empty;
    public int TotalTicketsSold { get; private set; }
    public int TotalTicketsRefunded { get; private set; }
    public int TotalTicketsCancelled { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal TotalRefundAmount { get; private set; }
    public int TotalResales { get; private set; }
    public decimal TotalResaleVolume { get; private set; }
    public int TotalScans { get; private set; }
    public int SuccessfulScans { get; private set; }
    public int FailedScans { get; private set; }

    private readonly List<TicketTypeAnalyticsEntry> _ticketTypeBreakdown = new();
    public IReadOnlyCollection<TicketTypeAnalyticsEntry> TicketTypeBreakdown => _ticketTypeBreakdown.AsReadOnly();

    private readonly List<DailySalesEntry> _dailySales = new();
    public IReadOnlyCollection<DailySalesEntry> DailySales => _dailySales.AsReadOnly();

    private EventAnalytics() { }

    public EventAnalytics(Guid eventId, string eventName)
    {
        EventId = eventId;
        EventName = eventName;
    }

    public void RecordSale(string ticketTypeName, decimal price, int quantity)
    {
        TotalTicketsSold += quantity;
        TotalRevenue += price * quantity;
        UpdatedAt = DateTime.UtcNow;

        var entry = _ticketTypeBreakdown.FirstOrDefault(e => e.TicketTypeName == ticketTypeName);
        if (entry is null)
        {
            entry = new TicketTypeAnalyticsEntry(ticketTypeName);
            _ticketTypeBreakdown.Add(entry);
        }
        entry.AddSale(price, quantity);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daily = _dailySales.FirstOrDefault(d => d.Date == today);
        if (daily is null)
        {
            daily = new DailySalesEntry(today);
            _dailySales.Add(daily);
        }
        daily.AddSale(price, quantity);
    }

    public void RecordRefund(string ticketTypeName, decimal amount)
    {
        TotalTicketsRefunded++;
        TotalRefundAmount += amount;
        UpdatedAt = DateTime.UtcNow;

        var entry = _ticketTypeBreakdown.FirstOrDefault(e => e.TicketTypeName == ticketTypeName);
        entry?.AddRefund(amount);
    }

    public void RecordResale(decimal price)
    {
        TotalResales++;
        TotalResaleVolume += price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordScan(bool success)
    {
        TotalScans++;
        if (success) SuccessfulScans++; else FailedScans++;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal AttendanceRate => TotalTicketsSold == 0 ? 0m : Math.Round((decimal)SuccessfulScans / TotalTicketsSold * 100, 1);
}

public class TicketTypeAnalyticsEntry
{
    public Guid Id { get; private set; }
    public string TicketTypeName { get; private set; } = string.Empty;
    public int Sold { get; private set; }
    public int Refunded { get; private set; }
    public decimal Revenue { get; private set; }

    private TicketTypeAnalyticsEntry() { }

    public TicketTypeAnalyticsEntry(string ticketTypeName)
    {
        Id = Guid.NewGuid();
        TicketTypeName = ticketTypeName;
    }

    public void AddSale(decimal price, int quantity)
    {
        Sold += quantity;
        Revenue += price * quantity;
    }

    public void AddRefund(decimal amount)
    {
        Refunded++;
        Revenue -= amount;
    }

    public decimal AveragePrice => Sold == 0 ? 0m : Math.Round(Revenue / Sold, 2);
}

public class DailySalesEntry
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int TicketsSold { get; private set; }
    public decimal Revenue { get; private set; }
    public int Refunds { get; private set; }

    private DailySalesEntry() { }

    public DailySalesEntry(DateOnly date)
    {
        Id = Guid.NewGuid();
        Date = date;
    }

    public void AddSale(decimal price, int quantity)
    {
        TicketsSold += quantity;
        Revenue += price * quantity;
    }

    public void AddRefund(decimal amount)
    {
        Refunds++;
        Revenue -= amount;
    }
}
