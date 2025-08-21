namespace Event.Application.Common.Models;

/// <summary>
/// Request DTO for creating a new event
/// </summary>
public record CreateEventRequest
{
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid PromoterId { get; init; }
    public Guid VenueId { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
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
/// Request DTO for updating an existing event
/// </summary>
public record UpdateEventRequest
{
    public string? Name { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? StartDateTime { get; init; }
    public DateTime? EndDateTime { get; init; }
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
/// Request DTO for searching events
/// </summary>
public record SearchEventsRequest
{
    public string? SearchTerm { get; init; }
    public string? Query { get; init; }
    public string? Category { get; init; }
    public string? City { get; init; }
    public string? Location { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Status { get; init; }
    public Guid? VenueId { get; init; }
    public Guid? PromoterId { get; init; }
    public List<string>? Categories { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? HasAvailability { get; init; }
    public bool? SortDescending { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; } = "asc";
}

/// <summary>
/// Request DTO for getting events with pagination
/// </summary>
public record GetEventsRequest
{
    public Guid? OrganizationId { get; init; }
    public Guid? PromoterId { get; init; }
    public Guid? VenueId { get; init; }
    public string? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<string>? Categories { get; init; }
    public bool? SortDescending { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "EventDate";
    public string? SortDirection { get; init; } = "asc";
}
