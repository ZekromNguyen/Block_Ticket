using Event.Domain.Entities;

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
}

/// <summary>
/// Repository interface for Event aggregate
/// </summary>
public interface IEventRepository : IRepository<EventAggregate>
{
    Task<EventAggregate?> GetBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventAggregate>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventAggregate>> GetPublishedEventsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EventAggregate>> SearchEventsAsync(
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
}

/// <summary>
/// Repository interface for Venue aggregate
/// </summary>
public interface IVenueRepository : IRepository<Venue>
{
    Task<IEnumerable<Venue>> GetByLocationAsync(string city, string? state = null, string? country = null, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEventsAsync(Guid venueId, CancellationToken cancellationToken = default);
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

/// <summary>
/// Repository interface for PricingRule aggregate
/// </summary>
public interface IPricingRuleRepository : IRepository<PricingRule>
{
    Task<IEnumerable<PricingRule>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PricingRule>> GetActiveRulesAsync(Guid eventId, DateTime effectiveDate, CancellationToken cancellationToken = default);
    Task<PricingRule?> GetByDiscountCodeAsync(string discountCode, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRulesAsync(Guid eventId, DateTime startDate, DateTime endDate, Guid? excludeRuleId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for EventSeries aggregate
/// </summary>
public interface IEventSeriesRepository : IRepository<EventSeries>
{
    Task<IEnumerable<EventSeries>> GetByPromoterId(Guid promoterId, CancellationToken cancellationToken = default);
    Task<EventSeries?> GetWithEventsAsync(Guid seriesId, CancellationToken cancellationToken = default);
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
