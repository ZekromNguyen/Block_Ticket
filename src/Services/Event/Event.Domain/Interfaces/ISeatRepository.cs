using Event.Domain.Entities;
using Event.Domain.Enums;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for Seat aggregate
/// </summary>
public interface ISeatRepository : IRepository<Seat>
{
    Task<IEnumerable<Seat>> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetBySectionAsync(Guid venueId, string section, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetByStatusAsync(Guid venueId, SeatStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetAvailableSeatsAsync(Guid venueId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetReservedSeatsAsync(Guid venueId, Guid? reservationId = null, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Seat> Seats, int TotalCount)> GetPagedByVenueAsync(
        Guid venueId,
        int pageNumber,
        int pageSize,
        string? section = null,
        string? row = null,
        SeatStatus? status = null,
        string? priceCategory = null,
        bool? isAccessible = null,
        CancellationToken cancellationToken = default);
    Task<Seat?> GetByPositionAsync(Guid venueId, string section, string row, string number, CancellationToken cancellationToken = default);
    Task<bool> PositionExistsAsync(Guid venueId, string section, string row, string number, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetExpiredReservationsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(Guid venueId, SeatStatus status, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetSeatCountsBySectionAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Seat>> GetByTicketTypeAllocationAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task BulkUpdateStatusAsync(IEnumerable<Guid> seatIds, SeatStatus newStatus, CancellationToken cancellationToken = default);
}
