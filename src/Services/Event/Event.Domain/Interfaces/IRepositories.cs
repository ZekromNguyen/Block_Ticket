using Event.Domain.Entities;
using Event.Domain.Models;

namespace Event.Domain.Interfaces;

/// <summary>
/// Base repository interface for common operations
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // Additional methods needed by Application layer
    void Update(T entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);
}

/// <summary>
/// Repository interface for Event aggregate
/// </summary>
public interface IEventRepository : IRepository<EventAggregate>
{
    Task<EventAggregate?> GetBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventAggregate>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventAggregate>> GetPublishedEventsAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<EventAggregate> Events, int TotalCount)> SearchEventsAsync(
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
        CancellationToken cancellationToken = default);
    Task<bool> IsSlugUniqueAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalEventsCountAsync(Guid? promoterId = null, CancellationToken cancellationToken = default);

    // Cursor-based pagination methods
    Task<Models.CursorPagedResult<EventAggregate>> GetCursorPagedAsync<TSortKey, TSecondaryKey>(
        Models.CursorPaginationParams pagination,
        System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? predicate,
        System.Linq.Expressions.Expression<Func<EventAggregate, TSortKey>> sortKeySelector,
        System.Linq.Expressions.Expression<Func<EventAggregate, TSecondaryKey>> secondaryKeySelector,
        bool sortDescending = false,
        bool includeTotal = false,
        CancellationToken cancellationToken = default)
        where TSortKey : IComparable<TSortKey>
        where TSecondaryKey : IComparable<TSecondaryKey>;

    // Slug availability check (alias for IsSlugUniqueAsync)
    Task<bool> IsSlugAvailableAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default);

    // Additional methods needed by Application layer
    Task<EventAggregate?> GetWithFullDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Venue aggregate
/// </summary>
public interface IVenueRepository : IRepository<Venue>
{
    Task<IEnumerable<Venue>> GetByLocationAsync(string city, string? state = null, string? country = null, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEventsAsync(Guid venueId, CancellationToken cancellationToken = default);

    // Search and bulk retrieval methods
    Task<(IEnumerable<Venue> Venues, int TotalCount)> SearchVenuesAsync(
        string? searchTerm = null,
        string? city = null,
        string? state = null,
        string? country = null,
        int? minCapacity = null,
        int? maxCapacity = null,
        bool? hasAccessibility = null,
        decimal? latitude = null,
        decimal? longitude = null,
        decimal? radiusKm = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Venue>> GetByIdsAsync(IEnumerable<Guid> venueIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Reservation aggregate
/// </summary>
public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetExpiredReservationsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task<Reservation?> GetActiveReservationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveReservationForSeatsAsync(List<Guid> seatIds, CancellationToken cancellationToken = default);
}

// IPricingRuleRepository interface is defined in IPricingRuleRepository.cs

/// <summary>
/// Repository interface for EventSeries aggregate
/// </summary>
public interface IEventSeriesRepository : IRepository<EventSeries>
{
    Task<IEnumerable<EventSeries>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default);
    Task<EventSeries?> GetWithEventsAsync(Guid seriesId, CancellationToken cancellationToken = default);
    Task<EventSeries?> GetBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugUniqueAsync(string slug, Guid organizationId, Guid? excludeSeriesId = null, CancellationToken cancellationToken = default);

    // Compatibility method for Application layer
    Task<EventSeries?> GetAsync(Guid seriesId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work interface for managing transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IEventRepository Events { get; }
    IVenueRepository Venues { get; }
    IReservationRepository Reservations { get; }
    IPricingRuleRepository PricingRules { get; }
    IEventSeriesRepository EventSeries { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for caching operations
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for external pricing engine integration
/// </summary>
public interface IPricingEngineService
{
    Task<decimal> CalculateDynamicPriceAsync(Guid eventId, Guid ticketTypeId, int demandLevel, CancellationToken cancellationToken = default);
    Task<bool> IsDynamicPricingEnabledAsync(Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for seat locking operations
/// </summary>
public interface ISeatLockService
{
    Task<bool> TryLockSeatsAsync(List<Guid> seatIds, Guid reservationId, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task ReleaseSeatLocksAsync(List<Guid> seatIds, CancellationToken cancellationToken = default);
    Task ExtendSeatLocksAsync(List<Guid> seatIds, TimeSpan additionalTime, CancellationToken cancellationToken = default);
    Task<bool> AreSeatsLockedAsync(List<Guid> seatIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for inventory snapshot operations
/// </summary>
public interface IInventorySnapshotService
{
    Task<string> GetInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task InvalidateInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetAvailabilitySnapshotAsync(Guid eventId, CancellationToken cancellationToken = default);
}
