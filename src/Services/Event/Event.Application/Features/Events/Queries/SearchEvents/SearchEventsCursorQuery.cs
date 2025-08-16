using Event.Application.Common.Models;
using Event.Domain.Enums;
using Event.Domain.Models;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Event.Application.Features.Events.Queries.SearchEvents;

/// <summary>
/// Cursor-based search events query
/// </summary>
public record SearchEventsCursorQuery : IRequest<CursorPagedResult<EventCatalogDto>>
{
    /// <summary>
    /// Search term to match against event title, description, or categories
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter events starting from this date
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter events ending before this date
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Filter by specific venue
    /// </summary>
    public Guid? VenueId { get; init; }

    /// <summary>
    /// Filter by city
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Filter by event categories
    /// </summary>
    public List<string>? Categories { get; init; }

    /// <summary>
    /// Minimum ticket price filter
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Maximum ticket price filter
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Filter to only show events with available tickets
    /// </summary>
    public bool? HasAvailability { get; init; }

    /// <summary>
    /// Filter by event status
    /// </summary>
    public EventStatus? Status { get; init; }

    /// <summary>
    /// Cursor pagination parameters
    /// </summary>
    public CursorPaginationParams Pagination { get; init; } = new();

    /// <summary>
    /// Sort field (EventDate, Title, CreatedAt, Price)
    /// </summary>
    public string SortBy { get; init; } = "EventDate";

    /// <summary>
    /// Sort direction (true for descending, false for ascending)
    /// </summary>
    public bool SortDescending { get; init; } = false;

    /// <summary>
    /// Create query from legacy offset-based request
    /// </summary>
    public static SearchEventsCursorQuery FromLegacyRequest(SearchEventsRequest request)
    {
        return new SearchEventsCursorQuery
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
            SortBy = request.SortBy ?? "EventDate",
            SortDescending = request.SortDescending,
            Pagination = new CursorPaginationParams
            {
                First = request.PageSize,
                IncludeTotalCount = false // Default to false for performance
            }
        };
    }

    /// <summary>
    /// Create query from cursor request
    /// </summary>
    public static SearchEventsCursorQuery FromCursorRequest(SearchEventsCursorRequest request)
    {
        return new SearchEventsCursorQuery
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
            Status = request.Status,
            SortBy = request.SortBy ?? "EventDate",
            SortDescending = request.SortDescending,
            Pagination = new CursorPaginationParams
            {
                First = request.First,
                After = request.After,
                Last = request.Last,
                Before = request.Before,
                IncludeTotalCount = request.IncludeTotalCount
            }
        };
    }
}

/// <summary>
/// Cursor-based get events query
/// </summary>
public record GetEventsCursorQuery : IRequest<CursorPagedResult<EventDto>>
{
    /// <summary>
    /// Filter by promoter ID
    /// </summary>
    public Guid? PromoterId { get; init; }

    /// <summary>
    /// Filter by venue ID
    /// </summary>
    public Guid? VenueId { get; init; }

    /// <summary>
    /// Filter by organization ID
    /// </summary>
    public Guid? OrganizationId { get; init; }

    /// <summary>
    /// Filter by event status
    /// </summary>
    public EventStatus? Status { get; init; }

    /// <summary>
    /// Filter events starting from this date
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Filter events ending before this date
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Cursor pagination parameters
    /// </summary>
    public CursorPaginationParams Pagination { get; init; } = new();

    /// <summary>
    /// Sort field (EventDate, CreatedAt, Title, Status)
    /// </summary>
    public string SortBy { get; init; } = "EventDate";

    /// <summary>
    /// Sort direction (true for descending, false for ascending)
    /// </summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Create query from legacy offset-based request
    /// </summary>
    public static GetEventsCursorQuery FromLegacyRequest(GetEventsRequest request)
    {
        return new GetEventsCursorQuery
        {
            PromoterId = request.PromoterId,
            VenueId = request.VenueId,
            OrganizationId = request.OrganizationId,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SortBy = request.SortBy ?? "EventDate",
            SortDescending = request.SortDescending,
            Pagination = new CursorPaginationParams
            {
                First = request.PageSize,
                IncludeTotalCount = false
            }
        };
    }

    /// <summary>
    /// Create query from cursor request
    /// </summary>
    public static GetEventsCursorQuery FromCursorRequest(GetEventsCursorRequest request)
    {
        return new GetEventsCursorQuery
        {
            PromoterId = request.PromoterId,
            VenueId = request.VenueId,
            OrganizationId = request.OrganizationId,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SortBy = request.SortBy ?? "EventDate",
            SortDescending = request.SortDescending,
            Pagination = new CursorPaginationParams
            {
                First = request.First,
                After = request.After,
                Last = request.Last,
                Before = request.Before,
                IncludeTotalCount = request.IncludeTotalCount
            }
        };
    }
}

/// <summary>
/// Request DTOs for cursor-based pagination
/// </summary>

/// <summary>
/// Cursor-based search events request
/// </summary>
public record SearchEventsCursorRequest
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
    public EventStatus? Status { get; init; }
    public string? SortBy { get; init; } = "EventDate";
    public bool SortDescending { get; init; } = false;

    // Cursor pagination parameters
    [Range(1, 100)]
    public int First { get; init; } = 20;
    public string? After { get; init; }
    [Range(1, 100)]
    public int Last { get; init; } = 20;
    public string? Before { get; init; }
    public bool IncludeTotalCount { get; init; } = false;
}

/// <summary>
/// Cursor-based get events request
/// </summary>
public record GetEventsCursorRequest
{
    public Guid? PromoterId { get; init; }
    public Guid? VenueId { get; init; }
    public Guid? OrganizationId { get; init; }
    public EventStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? SortBy { get; init; } = "EventDate";
    public bool SortDescending { get; init; } = true;

    // Cursor pagination parameters
    [Range(1, 100)]
    public int First { get; init; } = 20;
    public string? After { get; init; }
    [Range(1, 100)]
    public int Last { get; init; } = 20;
    public string? Before { get; init; }
    public bool IncludeTotalCount { get; init; } = false;
}
