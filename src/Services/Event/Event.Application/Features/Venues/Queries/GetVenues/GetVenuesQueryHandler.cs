using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Venues.Queries.GetVenues;

/// <summary>
/// Handler for GetVenuesQuery
/// </summary>
public class GetVenuesQueryHandler : IRequestHandler<GetVenuesQuery, PagedResult<VenueDto>>
{
    private readonly IVenueRepository _venueRepository;
    private readonly ILogger<GetVenuesQueryHandler> _logger;

    public GetVenuesQueryHandler(
        IVenueRepository venueRepository,
        ILogger<GetVenuesQueryHandler> logger)
    {
        _venueRepository = venueRepository;
        _logger = logger;
    }

    public async Task<PagedResult<VenueDto>> Handle(GetVenuesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting venues - City: {City}, HasSeatMap: {HasSeatMap}, Page: {PageNumber}, Size: {PageSize}", 
            request.City, request.HasSeatMap, request.PageNumber, request.PageSize);

        // Get venues with filtering and pagination
        var (venues, totalCount) = await _venueRepository.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            predicate: venue =>
                (string.IsNullOrEmpty(request.Name) || venue.Name.Contains(request.Name)) &&
                (string.IsNullOrEmpty(request.City) || venue.Address.City.Contains(request.City)) &&
                (string.IsNullOrEmpty(request.State) || venue.Address.State.Contains(request.State)) &&
                (string.IsNullOrEmpty(request.Country) || venue.Address.Country.Contains(request.Country)) &&
                (!request.HasSeatMap.HasValue || venue.HasSeatMap == request.HasSeatMap.Value) &&
                (!request.MinCapacity.HasValue || venue.TotalCapacity >= request.MinCapacity.Value) &&
                (!request.MaxCapacity.HasValue || venue.TotalCapacity <= request.MaxCapacity.Value),
            orderBy: GetOrderByExpression(request.SortBy, request.SortDescending));

        _logger.LogInformation("Found {TotalCount} venues matching criteria", totalCount);

        // Map to DTOs
        var venueDtos = venues.Select(MapToDto).ToList();

        return new PagedResult<VenueDto>
        {
            Items = venueDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static Func<IQueryable<Domain.Entities.Venue>, IOrderedQueryable<Domain.Entities.Venue>>? GetOrderByExpression(
        string? sortBy, bool sortDescending)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return sortDescending 
                ? q => q.OrderByDescending(v => v.CreatedAt)
                : q => q.OrderBy(v => v.CreatedAt);
        }

        return sortBy.ToLowerInvariant() switch
        {
            "name" => sortDescending 
                ? q => q.OrderByDescending(v => v.Name)
                : q => q.OrderBy(v => v.Name),
            "city" => sortDescending 
                ? q => q.OrderByDescending(v => v.Address.City)
                : q => q.OrderBy(v => v.Address.City),
            "capacity" => sortDescending 
                ? q => q.OrderByDescending(v => v.TotalCapacity)
                : q => q.OrderBy(v => v.TotalCapacity),
            "createdat" => sortDescending 
                ? q => q.OrderByDescending(v => v.CreatedAt)
                : q => q.OrderBy(v => v.CreatedAt),
            _ => sortDescending 
                ? q => q.OrderByDescending(v => v.CreatedAt)
                : q => q.OrderBy(v => v.CreatedAt)
        };
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
            TimeZone = venue.TimeZone.Value,
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
