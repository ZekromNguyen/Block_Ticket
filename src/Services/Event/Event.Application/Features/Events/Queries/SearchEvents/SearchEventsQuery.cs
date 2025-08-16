using Event.Application.Common.Models;
using Event.Domain.Enums;
using MediatR;

namespace Event.Application.Features.Events.Queries.SearchEvents;

/// <summary>
/// Query to search events with filtering and pagination
/// </summary>
public record SearchEventsQuery : IRequest<PagedResult<EventCatalogDto>>
{
    public string? SearchTerm { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? VenueId { get; init; }
    public string? City { get; init; }
    public List<string>? Categories { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? HasAvailability { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "EventDate";
    public bool SortDescending { get; init; }

    /// <summary>
    /// Create query from request DTO
    /// </summary>
    public static SearchEventsQuery FromRequest(SearchEventsRequest request)
    {
        return new SearchEventsQuery
        {
            SearchTerm = request.SearchTerm,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            VenueId = request.VenueId,
            City = request.City,
            Categories = request.Categories,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            HasAvailability = request.HasAvailability,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };
    }
}

/// <summary>
/// Query to get events with basic filtering
/// </summary>
public record GetEventsQuery : IRequest<PagedResult<EventDto>>
{
    public Guid? PromoterId { get; init; }
    public Guid? VenueId { get; init; }
    public EventStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<string>? Categories { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "EventDate";
    public bool SortDescending { get; init; }

    /// <summary>
    /// Create query from request DTO
    /// </summary>
    public static GetEventsQuery FromRequest(GetEventsRequest request)
    {
        return new GetEventsQuery
        {
            PromoterId = request.PromoterId,
            VenueId = request.VenueId,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Categories = request.Categories,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };
    }
}
