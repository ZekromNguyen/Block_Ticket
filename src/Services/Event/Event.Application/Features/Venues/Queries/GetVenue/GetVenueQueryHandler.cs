using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Venues.Queries.GetVenue;

/// <summary>
/// Handler for GetVenueQuery
/// </summary>
public class GetVenueQueryHandler : IRequestHandler<GetVenueQuery, VenueDto?>
{
    private readonly IVenueRepository _venueRepository;
    private readonly ILogger<GetVenueQueryHandler> _logger;

    public GetVenueQueryHandler(
        IVenueRepository venueRepository,
        ILogger<GetVenueQueryHandler> logger)
    {
        _venueRepository = venueRepository;
        _logger = logger;
    }

    public async Task<VenueDto?> Handle(GetVenueQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting venue with ID: {VenueId}", request.VenueId);

        var venue = await _venueRepository.GetByIdAsync(request.VenueId, cancellationToken);
        
        if (venue == null)
        {
            _logger.LogWarning("Venue with ID {VenueId} not found", request.VenueId);
            return null;
        }

        return MapToDto(venue);
    }

    private static VenueDto MapToDto(Domain.Entities.Venue venue)
    {
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = new AddressDto
            {
                Street = venue.Address.Street,
                City = venue.Address.City,
                State = venue.Address.State,
                PostalCode = venue.Address.PostalCode,
                Country = venue.Address.Country,
                Coordinates = venue.Address.Coordinates != null
                    ? new CoordinatesDto
                    {
                        Latitude = (decimal)venue.Address.Coordinates.Latitude,
                        Longitude = (decimal)venue.Address.Coordinates.Longitude
                    }
                    : null
            },
            TimeZone = venue.TimeZone,
            TotalCapacity = venue.TotalCapacity,
            ContactEmail = venue.ContactEmail,
            ContactPhone = venue.ContactPhone,
            Website = venue.Website,
            HasSeatMap = venue.HasSeatMap,
            SeatMapLastUpdated = venue.SeatMapLastUpdated,
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt
        };
    }
}
