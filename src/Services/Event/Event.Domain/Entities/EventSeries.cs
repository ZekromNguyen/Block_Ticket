using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents a series of related events (e.g., concert tour, festival series)
/// </summary>
public class EventSeries : BaseAuditableEntity
{
    private readonly List<Guid> _eventIds = new();
    private readonly List<string> _categories = new();
    private readonly List<string> _tags = new();

    // Basic Properties
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Slug Slug { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    public Guid PromoterId { get; private set; }
    
    // Series Metadata
    public DateTime? SeriesStartDate { get; private set; }
    public DateTime? SeriesEndDate { get; private set; }
    public int? MaxEvents { get; private set; }
    public bool IsActive { get; private set; }
    
    // Marketing
    public string? ImageUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? SeoTitle { get; private set; }
    public string? SeoDescription { get; private set; }
    
    // Versioning
    public int Version { get; private set; }
    
    // Navigation Properties
    public IReadOnlyCollection<Guid> EventIds => _eventIds.AsReadOnly();
    public IReadOnlyCollection<string> Categories => _categories.AsReadOnly();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    // For EF Core
    private EventSeries() { }

    public EventSeries(
        string name,
        string slug,
        Guid organizationId,
        Guid promoterId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Event series name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim();
        Slug = Slug.FromString(slug);
        OrganizationId = organizationId;
        PromoterId = promoterId;
        IsActive = true;
        Version = 1;
    }

    public void UpdateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Event series name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim();
        Version++;
    }

    public void SetSeriesDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value <= startDate.Value)
            throw new EventDomainException("Series end date must be after start date");

        SeriesStartDate = startDate;
        SeriesEndDate = endDate;
        Version++;
    }

    public void SetMaxEvents(int? maxEvents)
    {
        if (maxEvents.HasValue && maxEvents.Value <= 0)
            throw new EventDomainException("Max events must be greater than zero");
        
        if (maxEvents.HasValue && _eventIds.Count > maxEvents.Value)
            throw new EventDomainException($"Cannot set max events below current event count ({_eventIds.Count})");

        MaxEvents = maxEvents;
        Version++;
    }

    public void AddEvent(Guid eventId)
    {
        if (!IsActive)
            throw new EventDomainException("Cannot add events to inactive series");
        
        if (MaxEvents.HasValue && _eventIds.Count >= MaxEvents.Value)
            throw new EventDomainException($"Series has reached maximum event limit ({MaxEvents.Value})");
        
        if (_eventIds.Contains(eventId))
            throw new EventDomainException("Event is already part of this series");

        _eventIds.Add(eventId);
        Version++;
    }

    public void RemoveEvent(Guid eventId)
    {
        if (_eventIds.Remove(eventId))
        {
            Version++;
        }
    }

    public void AddCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new EventDomainException("Category cannot be empty");
        
        var normalizedCategory = category.Trim().ToLowerInvariant();
        if (!_categories.Contains(normalizedCategory))
        {
            _categories.Add(normalizedCategory);
            Version++;
        }
    }

    public void RemoveCategory(string category)
    {
        var normalizedCategory = category.Trim().ToLowerInvariant();
        if (_categories.Remove(normalizedCategory))
        {
            Version++;
        }
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new EventDomainException("Tag cannot be empty");
        
        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (!_tags.Contains(normalizedTag))
        {
            _tags.Add(normalizedTag);
            Version++;
        }
    }

    public void RemoveTag(string tag)
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (_tags.Remove(normalizedTag))
        {
            Version++;
        }
    }

    public void SetMarketingAssets(string? imageUrl, string? bannerUrl)
    {
        ImageUrl = imageUrl?.Trim();
        BannerUrl = bannerUrl?.Trim();
        Version++;
    }

    public void SetSeoInfo(string? seoTitle, string? seoDescription)
    {
        SeoTitle = seoTitle?.Trim();
        SeoDescription = seoDescription?.Trim();
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

    public void SetSeriesDateRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate <= startDate)
            throw new EventDomainException("Series end date must be after start date");

        SeriesStartDate = startDate;
        SeriesEndDate = endDate;
        Version++;
    }

    // Overload for Application layer compatibility
    public void SetSeriesDateRange(DateTimeRange dateRange)
    {
        SeriesStartDate = dateRange.StartDate;
        SeriesEndDate = dateRange.EndDate;
        Version++;
    }

    // Property for compatibility with Application layer
    public DateTimeRange? SeriesDateRange => GetSeriesDateRange();

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            Version++;
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            Version++;
        }
    }

    public bool CanAddMoreEvents()
    {
        return IsActive && (!MaxEvents.HasValue || _eventIds.Count < MaxEvents.Value);
    }

    public bool IsWithinSeriesDates(DateTime eventDate)
    {
        if (SeriesStartDate.HasValue && eventDate < SeriesStartDate.Value)
            return false;
        
        if (SeriesEndDate.HasValue && eventDate > SeriesEndDate.Value)
            return false;

        return true;
    }

    public int GetEventCount()
    {
        return _eventIds.Count;
    }

    public int GetRemainingEventSlots()
    {
        if (!MaxEvents.HasValue)
            return int.MaxValue;
        
        return Math.Max(0, MaxEvents.Value - _eventIds.Count);
    }

    public bool HasEvent(Guid eventId)
    {
        return _eventIds.Contains(eventId);
    }

    public void ClearEvents()
    {
        if (_eventIds.Any())
        {
            _eventIds.Clear();
            Version++;
        }
    }

    public void UpdateSlug(string newSlug)
    {
        var slug = Slug.FromString(newSlug);
        if (!Slug.Equals(slug))
        {
            Slug = slug;
            Version++;
        }
    }

    public bool IsSeriesActive()
    {
        if (!IsActive)
            return false;

        var now = DateTime.UtcNow;
        
        if (SeriesStartDate.HasValue && now < SeriesStartDate.Value)
            return false;
        
        if (SeriesEndDate.HasValue && now > SeriesEndDate.Value)
            return false;

        return true;
    }

    public DateTimeRange? GetSeriesDateRange()
    {
        if (!SeriesStartDate.HasValue || !SeriesEndDate.HasValue)
            return null;

        // Use UTC timezone for series date range
        return new DateTimeRange(SeriesStartDate.Value, SeriesEndDate.Value, "UTC");
    }

    public void ValidateEventForSeries(DateTime eventDate)
    {
        if (!IsActive)
            throw new EventDomainException("Cannot add events to inactive series");
        
        if (!CanAddMoreEvents())
            throw new EventDomainException("Series has reached maximum event capacity");
        
        if (!IsWithinSeriesDates(eventDate))
            throw new EventDomainException("Event date is outside series date range");
    }
}
