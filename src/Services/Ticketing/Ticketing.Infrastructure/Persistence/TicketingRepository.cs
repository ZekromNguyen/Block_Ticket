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

    public async Task<IReadOnlyCollection<Ticket>> GetRefundableTicketsByEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .Where(ticket => ticket.EventId == eventId &&
                !ticket.IsDeleted &&
                (ticket.Status == TicketStatus.Active ||
                    ticket.Status == TicketStatus.OnResale ||
                    ticket.Status == TicketStatus.PendingMint))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Ticket>> GetResaleTicketsAsync(Guid? eventId, CancellationToken cancellationToken)
    {
        var query = _context.Tickets.Where(ticket => ticket.Status == TicketStatus.OnResale && !ticket.IsDeleted);
        if (eventId.HasValue)
        {
            query = query.Where(ticket => ticket.EventId == eventId.Value);
        }

        return await query.OrderBy(ticket => ticket.ListedForResaleAt).ToListAsync(cancellationToken);
    }

    public Task<WaitingListEntry?> GetWaitingListEntryAsync(Guid userId, Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken)
    {
        return _context.WaitingListEntries.FirstOrDefaultAsync(
            entry => entry.UserId == userId && entry.EventId == eventId && entry.TicketTypeId == ticketTypeId && !entry.IsDeleted,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<WaitingListEntry>> GetWaitingListEntriesAsync(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken)
    {
        return await _context.WaitingListEntries
            .Where(entry => entry.EventId == eventId && entry.TicketTypeId == ticketTypeId && !entry.IsDeleted)
            .OrderBy(entry => entry.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddWaitingListEntryAsync(WaitingListEntry entry, CancellationToken cancellationToken)
    {
        await _context.WaitingListEntries.AddAsync(entry, cancellationToken);
    }

    public async Task AddAdminAuditNoteAsync(AdminAuditNote note, CancellationToken cancellationToken)
    {
        await _context.AdminAuditNotes.AddAsync(note, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminAuditNote>> GetAdminAuditNotesAsync(Guid? ticketId, Guid? reservationId, CancellationToken cancellationToken)
    {
        var query = _context.AdminAuditNotes.AsQueryable();
        if (ticketId.HasValue)
        {
            query = query.Where(note => note.TicketId == ticketId.Value);
        }

        if (reservationId.HasValue)
        {
            query = query.Where(note => note.ReservationId == reservationId.Value);
        }

        return await query.OrderByDescending(note => note.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<ReservationPayment?> GetPaymentByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        return _context.ReservationPayments
            .OrderByDescending(payment => payment.CreatedAt)
            .FirstOrDefaultAsync(payment => payment.ReservationId == reservationId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
