using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EventAggregate
/// </summary>
public class EventRepository : OrganizationAwareRepository<EventAggregate>, IEventRepository
{
    public EventRepository(
        EventDbContext context,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<EventRepository> logger)
        : base(context, organizationContextProvider, logger)
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

    public async Task<(IEnumerable<EventAggregate> Events, int TotalCount)> SearchEventsAsync(
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

        // Get total count before applying pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and get results
        var events = await query
            .OrderBy(e => e.EventDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
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

    public async Task<Event.Domain.Models.CursorPagedResult<EventAggregate>> GetCursorPagedAsync<TSortKey, TSecondaryKey>(
        Event.Domain.Models.CursorPaginationParams pagination,
        System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? predicate,
        System.Linq.Expressions.Expression<Func<EventAggregate, TSortKey>> sortKeySelector,
        System.Linq.Expressions.Expression<Func<EventAggregate, TSecondaryKey>> secondaryKeySelector,
        bool sortDescending = false,
        bool includeTotal = false,
        CancellationToken cancellationToken = default)
        where TSortKey : IComparable<TSortKey>
        where TSecondaryKey : IComparable<TSecondaryKey>
    {
        // TODO: Implement cursor-based pagination
        // This is a complex implementation that would require cursor parsing and comparison logic
        // For now, return empty result to satisfy interface
        return new Event.Domain.Models.CursorPagedResult<EventAggregate>
        {
            Items = new List<EventAggregate>(),
            HasNextPage = false,
            HasPreviousPage = false,
            PageSize = pagination.EffectivePageSize
        };
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        return await IsSlugUniqueAsync(slug, organizationId, excludeEventId, cancellationToken);
    }
}
