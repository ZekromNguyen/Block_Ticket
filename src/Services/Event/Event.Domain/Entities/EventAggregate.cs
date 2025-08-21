using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using NpgsqlTypes;
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
    public string Name { get; private set; } = string.Empty; // Alias for Title for backward compatibility
    public string Description { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid PromoterId { get; private set; }
    public Guid VenueId { get; private set; }
    public EventStatus Status { get; private set; }

    // Scheduling
    public DateTime EventDate { get; private set; }
    public DateTime StartDateTime { get; private set; } // Start time of the event
    public DateTime EndDateTime { get; private set; } // End time of the event
    public TimeZoneId TimeZone { get; private set; } = null!;
    public DateTimeRange? PublishWindow { get; private set; }

    // Status Tracking
    public DateTime? PublishedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Marketing
    public string? ImageUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    
    // Capacity
    public int TotalCapacity => _ticketTypes.Sum(tt => tt.Capacity.Total);

    // Versioning
    public int Version { get; private set; }
    public string? ChangeHistory { get; private set; } // JSON

    // Search (PostgreSQL specific)
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; private set; } = null!;

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
        TimeZoneId timeZone,
        DateTime? startDateTime = null,
        DateTime? endDateTime = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new EventDomainException("Event title cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new EventDomainException("Event description cannot be empty");

        if (eventDate <= DateTime.UtcNow)
            throw new EventDomainException("Event date must be in the future");

        Title = title.Trim();
        Name = title.Trim(); // Set Name as alias for Title
        Description = description.Trim();
        Slug = Slug.FromString(slug);
        OrganizationId = organizationId;
        PromoterId = promoterId;
        VenueId = venueId;
        EventDate = eventDate;
        StartDateTime = startDateTime ?? eventDate;
        EndDateTime = endDateTime ?? eventDate.AddHours(2); // Default 2-hour duration
        TimeZone = timeZone;
        Status = EventStatus.Draft;
        Version = 1;

        AddDomainEvent(new EventCreatedDomainEvent(Id, Title, PromoterId, EventDate, VenueId));
    }

    /// <summary>
    /// Factory method to create a new event aggregate
    /// </summary>
    public static EventAggregate CreateNew(
        Guid organizationId,
        string title,
        string description,
        List<string> categories,
        Guid promoterId,
        Guid venueId,
        DateTime eventDate,
        TimeZoneId timeZone,
        Guid createdBy)
    {
        var slug = Slug.FromString(title).Value;
        var eventAggregate = new EventAggregate(
            title,
            description,
            slug,
            organizationId,
            promoterId,
            venueId,
            eventDate,
            timeZone);

        // Add categories
        foreach (var category in categories)
        {
            eventAggregate.AddCategory(category);
        }

        return eventAggregate;
    }

    /// <summary>
    /// Overload for test scenarios with simpler parameters
    /// </summary>
    public static EventAggregate CreateNew(
        string title,
        string description,
        Guid promoterId,
        Guid venueId,
        DateTime eventDate,
        TimeZoneId timeZone)
    {
        var organizationId = Guid.NewGuid(); // Default for tests
        var slug = Slug.FromString(title).Value;
        return new EventAggregate(
            title,
            description,
            slug,
            organizationId,
            promoterId,
            venueId,
            eventDate,
            timeZone);
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

    // Overload for Application layer compatibility
    public void SetPublishWindow(DateTime publishStart)
    {
        // Default to 24 hours before event date
        var publishEnd = EventDate.AddHours(-1);
        SetPublishWindow(publishStart, publishEnd);
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

    // Individual update methods for Application layer
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new EventDomainException("Event title cannot be empty");

        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException("Cannot update cancelled or completed events");

        Title = title.Trim();
        Version++;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new EventDomainException("Event description cannot be empty");

        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException("Cannot update cancelled or completed events");

        Description = description.Trim();
        Version++;
    }

    public void UpdateEventDate(DateTime eventDate)
    {
        if (eventDate <= DateTime.UtcNow)
            throw new EventDomainException("Event date must be in the future");

        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException("Cannot update cancelled or completed events");

        EventDate = eventDate;
        Version++;
    }

    public void UpdateTimeZone(TimeZoneId timeZone)
    {
        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException("Cannot update cancelled or completed events");

        TimeZone = timeZone;
        Version++;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl?.Trim();
        Version++;
    }

    public void SetBannerUrl(string? bannerUrl)
    {
        BannerUrl = bannerUrl?.Trim();
        Version++;
    }

    public void SetSeoMetadata(string? seoTitle, string? seoDescription)
    {
        SeoTitle = seoTitle?.Trim();
        SeoDescription = seoDescription?.Trim();
        Version++;
    }

    public void ClearCategories()
    {
        _categories.Clear();
        Version++;
    }

    public void ClearTags()
    {
        _tags.Clear();
        Version++;
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
        PublishedAt = DateTime.UtcNow;
        Version++;

        AddDomainEvent(new EventPublishedDomainEvent(Id, Title, DateTime.UtcNow, EventDate));
    }

    // Overload for Application layer
    public void Publish(DateTime publishedAt)
    {
        if (Status != EventStatus.Draft && Status != EventStatus.Review)
            throw new EventDomainException($"Cannot publish event in {Status} status");

        if (!_ticketTypes.Any())
            throw new EventDomainException("Cannot publish event without ticket types");

        Status = EventStatus.Published;
        PublishedAt = publishedAt;
        Version++;

        AddDomainEvent(new EventPublishedDomainEvent(Id, Title, publishedAt, EventDate));
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
        Cancel(reason, DateTime.UtcNow);
    }

    // Overload for Application layer
    public void Cancel(string reason, DateTime cancelledAt)
    {
        if (Status == EventStatus.Cancelled || Status == EventStatus.Completed)
            throw new EventDomainException($"Cannot cancel event in {Status} status");

        Status = EventStatus.Cancelled;
        CancelledAt = cancelledAt;
        CancellationReason = reason;
        Version++;

        AddDomainEvent(new EventCancelledDomainEvent(Id, Title, cancelledAt, reason));
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

    /// <summary>
    /// Check if the event allows resale of tickets
    /// </summary>
    public bool AllowsResale()
    {
        // Default business rule: allow resale if event is not cancelled and is more than 24 hours away
        return Status != EventStatus.Cancelled &&
               EventDate > DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Check if the event allows refunds
    /// </summary>
    public bool AllowsRefunds()
    {
        // Default business rule: allow refunds if event is not cancelled and is more than the cutoff period away
        return Status != EventStatus.Cancelled &&
               EventDate > DateTime.UtcNow.AddDays(RefundCutoffDays);
    }

    /// <summary>
    /// Number of days before event when refunds are no longer allowed
    /// </summary>
    public int RefundCutoffDays { get; private set; } = 7; // Default 7 days
}
