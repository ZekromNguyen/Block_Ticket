using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Allocation aggregate
/// </summary>
public class AllocationRepository : OrganizationAwareRepository<Allocation>, IAllocationRepository
{
    public AllocationRepository(
        EventDbContext context,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<AllocationRepository> logger)
        : base(context, organizationContextProvider, logger)
    {
    }

    public async Task<IEnumerable<Allocation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.EventId == eventId)
            .Include(a => a.TicketType)
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Allocation>> GetByTicketTypeIdAsync(Guid ticketTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.TicketTypeId == ticketTypeId)
            .Include(a => a.Event)
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Allocation>> GetByTypeAsync(Guid eventId, AllocationType type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.EventId == eventId && a.Type == type)
            .Include(a => a.TicketType)
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Allocation>> GetActiveAllocationsAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = DbSet
            .Where(a => a.EventId == eventId && 
                       a.IsEnabled && 
                       a.Quantity > a.UsedQuantity &&
                       (a.StartTime == null || a.StartTime <= now) &&
                       (a.EndTime == null || a.EndTime > now));

        if (ticketTypeId.HasValue)
        {
            query = query.Where(a => a.TicketTypeId == ticketTypeId.Value);
        }

        return await query
            .Include(a => a.TicketType)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Allocation?> GetByAccessCodeAsync(string accessCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
            return null;

        return await DbSet
            .Where(a => a.AccessCode == accessCode)
            .Include(a => a.Event)
            .Include(a => a.TicketType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AccessCodeExistsAsync(string accessCode, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
            return false;

        var query = DbSet.Where(a => a.AccessCode == accessCode);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Allocation>> GetByCustomerSegmentAsync(Guid eventId, string customerSegment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerSegment))
            return Enumerable.Empty<Allocation>();

        // Note: This is a simplified implementation. In a real scenario, you might need
        // to use a more sophisticated approach for JSON array queries
        return await DbSet
            .Where(a => a.EventId == eventId && 
                       a.IsEnabled &&
                       a.AllowedCustomerSegments.Contains(customerSegment))
            .Include(a => a.TicketType)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Allocation> Allocations, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        AllocationType? type = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.EventId == eventId);

        // Apply filters
        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(a => a.IsEnabled && 
                                        a.Quantity > a.UsedQuantity &&
                                        (a.StartTime == null || a.StartTime <= now) &&
                                        (a.EndTime == null || a.EndTime > now));
            }
            else
            {
                query = query.Where(a => !a.IsEnabled || 
                                        a.Quantity <= a.UsedQuantity ||
                                        (a.StartTime != null && a.StartTime > now) ||
                                        (a.EndTime != null && a.EndTime <= now));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(searchTerm) ||
                                    (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                                    (a.AccessCode != null && a.AccessCode.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var allocations = await query
            .Include(a => a.TicketType)
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (allocations, totalCount);
    }

    public async Task<Dictionary<AllocationType, int>> GetAllocationCountsByTypeAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.EventId == eventId)
            .GroupBy(a => a.Type)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<int> GetTotalAllocatedQuantityAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.EventId == eventId);

        if (ticketTypeId.HasValue)
        {
            query = query.Where(a => a.TicketTypeId == ticketTypeId.Value);
        }

        return await query.SumAsync(a => a.Quantity, cancellationToken);
    }

    public async Task<int> GetTotalUsedQuantityAsync(Guid eventId, Guid? ticketTypeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.EventId == eventId);

        if (ticketTypeId.HasValue)
        {
            query = query.Where(a => a.TicketTypeId == ticketTypeId.Value);
        }

        return await query.SumAsync(a => a.UsedQuantity, cancellationToken);
    }
}
