using Event.Domain.Enums;

namespace Event.Application.Common.Models;

/// <summary>
/// Date time range DTO
/// </summary>
public record DateTimeRangeDto
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}

/// <summary>
/// Event data transfer object
/// </summary>
public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty; // Alias for Title
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public DateTime StartDateTime { get; init; } // Start time of the event
    public DateTime EndDateTime { get; init; } // End time of the event
    public string TimeZone { get; init; } = string.Empty;
    public DateTime? PublishStartDate { get; init; }
    public DateTime? PublishEndDate { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Status tracking properties
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeRangeDto? PublishWindow { get; init; }
    
    // Navigation properties
    public List<TicketTypeDto> TicketTypes { get; set; } = new();
    public List<PricingRuleDto> PricingRules { get; set; } = new();
    public List<AllocationDto> Allocations { get; set; } = new();
}

/// <summary>
/// Event catalog DTO for public listing
/// </summary>
public record EventCatalogDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    
    // Venue information
    public VenueSummaryDto Venue { get; init; } = null!;
    
    // Pricing information
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string Currency { get; init; } = "USD";
    
    // Availability
    public bool HasAvailability { get; init; }
    public int TotalCapacity { get; init; }
    public int AvailableCapacity { get; init; }
    public int AvailableTickets { get; init; }

    // Ticket types
    public List<TicketTypeSummaryDto> TicketTypes { get; init; } = new();

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Event detail DTO for public display
/// </summary>
public record EventDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    
    // Venue information
    public VenueDto Venue { get; init; } = null!;
    
    // Ticket types
    public List<TicketTypePublicDto> TicketTypes { get; init; } = new();
    
    // Availability
    public EventAvailabilityDto Availability { get; init; } = null!;
}

// CreateEventRequest and UpdateEventRequest are defined in EventRequestDtos.cs

// GetEventsRequest and SearchEventsRequest are defined in EventRequestDtos.cs

/// <summary>
/// Get public events request
/// </summary>
public record GetPublicEventsRequest
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? City { get; init; }
    public List<string>? Categories { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Search public events request
/// </summary>
public record SearchPublicEventsRequest
{
    public string SearchTerm { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? City { get; init; }
    public List<string>? Categories { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Get recommended events request
/// </summary>
public record GetRecommendedEventsRequest
{
    public Guid? UserId { get; init; }
    public List<string>? PreferredCategories { get; init; }
    public string? PreferredCity { get; init; }
    public int MaxResults { get; init; } = 10;
}

/// <summary>
/// Event series DTO
/// </summary>
public record EventSeriesDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public DateTime? SeriesStartDate { get; init; }
    public DateTime? SeriesEndDate { get; init; }
    public int? MaxEvents { get; init; }
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public List<Guid> EventIds { get; init; } = new();
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Create event series request
/// </summary>
public record CreateEventSeriesRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public DateTime? SeriesStartDate { get; init; }
    public DateTime? SeriesEndDate { get; init; }
    public int? MaxEvents { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Update event series request
/// </summary>
public record UpdateEventSeriesRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime? SeriesStartDate { get; init; }
    public DateTime? SeriesEndDate { get; init; }
    public int? MaxEvents { get; init; }
    public bool? IsActive { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public List<string>? Categories { get; init; }
    public List<string>? Tags { get; init; }
}

/// <summary>
/// Get event series request
/// </summary>
public record GetEventSeriesRequest
{
    public Guid? PromoterId { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// PagedResult<T> is defined in PagedResult.cs
