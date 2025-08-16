using Event.Domain.Entities;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for TicketType aggregate
/// </summary>
public interface ITicketTypeRepository : IRepository<TicketType>
{
    Task<IEnumerable<TicketType>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<TicketType?> GetByCodeAsync(Guid eventId, string code, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid eventId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketType>> GetVisibleByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketType>> GetOnSaleByEventIdAsync(Guid eventId, DateTime currentTime, CancellationToken cancellationToken = default);
    Task<(IEnumerable<TicketType> TicketTypes, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        bool? isVisible = null,
        bool? isOnSale = null,
        CancellationToken cancellationToken = default);
    Task<int> GetTotalCapacityByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableCapacityByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
