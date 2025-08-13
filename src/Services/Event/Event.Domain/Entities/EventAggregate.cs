using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Event aggregate root - represents a ticketed event
/// </summary>
public class EventAggregate : BaseAuditableEntity
{
    private readonly List<TicketType> _ticketTypes = new();
    private readonly List<PricingRule> _pricingRules = new();
    private readonly List<Allocation> _allocations = new();
    private readonly List<string> _categories = new();
    private readonly List<string> _tags = new();

    // Basic Properties
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid PromoterId { get; private set; }
    public Guid VenueId { get; private set; }
    public EventStatus Status { get; private set; }
    
    // Scheduling
    public DateTime EventDate { get; private set; }
    public TimeZoneId TimeZone { get; private set; } = null!;
    public DateTimeRange? PublishWindow { get; private set; }
    
    // Marketing
    public string? ImageUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    
    // Versioning
    public int Version { get; private set; }
    public string? ChangeHistory { get; private set; } // JSON
    
    // Navigation Properties
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();
    public IReadOnlyCollection<PricingRule> PricingRules => _pricingRules.AsReadOnly();
    public IReadOnlyCollection<Allocation> Allocations => _allocations.AsReadOnly();
    public IReadOnlyCollection<string> Categories => _categories.AsReadOnly();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    // For EF Core
    private EventAggregate() { }

    public EventAggregate(
        string title,
        string description,
        string slug,
        Guid organizationId,
        Guid promoterId,
        Guid venueId,
        DateTime eventDate,
        TimeZoneId timeZone)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new EventDomainException("Event title cannot be empty");
        
        if (string.IsNullOrWhiteSpace(description))
            throw new EventDomainException("Event description cannot be empty");
        
        if (eventDate <= DateTime.UtcNow)
            throw new EventDomainException("Event date must be in the future");

        Title = title.Trim();
        Description = description.Trim();
        Slug = Slug.FromString(slug);
        OrganizationId = organizationId;
        PromoterId = promoterId;
        VenueId = venueId;
        EventDate = eventDate;
        TimeZone = timeZone;
        Status = EventStatus.Draft;
        Version = 1;

