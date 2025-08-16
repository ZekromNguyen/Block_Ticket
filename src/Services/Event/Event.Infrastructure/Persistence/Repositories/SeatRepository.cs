using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Seat
/// </summary>
public class SeatRepository : BaseRepository<Seat>, ISeatRepository
{
    public SeatRepository(EventDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Seat>> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VenueId == venueId)
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetBySectionAsync(Guid venueId, string section, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VenueId == venueId && s.Position.Section == section)
            .OrderBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetByStatusAsync(Guid venueId, SeatStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VenueId == venueId && s.Status == status)
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetAvailableSeatsAsync(Guid venueId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(s => s.VenueId == venueId && s.Status == SeatStatus.Available);

        if (ticketTypeId.HasValue)
        {
            query = query.Where(s => s.AllocatedToTicketTypeId == ticketTypeId.Value || s.AllocatedToTicketTypeId == null);
        }

        return await query
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetReservedSeatsAsync(Guid venueId, Guid? reservationId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(s => s.VenueId == venueId && s.Status == SeatStatus.Reserved);

        if (reservationId.HasValue)
        {
            query = query.Where(s => s.CurrentReservationId == reservationId.Value);
        }

        return await query
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Seat> Seats, int TotalCount)> GetPagedByVenueAsync(
        Guid venueId,
        int pageNumber,
        int pageSize,
        string? section = null,
        string? row = null,
        SeatStatus? status = null,
        string? priceCategory = null,
        bool? isAccessible = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(s => s.VenueId == venueId);

        // Apply filters
        if (!string.IsNullOrEmpty(section))
        {
            query = query.Where(s => s.Position.Section.Contains(section));
        }

        if (!string.IsNullOrEmpty(row))
        {
            query = query.Where(s => s.Position.Row.Contains(row));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(priceCategory))
        {
            query = query.Where(s => s.PriceCategory == priceCategory);
        }

        if (isAccessible.HasValue)
        {
            query = query.Where(s => s.IsAccessible == isAccessible.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging and ordering
        var seats = await query
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (seats, totalCount);
    }

    public async Task<Seat?> GetByPositionAsync(Guid venueId, string section, string row, string number, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.VenueId == venueId && 
                                    s.Position.Section == section && 
                                    s.Position.Row == row && 
                                    s.Position.Number == number, 
                               cancellationToken);
    }

    public async Task<bool> PositionExistsAsync(Guid venueId, string section, string row, string number, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(s => s.VenueId == venueId && 
                                   s.Position.Section == section && 
                                   s.Position.Row == row && 
                                   s.Position.Number == number);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetExpiredReservationsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.Status == SeatStatus.Reserved && 
                       s.ReservedUntil.HasValue && 
                       s.ReservedUntil.Value <= cutoffTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(Guid venueId, SeatStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(s => s.VenueId == venueId && s.Status == status, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetSeatCountsBySectionAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VenueId == venueId)
            .GroupBy(s => s.Position.Section)
            .Select(g => new { Section = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Section, x => x.Count, cancellationToken);
    }

    public async Task<IEnumerable<Seat>> GetByTicketTypeAllocationAsync(Guid ticketTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.AllocatedToTicketTypeId == ticketTypeId)
            .OrderBy(s => s.Position.Section)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkUpdateStatusAsync(IEnumerable<Guid> seatIds, SeatStatus newStatus, CancellationToken cancellationToken = default)
    {
        var seatIdsList = seatIds.ToList();
        if (!seatIdsList.Any())
            return;

        await DbSet
            .Where(s => seatIdsList.Contains(s.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(seat => seat.Status, newStatus), cancellationToken);
    }
}
