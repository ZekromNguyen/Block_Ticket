using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Venues.Queries.GetVenues;

/// <summary>
/// Query to get venues with filtering and pagination
/// </summary>
public record GetVenuesQuery : IRequest<PagedResult<VenueDto>>
{
    public string? Name { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public bool? HasSeatMap { get; init; }
    public int? MinCapacity { get; init; }
    public int? MaxCapacity { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;

    /// <summary>
    /// Create query from request DTO
    /// </summary>
    public static GetVenuesQuery FromRequest(GetVenuesRequest request)
    {
        return new GetVenuesQuery
        {
            Name = null, // GetVenuesRequest doesn't have Name property
            City = request.City,
            State = request.State,
            Country = request.Country,
            HasSeatMap = request.HasSeatMap,
            MinCapacity = request.MinCapacity,
            MaxCapacity = request.MaxCapacity,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };
    }
}
