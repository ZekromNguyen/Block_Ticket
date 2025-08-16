using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Command to create a new event
/// </summary>
public record CreateEventCommand : IRequest<EventDto>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public DateTime EventDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public DateTime? PublishStartDate { get; init; }
    public DateTime? PublishEndDate { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Create command from request DTO
    /// </summary>
    public static CreateEventCommand FromRequest(CreateEventRequest request)
    {
        return new CreateEventCommand
        {
            Title = request.Title,
            Description = request.Description,
            Slug = request.Slug,
            OrganizationId = request.OrganizationId,
            PromoterId = request.PromoterId,
            VenueId = request.VenueId,
            EventDate = request.EventDate,
            TimeZone = request.TimeZone,
            PublishStartDate = request.PublishStartDate,
            PublishEndDate = request.PublishEndDate,
            ImageUrl = request.ImageUrl,
            BannerUrl = request.BannerUrl,
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
            Categories = request.Categories,
            Tags = request.Tags
        };
    }
}
