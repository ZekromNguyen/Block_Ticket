using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces;

public interface ITicketingRepository
{
    Task<Reservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken);

    Task<Reservation?> GetReservationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddReservationAsync(Reservation reservation, CancellationToken cancellationToken);

    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Ticket>> GetTicketsByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
