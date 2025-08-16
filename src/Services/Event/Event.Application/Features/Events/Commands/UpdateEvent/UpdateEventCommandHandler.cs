using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.Commands.UpdateEvent;

/// <summary>
/// Handler for UpdateEventCommand
/// </summary>
public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<UpdateEventCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
        ILogger<UpdateEventCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventRepository = eventRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating event {EventId} with expected version {ExpectedVersion}", 
            request.EventId, request.ExpectedVersion);

        // Get the existing event
        var eventAggregate = await GetEventAggregate(request.EventId, cancellationToken);

        // Validate version for optimistic concurrency control
        ValidateVersion(eventAggregate, request.ExpectedVersion);

        // Validate business rules
        await ValidateBusinessRules(eventAggregate, request, cancellationToken);

        // Track changes for audit
        var changes = new Dictionary<string, object>();

        // Apply updates
        ApplyUpdates(eventAggregate, request, changes);

        // Save changes
        _eventRepository.Update(eventAggregate);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        // Raise domain event for change tracking
        if (changes.Any())
        {
            eventAggregate.AddDomainEvent(new EventUpdatedDomainEvent(
                eventAggregate.Id, 
                eventAggregate.Title, 
                changes));
        }

        _logger.LogInformation("Successfully updated event {EventId} to version {Version}", 
            eventAggregate.Id, eventAggregate.Version);

        // Convert to DTO
        return MapToDto(eventAggregate);
    }

    private async Task<EventAggregate> GetEventAggregate(Guid eventId, CancellationToken cancellationToken)
    {
        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event with ID '{eventId}' not found");
        }
        return eventAggregate;
    }

    private static void ValidateVersion(EventAggregate eventAggregate, int expectedVersion)
    {
        if (eventAggregate.Version != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict: Expected version {expectedVersion}, but current version is {eventAggregate.Version}");
        }
    }

    private async Task ValidateBusinessRules(EventAggregate eventAggregate, UpdateEventCommand request, CancellationToken cancellationToken)
    {
        // Cannot update published events in certain ways
        if (eventAggregate.Status == EventStatus.Published || eventAggregate.Status == EventStatus.OnSale)
        {
            if (request.EventDate.HasValue && request.EventDate.Value != eventAggregate.EventDate)
            {
                throw new InvalidOperationException("Cannot change event date for published events");
            }
        }

        // Cannot update sold out events
        if (eventAggregate.Status == EventStatus.SoldOut)
        {
            throw new InvalidOperationException("Cannot update sold out events");
        }

        // Cannot update cancelled events
        if (eventAggregate.Status == EventStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot update cancelled events");
        }

        // Validate event date is still in the future if being changed
        if (request.EventDate.HasValue && request.EventDate.Value <= _dateTimeProvider.UtcNow.AddHours(1))
        {
            throw new InvalidOperationException("Event date must be at least 1 hour in the future");
        }
    }

    private void ApplyUpdates(EventAggregate eventAggregate, UpdateEventCommand request, Dictionary<string, object> changes)
    {
        if (!string.IsNullOrEmpty(request.Title) && request.Title != eventAggregate.Title)
        {
            changes["Title"] = new { Old = eventAggregate.Title, New = request.Title };
            eventAggregate.UpdateTitle(request.Title);
        }

        if (!string.IsNullOrEmpty(request.Description) && request.Description != eventAggregate.Description)
        {
            changes["Description"] = new { Old = eventAggregate.Description, New = request.Description };
            eventAggregate.UpdateDescription(request.Description);
        }

        if (request.EventDate.HasValue && request.EventDate.Value != eventAggregate.EventDate)
        {
            changes["EventDate"] = new { Old = eventAggregate.EventDate, New = request.EventDate.Value };
            eventAggregate.UpdateEventDate(request.EventDate.Value);
        }

        if (!string.IsNullOrEmpty(request.TimeZone) && request.TimeZone != eventAggregate.TimeZone.Value)
        {
            changes["TimeZone"] = new { Old = eventAggregate.TimeZone.Value, New = request.TimeZone };
            eventAggregate.UpdateTimeZone(TimeZoneId.FromString(request.TimeZone));
        }

        // Update publish window
        if (request.PublishStartDate.HasValue || request.PublishEndDate.HasValue)
        {
            var startDate = request.PublishStartDate ?? eventAggregate.PublishWindow?.StartDate;
            var endDate = request.PublishEndDate ?? eventAggregate.PublishWindow?.EndDate;

            if (startDate.HasValue && endDate.HasValue)
            {
                var newWindow = new DateTimeRange(startDate.Value, endDate.Value);
                if (eventAggregate.PublishWindow == null || !eventAggregate.PublishWindow.Equals(newWindow))
                {
                    changes["PublishWindow"] = new { Old = eventAggregate.PublishWindow, New = newWindow };
                    eventAggregate.SetPublishWindow(newWindow.StartDate, newWindow.EndDate);
                }
            }
        }

        // Update image URLs
        if (request.ImageUrl != null && request.ImageUrl != eventAggregate.ImageUrl)
        {
            changes["ImageUrl"] = new { Old = eventAggregate.ImageUrl, New = request.ImageUrl };
            eventAggregate.SetImageUrl(request.ImageUrl);
        }

        if (request.BannerUrl != null && request.BannerUrl != eventAggregate.BannerUrl)
        {
            changes["BannerUrl"] = new { Old = eventAggregate.BannerUrl, New = request.BannerUrl };
            eventAggregate.SetBannerUrl(request.BannerUrl);
        }

        // Update SEO metadata
        if (request.SeoTitle != null || request.SeoDescription != null)
        {
            var newSeoTitle = request.SeoTitle ?? eventAggregate.SeoTitle;
            var newSeoDescription = request.SeoDescription ?? eventAggregate.SeoDescription;

            if (newSeoTitle != eventAggregate.SeoTitle || newSeoDescription != eventAggregate.SeoDescription)
            {
                changes["SeoMetadata"] = new 
                { 
                    Old = new { Title = eventAggregate.SeoTitle, Description = eventAggregate.SeoDescription },
                    New = new { Title = newSeoTitle, Description = newSeoDescription }
                };
                eventAggregate.SetSeoMetadata(newSeoTitle, newSeoDescription);
            }
        }

        // Update categories
        if (request.Categories != null)
        {
            var currentCategories = eventAggregate.Categories.ToList();
            if (!currentCategories.SequenceEqual(request.Categories))
            {
                changes["Categories"] = new { Old = currentCategories, New = request.Categories };
                eventAggregate.ClearCategories();
                foreach (var category in request.Categories)
                {
                    eventAggregate.AddCategory(category);
                }
            }
        }

        // Update tags
        if (request.Tags != null)
        {
            var currentTags = eventAggregate.Tags.ToList();
            if (!currentTags.SequenceEqual(request.Tags))
            {
                changes["Tags"] = new { Old = currentTags, New = request.Tags };
                eventAggregate.ClearTags();
                foreach (var tag in request.Tags)
                {
                    eventAggregate.AddTag(tag);
                }
            }
        }
    }

    private static EventDto MapToDto(EventAggregate eventAggregate)
    {
        return new EventDto
        {
            Id = eventAggregate.Id,
            Title = eventAggregate.Title,
            Description = eventAggregate.Description,
            Slug = eventAggregate.Slug.Value,
            OrganizationId = eventAggregate.OrganizationId,
            PromoterId = eventAggregate.PromoterId,
            VenueId = eventAggregate.VenueId,
            Status = eventAggregate.Status.ToString(),
            EventDate = eventAggregate.EventDate,
            TimeZone = eventAggregate.TimeZone.Value,
            PublishStartDate = eventAggregate.PublishWindow?.StartDate,
            PublishEndDate = eventAggregate.PublishWindow?.EndDate,
            ImageUrl = eventAggregate.ImageUrl,
            BannerUrl = eventAggregate.BannerUrl,
            SeoTitle = eventAggregate.SeoTitle,
            SeoDescription = eventAggregate.SeoDescription,
            Categories = eventAggregate.Categories.ToList(),
            Tags = eventAggregate.Tags.ToList(),
            Version = eventAggregate.Version,
            CreatedAt = eventAggregate.CreatedAt,
            UpdatedAt = eventAggregate.UpdatedAt,
            TicketTypes = new List<TicketTypeDto>(),
            PricingRules = new List<PricingRuleDto>(),
            Allocations = new List<AllocationDto>()
        };
    }
}
