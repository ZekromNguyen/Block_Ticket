using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Event.Application.Services;

/// <summary>
/// Application service for venue management
/// </summary>
public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly ILogger<VenueService> _logger;

    public VenueService(
        IVenueRepository venueRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<VenueService> logger)
    {
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _organizationContextProvider = organizationContextProvider;
        _logger = logger;
    }

    public async Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating venue {VenueName}", request.Name);

        // Create Address value object
        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.PostalCode,
            request.Address.Country,
            request.Address.Coordinates != null 
                ? new GeoCoordinates((double)request.Address.Coordinates.Latitude, (double)request.Address.Coordinates.Longitude)
                : null
        );

        // Create TimeZone value object
        var timeZone = new TimeZoneId(request.TimeZone);

        // Get organization context
        var organizationId = _organizationContextProvider.GetCurrentOrganizationId();

        // Create venue using proper constructor
        var venue = new Venue(
            organizationId,
            request.Name,
            address,
            timeZone,
            request.TotalCapacity,
            request.Description
        );

        // Set contact information
        venue.SetContactInfo(request.ContactEmail, request.ContactPhone, request.Website);

        await _venueRepository.AddAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created venue {VenueId}", venue.Id);

        return MapToDto(venue);
    }

    public async Task<VenueDto> UpdateVenueAsync(Guid venueId, UpdateVenueRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating venue {VenueId}", venueId);

        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue == null)
        {
            throw new InvalidOperationException($"Venue {venueId} not found");
        }

        // Update basic info if provided
        if (!string.IsNullOrEmpty(request.Name) || !string.IsNullOrEmpty(request.Description) || request.Address != null)
        {
            // Use existing values if not provided in request
            var name = request.Name ?? venue.Name;
            var description = request.Description ?? venue.Description;
            var address = venue.Address; // Default to existing address

            if (request.Address != null)
            {
                // Create new Address value object
                address = new Address(
                    request.Address.Street,
                    request.Address.City,
                    request.Address.State,
                    request.Address.PostalCode,
                    request.Address.Country,
                    request.Address.Coordinates != null
                        ? new GeoCoordinates((double)request.Address.Coordinates.Latitude, (double)request.Address.Coordinates.Longitude)
                        : null
                );
            }

            // Update basic info using domain method
            venue.UpdateBasicInfo(name, description, address);
        }
        
        // Update contact information
        venue.SetContactInfo(request.ContactEmail, request.ContactPhone, request.Website);

        // Update capacity if provided
        if (request.TotalCapacity.HasValue)
        {
            venue.UpdateCapacity(request.TotalCapacity.Value);
        }

        await _venueRepository.UpdateAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated venue {VenueId}", venueId);

        return MapToDto(venue);
    }

    public async Task<VenueDto?> GetVenueAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        return venue != null ? MapToDto(venue) : null;
    }

    public async Task<PagedResult<VenueDto>> GetVenuesAsync(GetVenuesRequest request, CancellationToken cancellationToken = default)
    {
        var (venues, totalCount) = await _venueRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate: null, // Add filtering logic here
            orderBy: q => q.OrderBy(v => v.Name));

        var venueDtos = venues.Select(MapToDto).ToList();

        return new PagedResult<VenueDto>
        {
            Items = venueDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResult<VenueDto>> SearchVenuesAsync(SearchVenuesRequest request, CancellationToken cancellationToken = default)
    {
        var (venues, totalCount) = await _venueRepository.SearchVenuesAsync(
            searchTerm: request.SearchTerm,
            city: request.City,
            state: request.State,
            country: request.Country,
            minCapacity: request.MinCapacity,
            maxCapacity: request.MaxCapacity,
            hasAccessibility: null, // Not in request
            latitude: request.Latitude,
            longitude: request.Longitude,
            radiusKm: request.RadiusKm,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            cancellationToken: cancellationToken);

        var venueDtos = venues.Select(MapToDto).ToList();

        return new PagedResult<VenueDto>
        {
            Items = venueDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> DeleteVenueAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting venue {VenueId}", venueId);

        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue == null)
        {
            return false;
        }

        await _venueRepository.DeleteAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted venue {VenueId}", venueId);

        return true;
    }

    public async Task<SeatMapDto?> GetSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue?.SeatMap == null)
        {
            return null;
        }

        // For now, return a basic SeatMapDto. In a real implementation,
        // you would deserialize venue.SeatMap JSON to List<SeatMapRowDto>
        return new SeatMapDto
        {
            VenueId = venueId,
            SeatMapData = new List<SeatMapRowDto>(), // TODO: Deserialize venue.SeatMap JSON
            Version = venue.SeatMapVersion ?? "1.0",
            UpdatedAt = venue.SeatMapLastUpdated ?? venue.CreatedAt
        };
    }

    public async Task<SeatMapDto> UpdateSeatMapAsync(Guid venueId, UpdateSeatMapRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating seat map for venue {VenueId}", venueId);

        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue == null)
        {
            throw new InvalidOperationException($"Venue {venueId} not found");
        }

        // TODO: Implement proper seat map update using domain methods
        // For now, return a stub response
        await _venueRepository.UpdateAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated seat map for venue {VenueId}", venueId);

        return new SeatMapDto
        {
            VenueId = venueId,
            SeatMapData = new List<SeatMapRowDto>(),
            Version = "1.0",
            UpdatedAt = _dateTimeProvider.UtcNow
        };
    }

    public async Task<SeatMapImportResult> ImportSeatMapAsync(Guid venueId, ImportSeatMapRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing seat map for venue {VenueId}", venueId);

        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue == null)
        {
            throw new InvalidOperationException($"Venue {venueId} not found");
        }

        // TODO: Convert SeatMapRowDto to domain SeatMapRow and use venue.ImportSeatMap()
        // For now, return a stub response
        await _venueRepository.UpdateAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported seat map for venue {VenueId}", venueId);

        return new SeatMapImportResult
        {
            Success = true,
            VenueId = venueId,
            ImportedSeats = request.SeatMapData?.Count ?? 0,
            Version = "1.0"
        };
    }

    public async Task<SeatMapExportResult> ExportSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting seat map for venue {VenueId}", venueId);

        var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
        if (venue?.SeatMap == null)
        {
            throw new InvalidOperationException($"No seat map found for venue {venueId}");
        }

        return new SeatMapExportResult
        {
            VenueId = venueId,
            SeatMapData = new List<SeatMapRowDto>(), // TODO: Deserialize venue.SeatMap JSON
            Version = venue.SeatMapVersion ?? "1.0",
            ExportedAt = _dateTimeProvider.UtcNow
        };
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
            City = venue.City,
            State = venue.State,
            Country = venue.Country,
            PostalCode = venue.PostalCode,
            Phone = venue.Phone,
            Email = venue.Email,
            TimeZone = venue.TimeZone.Value,
            TotalCapacity = venue.TotalCapacity,
            Capacity = venue.Capacity,
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
