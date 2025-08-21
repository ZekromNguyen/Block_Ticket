using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Event.Application.Services;

/// <summary>
/// Application service for event management
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating event {EventName}", request.Name);

        // Create TimeZone value object
        var timeZone = new TimeZoneId(request.TimeZone);

        // Use the factory method to create the event aggregate
        var eventAggregate = EventAggregate.CreateNew(
            request.OrganizationId,
            request.Title, // Use Title as the primary name
            request.Description,
            request.Categories,
            request.PromoterId,
            request.VenueId,
            request.EventDate,
            timeZone,
            _currentUserService.UserId ?? Guid.Empty
        );

        await _eventRepository.AddAsync(eventAggregate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created event {EventId}", eventAggregate.Id);

        return MapToDto(eventAggregate);
    }

    public async Task<EventDto> UpdateEventAsync(Guid eventId, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating event {EventId}", eventId);

        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event {eventId} not found");
        }

        // Update basic info using domain method if values are provided
        if (!string.IsNullOrEmpty(request.Title) || !string.IsNullOrEmpty(request.Description) || request.EventDate.HasValue)
        {
            var title = request.Title ?? eventAggregate.Title;
            var description = request.Description ?? eventAggregate.Description;
            var eventDate = request.EventDate ?? eventAggregate.EventDate;

            eventAggregate.UpdateBasicInfo(title, description, eventDate);
        }

        await _eventRepository.UpdateAsync(eventAggregate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated event {EventId}", eventId);

        return MapToDto(eventAggregate);
    }

    public async Task<EventDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        return eventAggregate != null ? MapToDto(eventAggregate) : null;
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var eventAggregate = await _eventRepository.GetBySlugAsync(slug, organizationId, cancellationToken);
        return eventAggregate != null ? MapToDto(eventAggregate) : null;
    }

    public async Task<PagedResult<EventDto>> GetEventsAsync(GetEventsRequest request, CancellationToken cancellationToken = default)
    {
        var (events, totalCount) = await _eventRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate: null, // Add filtering logic here
            orderBy: q => q.OrderBy(e => e.StartDateTime));

        var eventDtos = events.Select(MapToDto).ToList();

        return new PagedResult<EventDto>
        {
            Items = eventDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResult<EventDto>> SearchEventsAsync(SearchEventsRequest request, CancellationToken cancellationToken = default)
    {
        var (events, totalCount) = await _eventRepository.SearchEventsAsync(
            request.SearchTerm,
            request.StartDate,
            request.EndDate,
            request.VenueId,
            request.Categories,
            request.MinPrice,
            request.MaxPrice,
            request.HasAvailability,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var eventDtos = events.Select(MapToDto).ToList();

        return new PagedResult<EventDto>
        {
            Items = eventDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<EventDto> PublishEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing event {EventId}", eventId);

        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event {eventId} not found");
        }

        // Use domain method to publish the event
        eventAggregate.Publish(_dateTimeProvider.UtcNow);

        await _eventRepository.UpdateAsync(eventAggregate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Published event {EventId}", eventId);

        return MapToDto(eventAggregate);
    }

    public async Task<EventDto> CancelEventAsync(Guid eventId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling event {EventId}", eventId);

        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event {eventId} not found");
        }

        // Use domain method to cancel the event
        eventAggregate.Cancel(reason, _dateTimeProvider.UtcNow);

        await _eventRepository.UpdateAsync(eventAggregate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled event {EventId}", eventId);

        return MapToDto(eventAggregate);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting event {EventId}", eventId);

        var eventAggregate = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            return false;
        }

        await _eventRepository.DeleteAsync(eventAggregate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted event {EventId}", eventId);

        return true;
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        return await _eventRepository.IsSlugAvailableAsync(slug, organizationId, excludeEventId, cancellationToken);
    }

    private static EventDto MapToDto(EventAggregate eventAggregate)
    {
        return new EventDto
        {
            Id = eventAggregate.Id,
            Name = eventAggregate.Name,
            Title = eventAggregate.Title,
            Description = eventAggregate.Description,
            Slug = eventAggregate.Slug.Value,
            OrganizationId = eventAggregate.OrganizationId,
            PromoterId = eventAggregate.PromoterId,
            VenueId = eventAggregate.VenueId,
            EventDate = eventAggregate.EventDate,
            StartDateTime = eventAggregate.StartDateTime,
            EndDateTime = eventAggregate.EndDateTime,
            TimeZone = eventAggregate.TimeZone.Value,
            Status = eventAggregate.Status.ToString(),
            ImageUrl = eventAggregate.ImageUrl,
            BannerUrl = eventAggregate.BannerUrl,
            SeoTitle = eventAggregate.SeoTitle,
            SeoDescription = eventAggregate.SeoDescription,
            Categories = eventAggregate.Categories.ToList(),
            Tags = eventAggregate.Tags.ToList(),
            Version = eventAggregate.Version,
            TotalCapacity = eventAggregate.TotalCapacity,
            PublishedAt = eventAggregate.PublishedAt,
            CancelledAt = eventAggregate.CancelledAt,
            CancellationReason = eventAggregate.CancellationReason,
            CreatedAt = eventAggregate.CreatedAt,
            UpdatedAt = eventAggregate.UpdatedAt
        };
    }
}
