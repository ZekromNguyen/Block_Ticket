using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

public class ReservationItem : BaseAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public Guid TicketTypeId { get; private set; }
    public string TicketTypeName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }

    public Reservation Reservation { get; private set; } = null!;

    private ReservationItem()
    {
    }

    public ReservationItem(Guid reservationId, Guid ticketTypeId, string ticketTypeName, decimal unitPrice, int quantity)
    {
        if (ticketTypeId == Guid.Empty)
        {
            throw new ArgumentException("Ticket type id is required", nameof(ticketTypeId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative");
        }

        ReservationId = reservationId;
        TicketTypeId = ticketTypeId;
        TicketTypeName = string.IsNullOrWhiteSpace(ticketTypeName) ? "General Admission" : ticketTypeName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        TotalPrice = unitPrice * quantity;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }

        Quantity = quantity;
        TotalPrice = UnitPrice * quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
