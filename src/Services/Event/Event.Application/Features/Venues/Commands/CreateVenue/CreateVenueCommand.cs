using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Venues.Commands.CreateVenue;

/// <summary>
/// Command to create a new venue
/// </summary>
public record CreateVenueCommand : IRequest<VenueDto>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddressDto Address { get; init; } = null!;
    public string TimeZone { get; init; } = string.Empty;
    public int TotalCapacity { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? Website { get; init; }

    /// <summary>
    /// Create command from request DTO
    /// </summary>
    public static CreateVenueCommand FromRequest(CreateVenueRequest request)
    {
        return new CreateVenueCommand
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            TimeZone = request.TimeZone,
            TotalCapacity = request.TotalCapacity,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Website = request.Website
        };
    }
}
