using Event.Domain.Entities;
using Event.Domain.Enums;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for Allocation aggregate
/// </summary>
public interface IAllocationRepository : IRepository<Allocation>
{
    Task<IEnumerable<Allocation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetByTicketTypeIdAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetByTypeAsync(Guid eventId, AllocationType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetActiveAllocationsAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default);
    Task<Allocation?> GetByAccessCodeAsync(string accessCode, CancellationToken cancellationToken = default);
    Task<bool> AccessCodeExistsAsync(string accessCode, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Allocation>> GetByCustomerSegmentAsync(Guid eventId, string customerSegment, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Allocation> Allocations, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        AllocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<Dictionary<AllocationType, int>> GetAllocationCountsByTypeAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetTotalAllocatedQuantityAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalUsedQuantityAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default);
}
