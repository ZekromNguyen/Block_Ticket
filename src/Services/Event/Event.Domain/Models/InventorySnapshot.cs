using System;
using System.Collections.Generic;

namespace Event.Domain.Models;

public class InventorySnapshot
{
    public Guid EventId { get; set; }
    public string ETag { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public SeatStatusSummary? SeatStatusSummary { get; set; }
    public List<TicketTypeAvailability> TicketTypeAvailability { get; set; } = new();
}

public class SeatStatusSummary
{
    public int TotalSeats { get; set; }
    public int Available { get; set; }
    public int Held { get; set; }
    public int Sold { get; set; }
    public int Blocked { get; set; }
}

public class TicketTypeAvailability
{
    public Guid TicketTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Available { get; set; }
    public int Held { get; set; }
    public int Sold { get; set; }
}

