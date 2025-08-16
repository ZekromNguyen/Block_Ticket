using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Queries.GetEventSeries;

/// <summary>
/// Handler for GetEventSeriesQuery
/// </summary>
public class GetEventSeriesQueryHandler : IRequestHandler<GetEventSeriesQuery, EventSeriesDto?>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventSeriesQueryHandler> _logger;

    public GetEventSeriesQueryHandler(
        IEventSeriesRepository eventSeriesRepository,
        IEventRepository eventRepository,
        ICacheService cacheService,
        ILogger<GetEventSeriesQueryHandler> logger)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<EventSeriesDto?> Handle(GetEventSeriesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting event series {SeriesId}", request.SeriesId);

        // Try to get from cache first
        var cacheKey = GetCacheKey(request.SeriesId, request.IncludeEvents);
        var cachedSeries = await _cacheService.GetAsync<EventSeriesDto>(cacheKey, cancellationToken);
        
        if (cachedSeries != null)
        {
            _logger.LogDebug("Event series {SeriesId} found in cache", request.SeriesId);
            return cachedSeries;
        }

        // Get from database
        var eventSeries = request.IncludeEvents 
            ? await _eventSeriesRepository.GetWithEventsAsync(request.SeriesId, cancellationToken)
            : await _eventSeriesRepository.GetByIdAsync(request.SeriesId, cancellationToken);
        
        if (eventSeries == null)
        {
            _logger.LogInformation("Event series {SeriesId} not found", request.SeriesId);
            return null;
        }

        // Convert to DTO
        var seriesDto = MapToDto(eventSeries);

        // Cache the result
        await _cacheService.SetAsync(cacheKey, seriesDto, TimeSpan.FromMinutes(5), cancellationToken);

        _logger.LogInformation("Successfully retrieved event series {SeriesId}", request.SeriesId);
        return seriesDto;
    }

    private static EventSeriesDto MapToDto(Domain.Entities.EventSeries eventSeries)
    {
        return new EventSeriesDto
        {
            Id = eventSeries.Id,
            Name = eventSeries.Name,
            Description = eventSeries.Description,
            Slug = eventSeries.Slug.Value,
            OrganizationId = eventSeries.OrganizationId,
            PromoterId = eventSeries.PromoterId,
            SeriesStartDate = eventSeries.SeriesDateRange?.StartDate,
            SeriesEndDate = eventSeries.SeriesDateRange?.EndDate,
            MaxEvents = eventSeries.MaxEvents,
            IsActive = eventSeries.IsActive,
            ImageUrl = eventSeries.ImageUrl,
            BannerUrl = eventSeries.BannerUrl,
            Categories = eventSeries.Categories.ToList(),
            Tags = eventSeries.Tags.ToList(),
            EventIds = eventSeries.EventIds.ToList(),
            Version = eventSeries.Version,
            CreatedAt = eventSeries.CreatedAt,
            UpdatedAt = eventSeries.UpdatedAt
        };
    }

    private static string GetCacheKey(Guid seriesId, bool includeEvents)
    {
        var includesStr = includeEvents ? ":with-events" : "";
        return $"event-series:{seriesId}{includesStr}";
    }
}

