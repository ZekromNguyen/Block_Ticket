using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Venues.Commands.CreateVenue;

/// <summary>
/// Handler for CreateVenueCommand
/// </summary>
public class CreateVenueCommandHandler : IRequestHandler<CreateVenueCommand, VenueDto>
{
    private readonly IVenueRepository _venueRepository;
    private readonly ILogger<CreateVenueCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateVenueCommandHandler(
        IVenueRepository venueRepository,
        ILogger<CreateVenueCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _venueRepository = venueRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VenueDto> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating venue: {VenueName} in {City}", 
            request.Name, request.Address.City);

        // Create the venue entity
        var venue = CreateVenueEntity(request);

        // Save the venue
        await _venueRepository.AddAsync(venue, cancellationToken);
        await _venueRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created venue with ID: {VenueId}", venue.Id);

        // Return the DTO
        return MapToDto(venue);
    }

    private Venue CreateVenueEntity(CreateVenueCommand request)
    {
        var coordinates = request.Address.Coordinates != null
            ? new GeoCoordinates((double)request.Address.Coordinates.Latitude, (double)request.Address.Coordinates.Longitude)
            : null;

        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.PostalCode,
            request.Address.Country,
            coordinates
        );

        var timeZone = new TimeZoneId(request.TimeZone);

        var venue = new Venue(
            request.Name,
            address,
            timeZone,
            request.TotalCapacity,
            request.Description
        );

        // Set contact information
        venue.SetContactInfo(request.ContactEmail, request.ContactPhone, request.Website);

        return venue;
    }

    private static VenueDto MapToDto(Venue venue)
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
