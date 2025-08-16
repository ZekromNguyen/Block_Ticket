using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Commands.CreateEventSeries;

/// <summary>
/// Handler for CreateEventSeriesCommand
/// </summary>
public class CreateEventSeriesCommandHandler : IRequestHandler<CreateEventSeriesCommand, EventSeriesDto>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ILogger<CreateEventSeriesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        ILogger<CreateEventSeriesCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventSeriesDto> Handle(CreateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating event series with slug: {Slug} for organization: {OrganizationId}", 
            request.Slug, request.OrganizationId);

        // Validate slug uniqueness (if needed)
        await ValidateSlugUniqueness(request.Slug, request.OrganizationId, cancellationToken);

        // Create the event series
        var eventSeries = CreateEventSeries(request);

        // Save the event series
        var createdSeries = await _eventSeriesRepository.AddAsync(eventSeries, cancellationToken);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created event series {SeriesId} with slug: {Slug}", 
            createdSeries.Id, request.Slug);

        // Convert to DTO
        return MapToDto(createdSeries);
    }

    private async Task ValidateSlugUniqueness(string slug, Guid organizationId, CancellationToken cancellationToken)
    {
        // Check if slug is unique within the organization
        var isUnique = await _eventSeriesRepository.IsSlugUniqueAsync(slug, organizationId, null, cancellationToken);

        if (!isUnique)
        {
            throw new InvalidOperationException($"An event series with slug '{slug}' already exists in this organization");
        }
    }

    private Domain.Entities.EventSeries CreateEventSeries(CreateEventSeriesCommand request)
    {
        // Create value objects
        var slug = Slug.FromString(request.Slug);
        
        DateTimeRange? seriesDateRange = null;
        if (request.SeriesStartDate.HasValue && request.SeriesEndDate.HasValue)
        {
            seriesDateRange = new DateTimeRange(request.SeriesStartDate.Value, request.SeriesEndDate.Value);
        }

        // Create the event series
        var eventSeries = new Domain.Entities.EventSeries(
            name: request.Name,
            description: request.Description,
            slug: slug,
            organizationId: request.OrganizationId,
            promoterId: request.PromoterId);

        // Set optional properties
        if (seriesDateRange != null)
        {
            eventSeries.SetSeriesDateRange(seriesDateRange);
        }

        if (request.MaxEvents.HasValue)
        {
            eventSeries.SetMaxEvents(request.MaxEvents.Value);
        }

        if (!string.IsNullOrEmpty(request.ImageUrl))
        {
            eventSeries.SetImageUrl(request.ImageUrl);
        }

        if (!string.IsNullOrEmpty(request.BannerUrl))
        {
            eventSeries.SetBannerUrl(request.BannerUrl);
        }

        // Add categories and tags
        foreach (var category in request.Categories)
        {
            eventSeries.AddCategory(category);
        }

        foreach (var tag in request.Tags)
        {
            eventSeries.AddTag(tag);
        }

        return eventSeries;
    }

    private static EventSeriesDto MapToDto(Domain.Entities.EventSeries eventSeries)
    {
        return new EventSeriesDto
        {
            Id = eventSeries.Id,
            Name = eventSeries.Name,
            Description = eventSeries.Description,
            Slug = eventSeries.Slug.Value,
            OrganizationId = eventSeries.OrganizationId,
            PromoterId = eventSeries.PromoterId,
            SeriesStartDate = eventSeries.SeriesDateRange?.StartDate,
            SeriesEndDate = eventSeries.SeriesDateRange?.EndDate,
            MaxEvents = eventSeries.MaxEvents,
            IsActive = eventSeries.IsActive,
            ImageUrl = eventSeries.ImageUrl,
            BannerUrl = eventSeries.BannerUrl,
            Categories = eventSeries.Categories.ToList(),
            Tags = eventSeries.Tags.ToList(),
            EventIds = eventSeries.EventIds.ToList(),
            Version = eventSeries.Version,
            CreatedAt = eventSeries.CreatedAt,
            UpdatedAt = eventSeries.UpdatedAt
        };
    }
}
