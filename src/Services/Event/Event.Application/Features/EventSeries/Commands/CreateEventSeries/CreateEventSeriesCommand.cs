using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Commands.CreateEventSeries;

/// <summary>
/// Command to create a new event series
/// </summary>
public record CreateEventSeriesCommand : IRequest<EventSeriesDto>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public DateTime? SeriesStartDate { get; init; }
    public DateTime? SeriesEndDate { get; init; }
    public int? MaxEvents { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Create command from request DTO
    /// </summary>
    public static CreateEventSeriesCommand FromRequest(CreateEventSeriesRequest request)
    {
        return new CreateEventSeriesCommand
        {
            Name = request.Name,
            Description = request.Description,
            Slug = request.Slug,
            OrganizationId = request.OrganizationId,
            PromoterId = request.PromoterId,
            SeriesStartDate = request.SeriesStartDate,
            SeriesEndDate = request.SeriesEndDate,
            MaxEvents = request.MaxEvents,
            ImageUrl = request.ImageUrl,
            BannerUrl = request.BannerUrl,
            Categories = request.Categories,
            Tags = request.Tags
        };
    }
}
