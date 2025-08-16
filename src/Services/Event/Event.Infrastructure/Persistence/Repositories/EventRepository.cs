using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EventAggregate with ETag support
/// </summary>
public class EventRepository : InventoryETagRepository<EventAggregate>, IEventRepository
{
    public EventRepository(EventDbContext context, ILogger<EventRepository> logger) : base(context, logger)
    {
    }

    public async Task<EventAggregate?> GetBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.TicketTypes)
            .Include(e => e.PricingRules)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Slug == slug && e.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.PromoterId == promoterId)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.Status == EventStatus.Published || e.Status == EventStatus.OnSale)
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> SearchEventsAsync(
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? venueId = null,
        List<string>? categories = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? hasAvailability = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryableNoTracking()
            .Include(e => e.TicketTypes)
            .Where(e => e.Status == EventStatus.Published || e.Status == EventStatus.OnSale);

        // Text search using PostgreSQL full-text search
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.SearchVector.Matches(EF.Functions.ToTsQuery("english", searchTerm)));
        }

        // Date range filter
        if (startDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EventDate <= endDate.Value);
        }

        // Venue filter
        if (venueId.HasValue)
        {
            query = query.Where(e => e.VenueId == venueId.Value);
        }

        // Categories filter
        if (categories?.Any() == true)
        {
            // This would need to be implemented based on how categories are stored
            // For now, assuming categories are stored as JSON array
            foreach (var category in categories)
            {
                query = query.Where(e => e.Categories.Contains(category));
            }
        }

        // Price range filter (based on minimum ticket price)
        if (minPrice.HasValue || maxPrice.HasValue)
        {
            query = query.Where(e => e.TicketTypes.Any(t => 
                (!minPrice.HasValue || t.BasePrice.Amount >= minPrice.Value) &&
                (!maxPrice.HasValue || t.BasePrice.Amount <= maxPrice.Value)));
        }

        // Availability filter
        if (hasAvailability.HasValue && hasAvailability.Value)
        {
            query = query.Where(e => e.TicketTypes.Any(t => t.Capacity.Available > 0));
        }

        return await query
            .OrderBy(e => e.EventDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => e.Slug == slug && e.OrganizationId == organizationId);

        if (excludeEventId.HasValue)
        {
            query = query.Where(e => e.Id != excludeEventId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetTotalEventsCountAsync(Guid? promoterId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (promoterId.HasValue)
        {
            query = query.Where(e => e.PromoterId == promoterId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    // Additional methods for the base interface (inherited from BaseRepository)

    // Event-specific query methods
    public async Task<IEnumerable<EventAggregate>> GetUpcomingEventsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(days);
        
        return await DbSet
            .Where(e => e.EventDate >= DateTime.UtcNow && e.EventDate <= cutoffDate)
            .Where(e => e.Status == EventStatus.Published || e.Status == EventStatus.OnSale)
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetEventsByVenueAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.VenueId == venueId)
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.Status == status)
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventAggregate?> GetWithFullDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.TicketTypes)
            .Include(e => e.PricingRules)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetEventsRequiringPublishAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await DbSet
            .Where(e => e.Status == EventStatus.Published)
            .Where(e => e.PublishWindow != null && 
                       e.PublishWindow.StartDate <= now && 
                       e.PublishWindow.EndDate >= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventAggregate>> GetExpiredEventsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await DbSet
            .Where(e => e.EventDate < now)
            .Where(e => e.Status == EventStatus.OnSale || e.Status == EventStatus.SoldOut)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets inventory summary with ETag for quick availability checks
    /// </summary>
    public override async Task<(int Available, int Sold, ETag ETag)?> GetInventorySummaryWithETagAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var result = await _context.Set<EventAggregate>()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.TotalCapacity,
                e.ETagValue,
                SoldCount = e.TicketTypes.Sum(tt => tt.Sold)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        var available = result.TotalCapacity - result.SoldCount;
        var etag = ETag.FromHash("EventAggregate", id.ToString(), result.ETagValue);

        return (available, result.SoldCount, etag);
    }

    /// <summary>
    /// Atomically reserves tickets with ETag validation
    /// </summary>
    public async Task<bool> TryReserveTicketsWithETagAsync(
        Guid eventId,
        Guid ticketTypeId,
        int quantity,
        ETag expectedETag,
        CancellationToken cancellationToken = default)
    {
        return await TryUpdateInventoryWithETagAsync(
            eventId,
            expectedETag,
            eventEntity =>
            {
                var ticketType = eventEntity.TicketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
                if (ticketType == null) return false;

                if (ticketType.GetAvailableQuantity() >= quantity)
                {
                    ticketType.ReserveTickets(quantity);
                    return true;
                }
                return false;
            },
            cancellationToken);
    }

    /// <summary>
    /// Atomically releases tickets with ETag validation
    /// </summary>
    public async Task<bool> TryReleaseTicketsWithETagAsync(
        Guid eventId,
        Guid ticketTypeId,
        int quantity,
        ETag expectedETag,
        CancellationToken cancellationToken = default)
    {
        return await TryUpdateInventoryWithETagAsync(
            eventId,
            expectedETag,
            eventEntity =>
            {
                var ticketType = eventEntity.TicketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
                if (ticketType == null) return false;

                if (ticketType.Sold >= quantity)
                {
                    ticketType.ReleaseTickets(quantity);
                    return true;
                }
                return false;
            },
            cancellationToken);
    }

    /// <summary>
    /// Gets events with their current inventory ETags for concurrent processing
    /// </summary>
    public async Task<Dictionary<Guid, ETag>> GetEventInventoryETagsAsync(
        IEnumerable<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.Set<EventAggregate>()
            .Where(e => eventIds.Contains(e.Id))
            .Select(e => new { e.Id, e.ETagValue })
            .ToListAsync(cancellationToken);

        return results.ToDictionary(
            r => r.Id,
            r => ETag.FromHash("EventAggregate", r.Id.ToString(), r.ETagValue)
        );
    }
}
