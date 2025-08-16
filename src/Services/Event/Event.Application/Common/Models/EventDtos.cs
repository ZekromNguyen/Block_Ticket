using Event.Domain.Enums;

namespace Event.Application.Common.Models;

/// <summary>
/// Event data transfer object
/// </summary>
public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
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

/// <summary>
/// Create event request
/// </summary>
public record CreateEventRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public DateTime EventDate { get; init; }
    public string TimeZone { get; init; } = string.Empty;
    public DateTime? PublishStartDate { get; init; }
    public DateTime? PublishEndDate { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Update event request
/// </summary>
public record UpdateEventRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? EventDate { get; init; }
    public string? TimeZone { get; init; }
    public DateTime? PublishStartDate { get; init; }
    public DateTime? PublishEndDate { get; init; }
    public string? ImageUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public List<string>? Categories { get; init; }
    public List<string>? Tags { get; init; }
}

/// <summary>
/// Get events request
/// </summary>
public record GetEventsRequest
{
    public Guid? PromoterId { get; init; }
    public Guid? VenueId { get; init; }
    public EventStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<string>? Categories { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Search events request
/// </summary>
public record SearchEventsRequest
{
    public string? SearchTerm { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? VenueId { get; init; }
    public string? City { get; init; }
    public List<string>? Categories { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? HasAvailability { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "EventDate";
    public bool SortDescending { get; init; }
}

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

/// <summary>
/// Paged result wrapper
/// </summary>
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
