using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces;

public interface ITicketingRepository
{
    Task<Reservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken);

    Task<Reservation?> GetReservationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddReservationAsync(Reservation reservation, CancellationToken cancellationToken);

    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetTicketsByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetRefundableTicketsByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetResaleTicketsAsync(Guid? eventId, CancellationToken cancellationToken);

    Task<WaitingListEntry?> GetWaitingListEntryAsync(Guid userId, Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WaitingListEntry>> GetWaitingListEntriesAsync(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken);

    Task AddWaitingListEntryAsync(WaitingListEntry entry, CancellationToken cancellationToken);

    Task AddAdminAuditNoteAsync(AdminAuditNote note, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AdminAuditNote>> GetAdminAuditNotesAsync(Guid? ticketId, Guid? reservationId, CancellationToken cancellationToken);

    Task<ReservationPayment?> GetPaymentByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
