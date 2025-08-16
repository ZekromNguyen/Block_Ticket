namespace Event.Application.Common.Models;

/// <summary>
/// Allocation data transfer object
/// </summary>
public record AllocationDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? TicketTypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty; // General, VIP, Press, Staff, etc.
    public int Quantity { get; init; }
    public int AllocatedQuantity { get; init; }
    public int UsedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public string? AccessCode { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public DateTime? AvailableUntil { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public List<string>? AllowedUserIds { get; init; }
    public List<string>? AllowedEmailDomains { get; init; }
    public List<Guid> AllocatedSeatIds { get; init; } = new();
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int Version { get; init; }
}

/// <summary>
/// Create allocation request
/// </summary>
public record CreateAllocationRequest
{
    public Guid? TicketTypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime? ExpiresAt { get; init; }
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Update allocation request
/// </summary>
public record UpdateAllocationRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Get allocations request
/// </summary>
public record GetAllocationsRequest
{
    public Guid? EventId { get; init; }
    public Guid? TicketTypeId { get; init; }
    public string? Type { get; init; }
    public bool? IsActive { get; init; }
    public string? AssignedTo { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Allocation summary DTO
/// </summary>
public record AllocationSummaryDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int TotalAllocations { get; init; }
    public int ActiveAllocations { get; init; }
    public int TotalQuantityAllocated { get; init; }
    public int TotalQuantityUsed { get; init; }
    public int TotalQuantityAvailable { get; init; }
    public decimal OverallUtilizationRate { get; init; }
    public List<AllocationTypeStatsDto> AllocationsByType { get; init; } = new();
}

/// <summary>
/// Allocation type statistics DTO
/// </summary>
public record AllocationTypeStatsDto
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
    public int TotalQuantity { get; init; }
    public int UsedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal UtilizationRate { get; init; }
}

/// <summary>
/// Allocation history DTO
/// </summary>
public record AllocationHistoryDto
{
    public Guid Id { get; init; }
    public Guid AllocationId { get; init; }
    public string Action { get; init; } = string.Empty; // Created, Updated, Allocated, Released, Transferred, Expired
    public string? Details { get; init; }
    public int? QuantityChanged { get; init; }
    public int? QuantityBefore { get; init; }
    public int? QuantityAfter { get; init; }
    public Guid? PerformedByUserId { get; init; }
    public string? PerformedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Allocation report DTO
/// </summary>
public record AllocationReportDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public DateTime ReportGeneratedAt { get; init; }
    public AllocationSummaryDto Summary { get; init; } = null!;
    public List<AllocationDto> Allocations { get; init; } = new();
    public List<AllocationHistoryDto> RecentActivity { get; init; } = new();
}

/// <summary>
/// Bulk allocation operation request
/// </summary>
public record BulkAllocationOperationRequest
{
    public List<Guid> AllocationIds { get; init; } = new();
    public string Operation { get; init; } = string.Empty; // Activate, Deactivate, Delete
    public string? Reason { get; init; }
}

/// <summary>
/// Bulk allocation operation result
/// </summary>
public record BulkAllocationOperationResult
{
    public int TotalRequested { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public List<BulkOperationItemResult> Results { get; init; } = new();
}

/// <summary>
/// Bulk operation item result
/// </summary>
public record BulkOperationItemResult
{
    public Guid AllocationId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Allocation validation result
/// </summary>
public record AllocationValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public AllocationDto? ValidatedAllocation { get; init; }
}
