using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Handler for CreateEventCommand
/// </summary>
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly ILogger<CreateEventCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        ILogger<CreateEventCommandHandler> logger,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating event with slug: {Slug} for organization: {OrganizationId}", 
            request.Slug, request.OrganizationId);

        // Validate slug uniqueness
        await ValidateSlugUniqueness(request.Slug, request.OrganizationId, cancellationToken);

        // Validate venue exists
        await ValidateVenueExists(request.VenueId, cancellationToken);

        // Create the event aggregate
        var eventAggregate = CreateEventAggregate(request);

        // Save the event
        var createdEvent = await _eventRepository.AddAsync(eventAggregate, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created event {EventId} with slug: {Slug}", 
            createdEvent.Id, request.Slug);

        // Convert to DTO
        return MapToDto(createdEvent);
    }

    private async Task ValidateSlugUniqueness(string slug, Guid organizationId, CancellationToken cancellationToken)
    {
        var isUnique = await _eventRepository.IsSlugUniqueAsync(slug, organizationId, null, cancellationToken);
        if (!isUnique)
        {
            throw new InvalidOperationException($"An event with slug '{slug}' already exists in this organization");
        }
    }

    private async Task ValidateVenueExists(Guid venueId, CancellationToken cancellationToken)
    {
        var venueExists = await _venueRepository.ExistsAsync(venueId, cancellationToken);
        if (!venueExists)
        {
            throw new InvalidOperationException($"Venue with ID '{venueId}' does not exist");
        }
    }

    private EventAggregate CreateEventAggregate(CreateEventCommand request)
    {
        // Create value objects
        var timeZone = TimeZoneId.FromString(request.TimeZone);
        
        DateTimeRange? publishWindow = null;
        if (request.PublishStartDate.HasValue && request.PublishEndDate.HasValue)
        {
            publishWindow = new DateTimeRange(request.PublishStartDate.Value, request.PublishEndDate.Value);
        }

        // Create the event aggregate
        var eventAggregate = new EventAggregate(
            title: request.Title,
            description: request.Description,
            slug: request.Slug,
            organizationId: request.OrganizationId,
            promoterId: request.PromoterId,
            venueId: request.VenueId,
            eventDate: request.EventDate,
            timeZone: timeZone);

        // Set optional properties
        if (publishWindow != null)
        {
            eventAggregate.SetPublishWindow(publishWindow.StartDate, publishWindow.EndDate);
        }

        if (!string.IsNullOrEmpty(request.ImageUrl))
        {
            eventAggregate.SetImageUrl(request.ImageUrl);
        }

        if (!string.IsNullOrEmpty(request.BannerUrl))
        {
            eventAggregate.SetBannerUrl(request.BannerUrl);
        }

        if (!string.IsNullOrEmpty(request.SeoTitle) || !string.IsNullOrEmpty(request.SeoDescription))
        {
            eventAggregate.SetSeoMetadata(request.SeoTitle, request.SeoDescription);
        }

        // Add categories and tags
        foreach (var category in request.Categories)
        {
            eventAggregate.AddCategory(category);
        }

        foreach (var tag in request.Tags)
        {
            eventAggregate.AddTag(tag);
        }

        return eventAggregate;
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
            TicketTypes = new List<TicketTypeDto>(), // Will be populated when ticket types are added
            PricingRules = new List<PricingRuleDto>(), // Will be populated when pricing rules are added
            Allocations = new List<AllocationDto>() // Will be populated when allocations are added
        };
    }
}