        AddDomainEvent(new EventCreatedDomainEvent(Id, Title, PromoterId, EventDate, VenueId));
    }

    public void UpdateBasicInfo(string title, string description, DateTime eventDate)
    {
        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException("Cannot update cancelled or completed events");

        var changes = new Dictionary<string, object>();
        
        if (Title != title.Trim())
        {
            changes["Title"] = new { Old = Title, New = title.Trim() };
            Title = title.Trim();
        }
        
        if (Description != description.Trim())
        {
            changes["Description"] = new { Old = Description, New = description.Trim() };
            Description = description.Trim();
        }
        
        if (EventDate != eventDate)
        {
            if (eventDate <= DateTime.UtcNow)
                throw new EventDomainException("Event date must be in the future");
            
            changes["EventDate"] = new { Old = EventDate, New = eventDate };
            EventDate = eventDate;
        }

        if (changes.Any())
        {
            Version++;
            AddDomainEvent(new EventUpdatedDomainEvent(Id, Title, changes));
        }
    }

    public void SetPublishWindow(DateTime publishStart, DateTime publishEnd)
    {
        if (publishEnd <= publishStart)
            throw new EventDomainException("Publish end date must be after start date");
        
        if (publishStart >= EventDate)
            throw new EventDomainException("Publish window must end before event date");

        PublishWindow = new DateTimeRange(publishStart, publishEnd, TimeZone);
    }

    public void AddCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new EventDomainException("Category cannot be empty");
        
        var normalizedCategory = category.Trim().ToLowerInvariant();
        if (!_categories.Contains(normalizedCategory))
        {
            _categories.Add(normalizedCategory);
        }
    }

    public void RemoveCategory(string category)
    {
        var normalizedCategory = category.Trim().ToLowerInvariant();
        _categories.Remove(normalizedCategory);
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new EventDomainException("Tag cannot be empty");
        
        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (!_tags.Contains(normalizedTag))
        {
            _tags.Add(normalizedTag);
        }
    }

    public void RemoveTag(string tag)
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();
        _tags.Remove(normalizedTag);
    }

    public void SetMarketingAssets(string? imageUrl, string? bannerUrl)
    {
        ImageUrl = imageUrl?.Trim();
        BannerUrl = bannerUrl?.Trim();
    }

    public void SetSeoInfo(string? seoTitle, string? seoDescription)
    {
        SeoTitle = seoTitle?.Trim();
        SeoDescription = seoDescription?.Trim();
    }

    public void Publish()
    {
        if (Status != EventStatus.Draft && Status != EventStatus.Review)
            throw new EventDomainException($"Cannot publish event in {Status} status");
        
        if (!_ticketTypes.Any())
            throw new EventDomainException("Cannot publish event without ticket types");
        
        if (PublishWindow == null)
            throw new EventDomainException("Cannot publish event without publish window");

        Status = EventStatus.Published;
        Version++;
        
        AddDomainEvent(new EventPublishedDomainEvent(Id, Title, DateTime.UtcNow, EventDate));
    }

    public void StartSale()
    {
        if (Status != EventStatus.Published)
            throw new EventDomainException($"Cannot start sale for event in {Status} status");
        
        if (PublishWindow != null && !PublishWindow.Contains(DateTime.UtcNow))
            throw new EventDomainException("Cannot start sale outside publish window");

        Status = EventStatus.OnSale;
        AddDomainEvent(new EventOnSaleDomainEvent(Id, Title, DateTime.UtcNow));
    }

    public void Cancel(string reason)
    {
        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException($"Cannot cancel event in {Status} status");

        Status = EventStatus.Cancelled;
        Version++;
        
        AddDomainEvent(new EventCancelledDomainEvent(Id, Title, DateTime.UtcNow, reason));
    }

    public void Complete()
    {
        if (Status != EventStatus.OnSale && Status != EventStatus.SoldOut)
            throw new EventDomainException($"Cannot complete event in {Status} status");
        
        if (EventDate > DateTime.UtcNow)
            throw new EventDomainException("Cannot complete event before event date");

        Status = EventStatus.Completed;
        Version++;
    }

    public void Archive()
    {
        if (Status != EventStatus.Completed && Status != EventStatus.Cancelled)
            throw new EventDomainException($"Cannot archive event in {Status} status");

        Status = EventStatus.Archived;
        Version++;
    }

    public bool CanBeModified()
    {
        return Status == EventStatus.Draft || Status == EventStatus.Review;
    }

    public bool IsPublic()
    {
        return Status == EventStatus.Published || 
               Status == EventStatus.OnSale || 
               Status == EventStatus.SoldOut;
    }

    public bool IsOnSale()
    {
        return Status == EventStatus.OnSale &&
               (PublishWindow?.Contains(DateTime.UtcNow) ?? false);
    }

    public void AddTicketType(TicketType ticketType)
    {
        if (!CanBeModified())
            throw new EventDomainException("Cannot modify published events");

        if (_ticketTypes.Any(tt => tt.Code == ticketType.Code))
            throw new EventDomainException($"Ticket type with code '{ticketType.Code}' already exists");

        _ticketTypes.Add(ticketType);
    }

    public void RemoveTicketType(string code)
    {
        if (!CanBeModified())
            throw new EventDomainException("Cannot modify published events");

        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Code == code);
        if (ticketType != null)
        {
            _ticketTypes.Remove(ticketType);
        }
    }

    public TicketType? GetTicketType(string code)
    {
        return _ticketTypes.FirstOrDefault(tt => tt.Code == code);
    }

    public TicketType? GetTicketType(Guid ticketTypeId)
    {
        return _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
    }

    public int GetTotalAvailableCapacity()
    {
        return _ticketTypes.Sum(tt => tt.Capacity.Available);
    }

    public bool HasAvailableTickets()
    {
        return _ticketTypes.Any(tt => tt.IsAvailable());
    }

    public void CheckAndUpdateSoldOutStatus()
    {
        if (Status == EventStatus.OnSale && !HasAvailableTickets())
        {
            Status = EventStatus.SoldOut;
            AddDomainEvent(new EventSoldOutDomainEvent(Id, Title, DateTime.UtcNow));
        }
    }
}
