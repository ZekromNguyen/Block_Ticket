using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Venues.Queries.GetVenue;

/// <summary>
/// Query to get a venue by ID
/// </summary>
public record GetVenueQuery : IRequest<VenueDto?>
{
    public Guid VenueId { get; init; }
    public bool IncludeSeatMap { get; init; } = false;

    public GetVenueQuery(Guid venueId, bool includeSeatMap = false)
    {
        VenueId = venueId;
        IncludeSeatMap = includeSeatMap;
    }
}
