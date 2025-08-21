using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using DomainModels = Event.Domain.Models;

namespace Event.Application.Features.Events.Queries.SearchEvents;

/// <summary>
/// Handler for cursor-based event search queries
/// </summary>
public class SearchEventsCursorQueryHandler : IRequestHandler<SearchEventsCursorQuery, DomainModels.CursorPagedResult<EventCatalogDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SearchEventsCursorQueryHandler> _logger;

    public SearchEventsCursorQueryHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        ICacheService cacheService,
        ILogger<SearchEventsCursorQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<DomainModels.CursorPagedResult<EventCatalogDto>> Handle(SearchEventsCursorQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cursor-based search for events with term: {SearchTerm}, Sort: {SortBy}", 
            request.SearchTerm, request.SortBy);

        // Generate cache key if not requesting total count (expensive operation)
        string? cacheKey = null;
        if (!request.Pagination.IncludeTotalCount)
        {
            cacheKey = GenerateSearchCacheKey(request);
            
            // Try to get from cache first
            var cachedResult = await _cacheService.GetAsync<DomainModels.CursorPagedResult<EventCatalogDto>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cursor search results found in cache for key: {CacheKey}", cacheKey);
                return cachedResult;
            }
        }

        // Build filter predicate
        var predicate = BuildFilterPredicate(request);

        // Get cursor-paginated results with appropriate sorting
        var result = await GetCursorPagedEvents(request, predicate, cancellationToken);

        // Get venue information for the events
        var venueIds = result.Items.Select(e => e.VenueId).Distinct().ToList();
        var venues = await GetVenuesSummary(venueIds, cancellationToken);

        // Convert to catalog DTOs
        var catalogDtos = result.Items.Select(e => MapToCatalogDto(e, venues)).ToList();

        var finalResult = new DomainModels.CursorPagedResult<EventCatalogDto>
        {
            Items = catalogDtos,
            NextCursor = result.NextCursor,
            PreviousCursor = result.PreviousCursor,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };

        // Cache the result if not including total count
        if (!string.IsNullOrEmpty(cacheKey))
        {
            await _cacheService.SetAsync(cacheKey, finalResult, TimeSpan.FromMinutes(2), cancellationToken);
        }

        _logger.LogInformation("Found {Count} events matching cursor search criteria", catalogDtos.Count);
        return finalResult;
    }

    private async Task<DomainModels.CursorPagedResult<EventAggregate>> GetCursorPagedEvents(
        SearchEventsCursorQuery request,
        Expression<Func<EventAggregate, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return request.SortBy?.ToLowerInvariant() switch
        {
            "eventdate" => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.EventDate,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "createdat" => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.CreatedAt,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "title" => await _eventRepository.GetCursorPagedAsync<string, Guid>(
                request.Pagination,
                predicate,
                e => e.Title,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "status" => await _eventRepository.GetCursorPagedAsync<int, Guid>(
                request.Pagination,
                predicate,
                e => (int)e.Status,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            // Default to EventDate sorting
            _ => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.EventDate,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken)
        };
    }

    private Expression<Func<EventAggregate, bool>> BuildFilterPredicate(SearchEventsCursorQuery request)
    {
        Expression<Func<EventAggregate, bool>> predicate = e => true;

        // Always filter to published events for public search
        predicate = predicate.And(e => e.Status == EventStatus.Published || e.Status == EventStatus.OnSale);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = predicate.And(e => 
                e.Title.ToLower().Contains(searchTerm) ||
                e.Description.ToLower().Contains(searchTerm) ||
                e.Categories.Any(c => c.ToLower().Contains(searchTerm)));
        }

        if (request.StartDate.HasValue)
        {
            predicate = predicate.And(e => e.EventDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            predicate = predicate.And(e => e.EventDate <= request.EndDate.Value);
        }

        if (request.VenueId.HasValue)
        {
            predicate = predicate.And(e => e.VenueId == request.VenueId.Value);
        }

        if (request.Categories?.Any() == true)
        {
            predicate = predicate.And(e => request.Categories.Any(cat => e.Categories.Contains(cat)));
        }

        if (request.MinPrice.HasValue)
        {
            predicate = predicate.And(e => e.TicketTypes.Any(tt => tt.BasePrice.Amount >= request.MinPrice.Value));
        }

        if (request.MaxPrice.HasValue)
        {
            predicate = predicate.And(e => e.TicketTypes.Any(tt => tt.BasePrice.Amount <= request.MaxPrice.Value));
        }

        if (request.HasAvailability.HasValue && request.HasAvailability.Value)
        {
            predicate = predicate.And(e => e.TicketTypes.Any(tt => tt.Capacity.Available > 0));
        }

        if (request.Status.HasValue)
        {
            predicate = predicate.And(e => e.Status == request.Status.Value);
        }

        return predicate;
    }

    private async Task<Dictionary<Guid, VenueSummaryDto>> GetVenuesSummary(
        List<Guid> venueIds, 
        CancellationToken cancellationToken)
    {
        if (!venueIds.Any()) return new Dictionary<Guid, VenueSummaryDto>();

        var venues = await _venueRepository.GetByIdsAsync(venueIds, cancellationToken);
        return venues.ToDictionary(v => v.Id, v => new VenueSummaryDto
        {
            Id = v.Id,
            Name = v.Name,
            City = v.Address?.City ?? string.Empty,
            State = v.Address?.State ?? string.Empty,
            Country = v.Address?.Country ?? string.Empty
        });
    }

    private EventCatalogDto MapToCatalogDto(EventAggregate eventEntity, Dictionary<Guid, VenueSummaryDto> venues)
    {
        venues.TryGetValue(eventEntity.VenueId, out var venue);

        return new EventCatalogDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            Slug = eventEntity.Slug?.Value ?? string.Empty,
            EventDate = eventEntity.EventDate,
            TimeZone = eventEntity.TimeZone?.Value ?? string.Empty,
            Status = eventEntity.Status.ToString(),
            ImageUrl = eventEntity.ImageUrl,
            BannerUrl = eventEntity.BannerUrl,
            Categories = eventEntity.Categories.ToList(),
            Tags = eventEntity.Tags.ToList(),
            Venue = venue,
            TicketTypes = eventEntity.TicketTypes.Select(tt => new TicketTypeSummaryDto
            {
                Id = tt.Id,
                Name = tt.Name,
                Description = tt.Description,
                BasePrice = new MoneyDto
                {
                    Amount = tt.BasePrice.Amount,
                    Currency = tt.BasePrice.Currency
                },
                AvailableQuantity = tt.Capacity.Available,
                TotalCapacity = tt.Capacity.Total,
                IsAvailable = tt.IsAvailable()
            }).ToList(),
            MinPrice = eventEntity.TicketTypes
                .Where(tt => tt.IsAvailable())
                .Select(tt => tt.BasePrice.Amount)
                .DefaultIfEmpty(0)
                .Min(),
            MaxPrice = eventEntity.TicketTypes
                .Where(tt => tt.IsAvailable())
                .Select(tt => tt.BasePrice.Amount)
                .DefaultIfEmpty(0)
                .Max(),
            AvailableTickets = eventEntity.TicketTypes.Sum(tt => tt.Capacity.Available),
            TotalCapacity = eventEntity.TotalCapacity,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt ?? DateTime.UtcNow
        };
    }

    private string GenerateSearchCacheKey(SearchEventsCursorQuery request)
    {
        var keyData = new
        {
            SearchTerm = request.SearchTerm?.ToLowerInvariant(),
            request.StartDate,
            request.EndDate,
            request.VenueId,
            City = request.City?.ToLowerInvariant(),
            Categories = request.Categories?.OrderBy(c => c).ToList(),
            request.MinPrice,
            request.MaxPrice,
            request.HasAvailability,
            request.Status,
            request.SortBy,
            request.SortDescending,
            Cursor = request.Pagination.EffectiveCursor,
            PageSize = request.Pagination.EffectivePageSize,
            Direction = request.Pagination.Direction
        };

        return $"events:search:cursor:{keyData.GetHashCode():X8}";
    }
}

