using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence;

public sealed class TicketingRepository : ITicketingRepository
{
    private readonly TicketingDbContext _context;

    public TicketingRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public Task<Reservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        return _context.Reservations
            .Include(reservation => reservation.Items)
            .Include(reservation => reservation.Payments)
            .Include(reservation => reservation.Tickets)
            .FirstOrDefaultAsync(reservation => reservation.Id == reservationId && !reservation.IsDeleted, cancellationToken);
    }

    public Task<Reservation?> GetReservationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return _context.Reservations
            .Include(reservation => reservation.Items)
            .Include(reservation => reservation.Payments)
            .Include(reservation => reservation.Tickets)
            .FirstOrDefaultAsync(reservation => reservation.IdempotencyKey == idempotencyKey && !reservation.IsDeleted, cancellationToken);
    }

    public async Task AddReservationAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _context.Reservations.AddAsync(reservation, cancellationToken);
    }

    public Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        return _context.Tickets.FirstOrDefaultAsync(ticket => ticket.Id == ticketId && !ticket.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ticket>> GetTicketsByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .Where(ticket => ticket.UserId == userId && !ticket.IsDeleted)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
