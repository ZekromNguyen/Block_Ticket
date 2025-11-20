using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Commands.UpdateEventSeries;

/// <summary>
/// Command to update an existing event series
/// </summary>
public record UpdateEventSeriesCommand : IRequest<EventSeriesDto>
{
    public Guid SeriesId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Slug { get; init; }
    public DateTime? SeriesStartDate { get; init; }
    public DateTime? SeriesEndDate { get; init; }
    public int? MaxEvents { get; init; }
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
    public static UpdateEventSeriesCommand FromRequest(Guid seriesId, UpdateEventSeriesRequest request, int expectedVersion)
    {
        return new UpdateEventSeriesCommand
        {
            SeriesId = seriesId,
            Name = request.Name,
            Description = request.Description,
            Slug = request.Slug,
            SeriesStartDate = request.SeriesStartDate,
            SeriesEndDate = request.SeriesEndDate,
            MaxEvents = request.MaxEvents,
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


