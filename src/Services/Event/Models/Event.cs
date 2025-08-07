using Shared.Common.Models;

namespace Event.Api.Models;

public class Event : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime SaleStartDate { get; set; }
    public DateTime SaleEndDate { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public decimal TicketPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public Guid PromoterId { get; set; }
    
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
}

public class TicketType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
}

public enum EventStatus
{
    Draft,
    Published,
    OnSale,
    SoldOut,
    Completed,
    Cancelled
}
