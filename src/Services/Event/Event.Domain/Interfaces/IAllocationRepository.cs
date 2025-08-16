using Event.Domain.Entities;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for Allocation aggregate
/// </summary>
public interface IAllocationRepository : IRepository<Allocation>
{
    Task<IEnumerable<Allocation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetByTicketTypeIdAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetActiveByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetByAccessCodeAsync(string accessCode, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Allocation> Allocations, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        string? type = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
    Task<int> GetTotalAllocatedQuantityByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableQuantityByAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<bool> HasAvailableQuantityAsync(Guid allocationId, int requestedQuantity, CancellationToken cancellationToken = default);
}
