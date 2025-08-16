using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Queries.GetEventSeries;

/// <summary>
/// Query to get a single event series by ID
/// </summary>
public record GetEventSeriesQuery : IRequest<EventSeriesDto?>
{
    public Guid SeriesId { get; init; }
    public bool IncludeEvents { get; init; } = false;

    public GetEventSeriesQuery(Guid seriesId, bool includeEvents = false)
    {
        SeriesId = seriesId;
        IncludeEvents = includeEvents;
    }
}

/// <summary>
/// Query to get event series with filtering and pagination
/// </summary>
public record GetEventSeriesListQuery : IRequest<PagedResult<EventSeriesDto>>
{
    public Guid? PromoterId { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "CreatedAt";
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Create query from request DTO
    /// </summary>
    public static GetEventSeriesListQuery FromRequest(GetEventSeriesRequest request)
    {
        return new GetEventSeriesListQuery
        {
            PromoterId = request.PromoterId,
            IsActive = request.IsActive,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