/// <summary>
/// Handler for cursor-based get events queries
/// </summary>
public class GetEventsCursorQueryHandler : IRequestHandler<GetEventsCursorQuery, DomainModels.CursorPagedResult<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventsCursorQueryHandler> _logger;

    public GetEventsCursorQueryHandler(
        IEventRepository eventRepository,
        ICacheService cacheService,
        ILogger<GetEventsCursorQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<DomainModels.CursorPagedResult<EventDto>> Handle(GetEventsCursorQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cursor-based get events - Promoter: {PromoterId}, Venue: {VenueId}, Status: {Status}",
            request.PromoterId, request.VenueId, request.Status);

        // Generate cache key if not requesting total count
        string? cacheKey = null;
        if (!request.Pagination.IncludeTotalCount)
        {
            cacheKey = GenerateGetEventsCacheKey(request);

            var cachedResult = await _cacheService.GetAsync<DomainModels.CursorPagedResult<EventDto>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cursor events found in cache for key: {CacheKey}", cacheKey);
                return cachedResult;
            }
        }

        // Build filter predicate
        var predicate = BuildFilterPredicate(request);

        // Get cursor-paginated results
        var result = await GetCursorPagedEvents(request, predicate, cancellationToken);

        // Convert to DTOs
        var eventDtos = result.Items.Select(MapToEventDto).ToList();

        var finalResult = new DomainModels.CursorPagedResult<EventDto>
        {
            Items = eventDtos,
            NextCursor = result.NextCursor,
            PreviousCursor = result.PreviousCursor,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };

        // Cache the result if not including total count
        if (!string.IsNullOrEmpty(cacheKey))
        {
            await _cacheService.SetAsync(cacheKey, finalResult, TimeSpan.FromMinutes(2), cancellationToken);
        }

        _logger.LogInformation("Found {Count} events matching cursor criteria", eventDtos.Count);
        return finalResult;
    }

    private async Task<DomainModels.CursorPagedResult<EventAggregate>> GetCursorPagedEvents(
        GetEventsCursorQuery request,
        Expression<Func<EventAggregate, bool>> predicate,
        CancellationToken cancellationToken)
    {
        return request.SortBy?.ToLowerInvariant() switch
        {
            "eventdate" => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.EventDate,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "createdat" => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.CreatedAt,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "title" => await _eventRepository.GetCursorPagedAsync<string, Guid>(
                request.Pagination,
                predicate,
                e => e.Title,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            "status" => await _eventRepository.GetCursorPagedAsync<int, Guid>(
                request.Pagination,
                predicate,
                e => (int)e.Status,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken),

            // Default to EventDate sorting
            _ => await _eventRepository.GetCursorPagedAsync<DateTime, Guid>(
                request.Pagination,
                predicate,
                e => e.EventDate,
                e => e.Id,
                request.SortDescending,
                false,
                cancellationToken)
        };
    }

    private Expression<Func<EventAggregate, bool>> BuildFilterPredicate(GetEventsCursorQuery request)
    {
        Expression<Func<EventAggregate, bool>> predicate = e => true;

        if (request.PromoterId.HasValue)
        {
            predicate = predicate.And(e => e.PromoterId == request.PromoterId.Value);
        }

        if (request.VenueId.HasValue)
        {
            predicate = predicate.And(e => e.VenueId == request.VenueId.Value);
        }

        if (request.OrganizationId.HasValue)
        {
            predicate = predicate.And(e => e.OrganizationId == request.OrganizationId.Value);
        }

        if (request.Status.HasValue)
        {
            predicate = predicate.And(e => e.Status == request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            predicate = predicate.And(e => e.EventDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            predicate = predicate.And(e => e.EventDate <= request.EndDate.Value);
        }

        return predicate;
    }

    private EventDto MapToEventDto(EventAggregate eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            Slug = eventEntity.Slug?.Value ?? string.Empty,
            OrganizationId = eventEntity.OrganizationId,
            PromoterId = eventEntity.PromoterId,
            VenueId = eventEntity.VenueId,
            Status = eventEntity.Status.ToString(),
            EventDate = eventEntity.EventDate,
            TimeZone = eventEntity.TimeZone?.Value ?? string.Empty,
            ImageUrl = eventEntity.ImageUrl,
            BannerUrl = eventEntity.BannerUrl,
            SeoTitle = eventEntity.SeoTitle,
            SeoDescription = eventEntity.SeoDescription,
            Categories = eventEntity.Categories.ToList(),
            Tags = eventEntity.Tags.ToList(),
            Version = eventEntity.Version,
            TotalCapacity = eventEntity.TotalCapacity,
            AvailableCapacity = eventEntity.GetTotalAvailableCapacity(),
            PublishWindow = eventEntity.PublishWindow != null ? new DateTimeRangeDto
            {
                Start = eventEntity.PublishWindow.StartDate,
                End = eventEntity.PublishWindow.EndDate
            } : null,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt ?? DateTime.UtcNow
        };
    }

    private string GenerateGetEventsCacheKey(GetEventsCursorQuery request)
    {
        var keyData = new
        {
            request.PromoterId,
            request.VenueId,
            request.OrganizationId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.SortBy,
            request.SortDescending,
            Cursor = request.Pagination.EffectiveCursor,
            PageSize = request.Pagination.EffectivePageSize,
            Direction = request.Pagination.Direction
        };

        return $"events:get:cursor:{keyData.GetHashCode():X8}";
    }
}

/// <summary>
/// Extension methods for building dynamic predicates
/// </summary>
public static class PredicateExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);

        var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }
}

internal class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public override Expression? Visit(Expression? node)
    {
        return node == _oldValue ? _newValue : base.Visit(node);
    }
}
