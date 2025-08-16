using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Events.Commands.UpdateEvent;

/// <summary>
/// Command to update an existing event
/// </summary>
public record UpdateEventCommand : IRequest<EventDto>
{
    public Guid EventId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? EventDate { get; init; }
    public string? TimeZone { get; init; }
    public DateTime? PublishStartDate { get; init; }
    public DateTime? PublishEndDate { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public List<string>? Categories { get; init; }
    public List<string>? Tags { get; init; }
    public int ExpectedVersion { get; init; } // For optimistic concurrency control

    /// <summary>
    /// Create command from request DTO
    /// </summary>
    public static UpdateEventCommand FromRequest(Guid eventId, UpdateEventRequest request, int expectedVersion)
    {
        return new UpdateEventCommand
        {
            EventId = eventId,
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            TimeZone = request.TimeZone,
            PublishStartDate = request.PublishStartDate,
            PublishEndDate = request.PublishEndDate,
            ImageUrl = request.ImageUrl,
            BannerUrl = request.BannerUrl,
            SeoTitle = request.SeoTitle,
            SeoDescription = request.SeoDescription,
            Categories = request.Categories,
            Tags = request.Tags,
            ExpectedVersion = expectedVersion
        };
    }
}
