using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

public class AdminAuditNote : BaseAuditableEntity
{
    public Guid? TicketId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string AdminUserId { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;

    private AdminAuditNote()
    {
    }

    public AdminAuditNote(Guid? ticketId, Guid? reservationId, string action, string adminUserId, string note)
    {
        TicketId = ticketId;
        ReservationId = reservationId;
        Action = action;
        AdminUserId = adminUserId;
        Note = note;
    }
}
