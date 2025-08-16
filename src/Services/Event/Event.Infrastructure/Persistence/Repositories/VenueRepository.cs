using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Venue
/// </summary>
public class VenueRepository : BaseRepository<Venue>, IVenueRepository
{
    public VenueRepository(EventDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Venue>> GetByLocationAsync(string city, string? state = null, string? country = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(v => v.Address.City.ToLower() == city.ToLower());

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(v => v.Address.State.ToLower() == state.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(v => v.Address.Country.ToLower() == country.ToLower());
        }

        return await query
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Venue?> GetWithSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(v => v.Seats)
            .FirstOrDefaultAsync(v => v.Id == venueId, cancellationToken);
    }

    public async Task<bool> HasActiveEventsAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await Context.Events
            .AnyAsync(e => e.VenueId == venueId && 
                          e.EventDate >= now && 
                          (e.Status == EventStatus.Published || 
                           e.Status == EventStatus.OnSale || 
                           e.Status == EventStatus.SoldOut), 
                     cancellationToken);
    }

    // Additional methods for the base interface (inherited from BaseRepository)

    // Venue-specific query methods
    public async Task<IEnumerable<Venue>> GetVenuesWithSeatMapsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.HasSeatMap)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Venue>> GetVenuesByCapacityRangeAsync(int minCapacity, int maxCapacity, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.TotalCapacity >= minCapacity && v.TotalCapacity <= maxCapacity)
            .OrderBy(v => v.TotalCapacity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Venue>> SearchVenuesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync(cancellationToken);
        }

        var lowerSearchTerm = searchTerm.ToLower();

        return await DbSet
            .Where(v => v.Name.ToLower().Contains(lowerSearchTerm) ||
                       v.Address.City.ToLower().Contains(lowerSearchTerm) ||
                       v.Address.State.ToLower().Contains(lowerSearchTerm) ||
                       (v.Description != null && v.Description.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Venue>> GetVenuesNearLocationAsync(double latitude, double longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        // Using Haversine formula for distance calculation
        // Note: This is a simplified version. For production, consider using PostGIS or similar
        var venues = await DbSet
            .Where(v => v.Address.Coordinates != null)
            .ToListAsync(cancellationToken);

        return venues
            .Where(v => CalculateDistance(latitude, longitude,
                                        v.Address.Coordinates!.Latitude,
                                        v.Address.Coordinates.Longitude) <= radiusKm)
            .OrderBy(v => CalculateDistance(latitude, longitude,
                                          v.Address.Coordinates!.Latitude,
                                          v.Address.Coordinates.Longitude))
            .ToList();
    }

    public async Task<Dictionary<Guid, int>> GetVenueEventCountsAsync(IEnumerable<Guid> venueIds, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await Context.Set<EventAggregate>()
            .Where(e => venueIds.Contains(e.VenueId) && e.EventDate >= now)
            .GroupBy(e => e.VenueId)
            .Select(g => new { VenueId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VenueId, x => x.Count, cancellationToken);
    }

    public async Task<IEnumerable<Venue>> GetVenuesWithRecentSeatMapUpdatesAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        return await DbSet
            .Where(v => v.HasSeatMap && 
                       v.SeatMapLastUpdated.HasValue && 
                       v.SeatMapLastUpdated.Value >= cutoffDate)
            .OrderByDescending(v => v.SeatMapLastUpdated)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalSeatsCountAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await Context.Seats
            .CountAsync(s => s.VenueId == venueId, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetSeatCountsBySectionAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await Context.Seats
            .Where(s => s.VenueId == venueId)
            .GroupBy(s => s.Position.Section)
            .Select(g => new { Section = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Section, x => x.Count, cancellationToken);
    }

    public async Task<IEnumerable<Venue>> GetVenuesByTimeZoneAsync(string timeZone, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.TimeZone == timeZone)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
