using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Event.Domain.ValueObjects;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EventSeries
/// </summary>
public class EventSeriesRepository : BaseRepository<EventSeries>, IEventSeriesRepository
{
    public EventSeriesRepository(EventDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<EventSeries>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.PromoterId == promoterId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventSeries?> GetWithEventsAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.EventIds) // This would need to be properly configured in EF Core
            .FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken);
    }

    public async Task<EventSeries?> GetBySlugAsync(string slug, Guid organizationId, bool includeEvents, CancellationToken cancellationToken = default)
    {
        // Note: includeEvents is not used here as there's no direct navigation property.
        // Event loading should be handled in the application layer if required.
        var slugValueObject = new Slug(slug);
        return await DbSet
            .FirstOrDefaultAsync(s => s.Slug == slugValueObject && s.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid organizationId, Guid? excludeSeriesId = null, CancellationToken cancellationToken = default)
    {
        var slugValueObject = new Slug(slug);
        var query = DbSet.Where(s => s.Slug == slugValueObject && s.OrganizationId == organizationId);

        if (excludeSeriesId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSeriesId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<EventSeries?> GetAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(seriesId, cancellationToken);
    }

    // Additional methods for the base interface (inherited from BaseRepository)

    // EventSeries-specific query methods
    public async Task<IEnumerable<EventSeries>> GetActiveSeriesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<EventSeries>> GetSeriesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.SeriesStartDate <= endDate && s.SeriesEndDate >= startDate)
            .OrderBy(s => s.SeriesStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetEventCountInSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        var series = await GetByIdAsync(seriesId, cancellationToken);
        return series?.EventIds.Count ?? 0;
    }

    public async Task<bool> CanAddEventToSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
    {
        var series = await GetByIdAsync(seriesId, cancellationToken);
        if (series == null) return false;

        var currentEventCount = series.EventIds.Count;
        return series.MaxEvents == null || currentEventCount < series.MaxEvents;
    }

    public async Task<IEnumerable<EventSeries>> GetSeriesRequiringMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await DbSet
            .Where(s => s.IsActive && s.SeriesEndDate.HasValue && s.SeriesEndDate.Value < now)
            .ToListAsync(cancellationToken);
    }
}
