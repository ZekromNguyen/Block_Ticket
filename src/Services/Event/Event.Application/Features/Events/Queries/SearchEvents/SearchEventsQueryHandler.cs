using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Event.Application.Features.Events.Queries.SearchEvents;

/// <summary>
/// Handler for SearchEventsQuery
/// </summary>
public class SearchEventsQueryHandler : IRequestHandler<SearchEventsQuery, PagedResult<EventCatalogDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SearchEventsQueryHandler> _logger;

    public SearchEventsQueryHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        ICacheService cacheService,
        ILogger<SearchEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<EventCatalogDto>> Handle(SearchEventsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching events with term: {SearchTerm}, Page: {PageNumber}, Size: {PageSize}", 
            request.SearchTerm, request.PageNumber, request.PageSize);

        // Generate cache key based on search parameters
        var cacheKey = GenerateSearchCacheKey(request);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<PagedResult<EventCatalogDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Search results found in cache for key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        // Search in database
        var events = await _eventRepository.SearchEventsAsync(
            searchTerm: request.SearchTerm,
            startDate: request.StartDate,
            endDate: request.EndDate,
            venueId: request.VenueId,
            categories: request.Categories,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            hasAvailability: request.HasAvailability,
            skip: (request.PageNumber - 1) * request.PageSize,
            take: request.PageSize,
            cancellationToken: cancellationToken);

        // Get total count for pagination
        var totalCount = await GetTotalSearchCount(request, cancellationToken);

        // Get venue information for the events
        var venueIds = events.Events.Select(e => e.VenueId).Distinct().ToList();
        var venues = await GetVenuesSummary(venueIds, cancellationToken);

        // Convert to catalog DTOs
        var catalogDtos = events.Events.Select(e => MapToCatalogDto(e, venues)).ToList();

        // Apply sorting if needed
        catalogDtos = ApplySorting(catalogDtos, request.SortBy, request.SortDescending);

        var result = new PagedResult<EventCatalogDto>
        {
            Items = catalogDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(30), cancellationToken);

        _logger.LogInformation("Found {Count} events matching search criteria", catalogDtos.Count);
        return result;
    }

    private async Task<int> GetTotalSearchCount(SearchEventsQuery request, CancellationToken cancellationToken)
    {
        // For now, we'll use a simple approach. In production, you might want to optimize this
        // by having a separate count query or using the repository to return both results and count
        var allEvents = await _eventRepository.SearchEventsAsync(
            searchTerm: request.SearchTerm,
            startDate: request.StartDate,
            endDate: request.EndDate,
            venueId: request.VenueId,
            categories: request.Categories,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            hasAvailability: request.HasAvailability,
            skip: 0,
            take: int.MaxValue,
            cancellationToken: cancellationToken);

        return allEvents.TotalCount;
    }

    private async Task<Dictionary<Guid, VenueSummaryDto>> GetVenuesSummary(List<Guid> venueIds, CancellationToken cancellationToken)
    {
        var venues = new Dictionary<Guid, VenueSummaryDto>();
        
        foreach (var venueId in venueIds)
        {
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue != null)
            {
                venues[venueId] = new VenueSummaryDto
                {
                    Id = venue.Id,
                    Name = venue.Name,
                    City = venue.Address.City,
                    State = venue.Address.State,
                    Country = venue.Address.Country,
                    TotalCapacity = venue.TotalCapacity,
                    HasSeatMap = venue.HasSeatMap
                };
            }
        }

        return venues;
    }

    private static EventCatalogDto MapToCatalogDto(EventAggregate eventAggregate, Dictionary<Guid, VenueSummaryDto> venues)
    {
        venues.TryGetValue(eventAggregate.VenueId, out var venue);

        // Calculate pricing information from ticket types
        var minPrice = eventAggregate.TicketTypes.Any() ? eventAggregate.TicketTypes.Min(t => t.BasePrice.Amount) : (decimal?)null;
        var maxPrice = eventAggregate.TicketTypes.Any() ? eventAggregate.TicketTypes.Max(t => t.BasePrice.Amount) : (decimal?)null;
        var currency = eventAggregate.TicketTypes.FirstOrDefault()?.BasePrice.Currency ?? "USD";

        // Calculate availability
        var totalCapacity = eventAggregate.TicketTypes.Sum(t => t.Capacity.Total);
        var availableCapacity = eventAggregate.TicketTypes.Sum(t => t.Capacity.Available);
        var hasAvailability = availableCapacity > 0;

        return new EventCatalogDto
        {
            Id = eventAggregate.Id,
            Title = eventAggregate.Title,
            Description = eventAggregate.Description,
            Slug = eventAggregate.Slug.Value,
            EventDate = eventAggregate.EventDate,
            TimeZone = eventAggregate.TimeZone.Value,
            ImageUrl = eventAggregate.ImageUrl,
            Categories = eventAggregate.Categories.ToList(),
            Tags = eventAggregate.Tags.ToList(),
            Venue = venue ?? new VenueSummaryDto { Id = eventAggregate.VenueId, Name = "Unknown Venue" },
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Currency = currency,
            HasAvailability = hasAvailability,
            TotalCapacity = totalCapacity,
            AvailableCapacity = availableCapacity
        };
    }

    private static List<EventCatalogDto> ApplySorting(List<EventCatalogDto> events, string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLower() switch
        {
            "title" => sortDescending ? events.OrderByDescending(e => e.Title).ToList() : events.OrderBy(e => e.Title).ToList(),
            "eventdate" => sortDescending ? events.OrderByDescending(e => e.EventDate).ToList() : events.OrderBy(e => e.EventDate).ToList(),
            "price" => sortDescending ? events.OrderByDescending(e => e.MinPrice ?? 0).ToList() : events.OrderBy(e => e.MinPrice ?? 0).ToList(),
            "venue" => sortDescending ? events.OrderByDescending(e => e.Venue.Name).ToList() : events.OrderBy(e => e.Venue.Name).ToList(),
            _ => sortDescending ? events.OrderByDescending(e => e.EventDate).ToList() : events.OrderBy(e => e.EventDate).ToList()
        };
    }

    private static string GenerateSearchCacheKey(SearchEventsQuery request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append("search:events:");
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
            keyBuilder.Append($"term:{request.SearchTerm}:");
        
        if (request.StartDate.HasValue)
            keyBuilder.Append($"start:{request.StartDate.Value:yyyyMMdd}:");
        
        if (request.EndDate.HasValue)
            keyBuilder.Append($"end:{request.EndDate.Value:yyyyMMdd}:");
        
        if (request.VenueId.HasValue)
            keyBuilder.Append($"venue:{request.VenueId.Value}:");
        
        if (!string.IsNullOrEmpty(request.City))
            keyBuilder.Append($"city:{request.City}:");
        
        if (request.Categories?.Any() == true)
            keyBuilder.Append($"cats:{string.Join(",", request.Categories.OrderBy(c => c))}:");
        
        if (request.MinPrice.HasValue)
            keyBuilder.Append($"minp:{request.MinPrice.Value}:");
        
        if (request.MaxPrice.HasValue)
            keyBuilder.Append($"maxp:{request.MaxPrice.Value}:");
        
        if (request.HasAvailability.HasValue)
            keyBuilder.Append($"avail:{request.HasAvailability.Value}:");
        
        keyBuilder.Append($"page:{request.PageNumber}:size:{request.PageSize}:");
        keyBuilder.Append($"sort:{request.SortBy}:desc:{request.SortDescending}");

        // Generate a hash of the key to keep it manageable
        var keyString = keyBuilder.ToString();
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        var hash = Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
        
        return $"search:events:{hash}";
    }
}

