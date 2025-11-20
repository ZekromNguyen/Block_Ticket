using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Commands.UpdateEventSeries;

/// <summary>
/// Handler for UpdateEventSeriesCommand
/// </summary>
public class UpdateEventSeriesCommandHandler : IRequestHandler<UpdateEventSeriesCommand, EventSeriesDto>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ILogger<UpdateEventSeriesCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        ILogger<UpdateEventSeriesCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventSeriesDto> Handle(UpdateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating event series {SeriesId} with expected version {ExpectedVersion}", 
            request.SeriesId, request.ExpectedVersion);

        // Get the existing event series
        var eventSeries = await GetEventSeries(request.SeriesId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventSeries, request.ExpectedVersion);

        // Validate slug uniqueness if slug is being changed
        if (!string.IsNullOrEmpty(request.Slug) && request.Slug != eventSeries.Slug.Value)
        {
            await ValidateSlugUniqueness(request.Slug, eventSeries.OrganizationId, request.SeriesId, cancellationToken);
        }

        // Update the event series
        await UpdateEventSeries(eventSeries, request, cancellationToken);

        // Save changes
        _eventSeriesRepository.Update(eventSeries);
        await _eventSeriesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated event series {SeriesId} to version {Version}", 
            eventSeries.Id, eventSeries.Version);

        return EventSeriesDto.FromEntity(eventSeries);
    }

    private async Task<Domain.Entities.EventSeries> GetEventSeries(Guid seriesId, CancellationToken cancellationToken)
    {
        var eventSeries = await _eventSeriesRepository.GetByIdAsync(seriesId, cancellationToken);
        if (eventSeries == null)
        {
            throw new InvalidOperationException($"Event series with ID {seriesId} not found");
        }
        return eventSeries;
    }

    private static void ValidateVersion(Domain.Entities.EventSeries eventSeries, int expectedVersion)
    {
        if (eventSeries.Version != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict: Expected version {expectedVersion}, but current version is {eventSeries.Version}");
        }
    }

    private async Task ValidateSlugUniqueness(string slug, Guid organizationId, Guid excludeSeriesId, CancellationToken cancellationToken)
    {
        var isUnique = await _eventSeriesRepository.IsSlugUniqueAsync(slug, organizationId, excludeSeriesId, cancellationToken);
        if (!isUnique)
        {
            throw new InvalidOperationException($"Event series with slug '{slug}' already exists in this organization");
        }
    }

    private async Task UpdateEventSeries(Domain.Entities.EventSeries eventSeries, UpdateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        // Update basic information
        if (!string.IsNullOrEmpty(request.Name) || request.Description != null)
        {
            eventSeries.UpdateBasicInfo(
                request.Name ?? eventSeries.Name,
                request.Description ?? eventSeries.Description);
        }

        // Update slug if provided
        if (!string.IsNullOrEmpty(request.Slug) && request.Slug != eventSeries.Slug.Value)
        {
            eventSeries.UpdateSlug(request.Slug);
        }

        // Update series dates
        if (request.SeriesStartDate.HasValue || request.SeriesEndDate.HasValue)
        {
            var startDate = request.SeriesStartDate ?? eventSeries.SeriesStartDate;
            var endDate = request.SeriesEndDate ?? eventSeries.SeriesEndDate;
            eventSeries.SetSeriesDates(startDate, endDate);
        }

        // Update max events
        if (request.MaxEvents.HasValue)
        {
            eventSeries.SetMaxEvents(request.MaxEvents.Value);
        }

        // Update marketing assets
        if (request.ImageUrl != null || request.BannerUrl != null)
        {
            eventSeries.SetMarketingAssets(
                request.ImageUrl ?? eventSeries.ImageUrl,
                request.BannerUrl ?? eventSeries.BannerUrl);
        }

        // Update SEO information
        if (request.SeoTitle != null || request.SeoDescription != null)
        {
            eventSeries.SetSeoInfo(
                request.SeoTitle ?? eventSeries.SeoTitle,
                request.SeoDescription ?? eventSeries.SeoDescription);
        }

        // Update categories
        if (request.Categories != null)
        {
            // Clear existing categories and add new ones
            eventSeries.ClearCategories();
            foreach (var category in request.Categories)
            {
                eventSeries.AddCategory(category);
            }
        }

        // Update tags
        if (request.Tags != null)
        {
            // Clear existing tags and add new ones
            eventSeries.ClearTags();
            foreach (var tag in request.Tags)
            {
                eventSeries.AddTag(tag);
            }
        }
    }


}