/// <summary>
/// Handler for GetEventSeriesListQuery
/// </summary>
public class GetEventSeriesListQueryHandler : IRequestHandler<GetEventSeriesListQuery, PagedResult<EventSeriesDto>>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventSeriesListQueryHandler> _logger;

    public GetEventSeriesListQueryHandler(
        IEventSeriesRepository eventSeriesRepository,
        ICacheService cacheService,
        ILogger<GetEventSeriesListQueryHandler> logger)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<PagedResult<EventSeriesDto>> Handle(GetEventSeriesListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting event series list - Promoter: {PromoterId}, Active: {IsActive}", 
            request.PromoterId, request.IsActive);

        // Generate cache key
        var cacheKey = GenerateCacheKey(request);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<PagedResult<EventSeriesDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Event series list found in cache for key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        // Build filter predicate
        var (eventSeriesList, totalCount) = await _eventSeriesRepository.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            predicate: BuildFilterPredicate(request),
            orderBy: BuildOrderBy(request.SortBy, request.SortDescending));

        // Convert to DTOs
        var seriesDtos = eventSeriesList.Select(MapToDto).ToList();

        var result = new PagedResult<EventSeriesDto>
        {
            Items = seriesDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);

        _logger.LogInformation("Found {Count} event series matching criteria", seriesDtos.Count);
        return result;
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Entities.EventSeries, bool>>? BuildFilterPredicate(GetEventSeriesListQuery request)
    {
        System.Linq.Expressions.Expression<Func<Domain.Entities.EventSeries, bool>>? predicate = null;

        if (request.PromoterId.HasValue)
        {
            predicate = CombinePredicates(predicate, s => s.PromoterId == request.PromoterId.Value);
        }

        if (request.IsActive.HasValue)
        {
            predicate = CombinePredicates(predicate, s => s.IsActive == request.IsActive.Value);
        }

        return predicate;
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Entities.EventSeries, bool>>? CombinePredicates(
        System.Linq.Expressions.Expression<Func<Domain.Entities.EventSeries, bool>>? first,
        System.Linq.Expressions.Expression<Func<Domain.Entities.EventSeries, bool>> second)
    {
        if (first == null) return second;

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Domain.Entities.EventSeries), "s");
        var firstBody = new ParameterReplacer(parameter).Visit(first.Body);
        var secondBody = new ParameterReplacer(parameter).Visit(second.Body);
        var combined = System.Linq.Expressions.Expression.AndAlso(firstBody!, secondBody!);
        
        return System.Linq.Expressions.Expression.Lambda<Func<Domain.Entities.EventSeries, bool>>(combined, parameter);
    }

    private static Func<IQueryable<Domain.Entities.EventSeries>, IOrderedQueryable<Domain.Entities.EventSeries>>? BuildOrderBy(string? sortBy, bool sortDescending)
    {
        return sortBy?.ToLower() switch
        {
            "name" => sortDescending ? q => q.OrderByDescending(s => s.Name) : q => q.OrderBy(s => s.Name),
            "createdat" => sortDescending ? q => q.OrderByDescending(s => s.CreatedAt) : q => q.OrderBy(s => s.CreatedAt),
            "updatedat" => sortDescending ? q => q.OrderByDescending(s => s.UpdatedAt) : q => q.OrderBy(s => s.UpdatedAt),
            "seriesstartdate" => sortDescending ? q => q.OrderByDescending(s => s.SeriesDateRange!.StartDate) : q => q.OrderBy(s => s.SeriesDateRange!.StartDate),
            _ => sortDescending ? q => q.OrderByDescending(s => s.CreatedAt) : q => q.OrderBy(s => s.CreatedAt)
        };
    }

    private static EventSeriesDto MapToDto(Domain.Entities.EventSeries eventSeries)
    {
        return new EventSeriesDto
        {
            Id = eventSeries.Id,
            Name = eventSeries.Name,
            Description = eventSeries.Description,
            Slug = eventSeries.Slug.Value,
            OrganizationId = eventSeries.OrganizationId,
            PromoterId = eventSeries.PromoterId,
            SeriesStartDate = eventSeries.SeriesDateRange?.StartDate,
            SeriesEndDate = eventSeries.SeriesDateRange?.EndDate,
            MaxEvents = eventSeries.MaxEvents,
            IsActive = eventSeries.IsActive,
            ImageUrl = eventSeries.ImageUrl,
            BannerUrl = eventSeries.BannerUrl,
            Categories = eventSeries.Categories.ToList(),
            Tags = eventSeries.Tags.ToList(),
            EventIds = eventSeries.EventIds.ToList(),
            Version = eventSeries.Version,
            CreatedAt = eventSeries.CreatedAt,
            UpdatedAt = eventSeries.UpdatedAt
        };
    }

    private static string GenerateCacheKey(GetEventSeriesListQuery request)
    {
        var keyBuilder = new System.Text.StringBuilder();
        keyBuilder.Append("event-series:list:");
        
        if (request.PromoterId.HasValue)
            keyBuilder.Append($"promoter:{request.PromoterId.Value}:");
        
        if (request.IsActive.HasValue)
            keyBuilder.Append($"active:{request.IsActive.Value}:");
        
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