/// <summary>
/// Handler for GetEventsQuery
/// </summary>
public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventsQueryHandler> _logger;

    public GetEventsQueryHandler(
        IEventRepository eventRepository,
        ICacheService cacheService,
        ILogger<GetEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting events with filters - Promoter: {PromoterId}, Venue: {VenueId}, Status: {Status}",
            request.PromoterId, request.VenueId, request.Status);

        // Generate cache key
        var cacheKey = GenerateGetEventsCacheKey(request);

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<PagedResult<EventDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Events found in cache for key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        // Build filter predicate
        var (events, totalCount) = await _eventRepository.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            predicate: BuildFilterPredicate(request),
            orderBy: BuildOrderBy(request.SortBy, request.SortDescending));

        // Convert to DTOs
        var eventDtos = events.Select(e => MapToEventDto(e)).ToList();

        var result = new PagedResult<EventDto>
        {
            Items = eventDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);

        _logger.LogInformation("Found {Count} events matching criteria", eventDtos.Count);
        return result;
    }

    private static System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? BuildFilterPredicate(GetEventsQuery request)
    {
        System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? predicate = null;

        if (request.PromoterId.HasValue)
        {
            predicate = CombinePredicates(predicate, e => e.PromoterId == request.PromoterId.Value);
        }

        if (request.VenueId.HasValue)
        {
            predicate = CombinePredicates(predicate, e => e.VenueId == request.VenueId.Value);
        }

        if (request.Status.HasValue)
        {
            predicate = CombinePredicates(predicate, e => e.Status == request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            predicate = CombinePredicates(predicate, e => e.EventDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            predicate = CombinePredicates(predicate, e => e.EventDate <= request.EndDate.Value);
        }

        if (request.Categories?.Any() == true)
        {
            foreach (var category in request.Categories)
            {
                predicate = CombinePredicates(predicate, e => e.Categories.Contains(category));
            }
        }

        return predicate;
    }

    private static System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? CombinePredicates(
        System.Linq.Expressions.Expression<Func<EventAggregate, bool>>? first,
        System.Linq.Expressions.Expression<Func<EventAggregate, bool>> second)
    {
        if (first == null) return second;

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(EventAggregate), "e");
        var firstBody = new ParameterReplacer(parameter).Visit(first.Body);
        var secondBody = new ParameterReplacer(parameter).Visit(second.Body);
        var combined = System.Linq.Expressions.Expression.AndAlso(firstBody!, secondBody!);

        return System.Linq.Expressions.Expression.Lambda<Func<EventAggregate, bool>>(combined, parameter);
    }

    private static Func<IQueryable<EventAggregate>, IOrderedQueryable<EventAggregate>>? BuildOrderBy(string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLower() switch
        {
            "title" => sortDescending ? q => q.OrderByDescending(e => e.Title) : q => q.OrderBy(e => e.Title),
            "eventdate" => sortDescending ? q => q.OrderByDescending(e => e.EventDate) : q => q.OrderBy(e => e.EventDate),
            "createdat" => sortDescending ? q => q.OrderByDescending(e => e.CreatedAt) : q => q.OrderBy(e => e.CreatedAt),
            "updatedat" => sortDescending ? q => q.OrderByDescending(e => e.UpdatedAt) : q => q.OrderBy(e => e.UpdatedAt),
            _ => sortDescending ? q => q.OrderByDescending(e => e.EventDate) : q => q.OrderBy(e => e.EventDate)
        };
    }

    private static EventDto MapToEventDto(EventAggregate eventAggregate)
    {
        return new EventDto
        {
            Id = eventAggregate.Id,
            Title = eventAggregate.Title,
            Description = eventAggregate.Description,
            Slug = eventAggregate.Slug.Value,
            OrganizationId = eventAggregate.OrganizationId,
            PromoterId = eventAggregate.PromoterId,
            VenueId = eventAggregate.VenueId,
            Status = eventAggregate.Status.ToString(),
            EventDate = eventAggregate.EventDate,
            TimeZone = eventAggregate.TimeZone.Value,
            PublishStartDate = eventAggregate.PublishWindow?.StartDate,
            PublishEndDate = eventAggregate.PublishWindow?.EndDate,
            ImageUrl = eventAggregate.ImageUrl,
            BannerUrl = eventAggregate.BannerUrl,
            SeoTitle = eventAggregate.SeoTitle,
            SeoDescription = eventAggregate.SeoDescription,
            Categories = eventAggregate.Categories.ToList(),
            Tags = eventAggregate.Tags.ToList(),
            Version = eventAggregate.Version,
            CreatedAt = eventAggregate.CreatedAt,
            UpdatedAt = eventAggregate.UpdatedAt,
            TicketTypes = new List<TicketTypeDto>(),
            PricingRules = new List<PricingRuleDto>(),
            Allocations = new List<AllocationDto>()
        };
    }

    private static string GenerateGetEventsCacheKey(GetEventsQuery request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append("events:list:");

        if (request.PromoterId.HasValue)
            keyBuilder.Append($"promoter:{request.PromoterId.Value}:");

        if (request.VenueId.HasValue)
            keyBuilder.Append($"venue:{request.VenueId.Value}:");

        if (request.Status.HasValue)
            keyBuilder.Append($"status:{request.Status.Value}:");

        if (request.StartDate.HasValue)
            keyBuilder.Append($"start:{request.StartDate.Value:yyyyMMdd}:");

        if (request.EndDate.HasValue)
            keyBuilder.Append($"end:{request.EndDate.Value:yyyyMMdd}:");

        if (request.Categories?.Any() == true)
            keyBuilder.Append($"cats:{string.Join(",", request.Categories.OrderBy(c => c))}:");

        keyBuilder.Append($"page:{request.PageNumber}:size:{request.PageSize}:");
        keyBuilder.Append($"sort:{request.SortBy}:desc:{request.SortDescending}");

        return keyBuilder.ToString();
    }
}

/// <summary>
/// Helper class for combining LINQ expressions
/// </summary>
internal class ParameterReplacer : System.Linq.Expressions.ExpressionVisitor
{
    private readonly System.Linq.Expressions.ParameterExpression _parameter;

    public ParameterReplacer(System.Linq.Expressions.ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
    {
        return _parameter;
    }
}
