using Event.Domain.Enums;

namespace Event.Application.Common.Models;

/// <summary>
/// Pricing rule usage DTO
/// </summary>
public record PricingRuleUsageDto
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public int TotalUses { get; init; }
    public int CurrentUses { get; init; }
    public int MaxUses { get; init; }
    public DateTime? FirstUsed { get; init; }
    public DateTime? LastUsed { get; init; }
    public MoneyDto TotalDiscountAmount { get; init; } = null!;
    public List<PricingRuleUsageDetailDto> RecentUsages { get; init; } = new();
}

/// <summary>
/// Pricing rule usage detail DTO
/// </summary>
public record PricingRuleUsageDetailDto
{
    public Guid UsageId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? UserId { get; init; }
    public MoneyDto DiscountAmount { get; init; } = null!;
    public DateTime UsedAt { get; init; }
    public string? CustomerEmail { get; init; }
}

/// <summary>
/// Get pricing rules request
/// </summary>
public record GetPricingRulesRequest
{
    public Guid? EventId { get; init; }
    public string? Type { get; init; }
    public bool? IsActive { get; init; }
    public DateTime? EffectiveDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

/// <summary>
/// Pricing rule summary DTO
/// </summary>
public record PricingRuleSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string? DiscountCode { get; init; }
    public int CurrentUses { get; init; }
    public int? MaxUses { get; init; }
}

/// <summary>
/// Pricing rule validation result
/// </summary>
public record PricingRuleValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public PricingRuleDto? ValidatedRule { get; init; }
}

/// <summary>
/// Pricing rule conflict DTO
/// </summary>
public record PricingRuleConflictDto
{
    public Guid RuleId1 { get; init; }
    public Guid RuleId2 { get; init; }
    public string ConflictType { get; init; } = string.Empty; // OverlappingPeriod, SameDiscountCode, PriorityConflict
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty; // High, Medium, Low
    public string? RecommendedAction { get; init; }
}

/// <summary>
/// Pricing rule performance DTO
/// </summary>
public record PricingRulePerformanceDto
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public int TotalApplications { get; init; }
    public MoneyDto TotalDiscountAmount { get; init; } = null!;
    public decimal AverageDiscountAmount { get; init; }
    public int UniqueCustomers { get; init; }
    public decimal ConversionRate { get; init; }
    public DateTime? FirstUsed { get; init; }
    public DateTime? LastUsed { get; init; }
    public List<DailyRuleUsageDto> DailyUsage { get; init; } = new();
}

/// <summary>
/// Daily rule usage DTO
/// </summary>
public record DailyRuleUsageDto
{
    public DateTime Date { get; init; }
    public int Applications { get; init; }
    public MoneyDto TotalDiscount { get; init; } = null!;
    public int UniqueCustomers { get; init; }
}

/// <summary>
/// Bulk pricing rule operation request
/// </summary>
public record BulkPricingRuleOperationRequest
{
    public List<Guid> RuleIds { get; init; } = new();
    public string Operation { get; init; } = string.Empty; // Activate, Deactivate, Delete, UpdatePriority
    public object? OperationData { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Bulk pricing rule operation result
/// </summary>
public record BulkPricingRuleOperationResult
{
    public int TotalRequested { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public List<BulkPricingRuleOperationItemResult> Results { get; init; } = new();
}

/// <summary>
/// Bulk pricing rule operation item result
/// </summary>
public record BulkPricingRuleOperationItemResult
{
    public Guid RuleId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public PricingRuleDto? UpdatedRule { get; init; }
}

/// <summary>
/// Pricing rule test result DTO
/// </summary>
public record PricingRuleTestResult
{
    public Guid RuleId { get; init; }
    public bool WouldApply { get; init; }
    public string? ReasonNotApplied { get; init; }
    public MoneyDto? DiscountAmount { get; init; }
    public MoneyDto? FinalPrice { get; init; }
    public List<string> ConditionResults { get; init; } = new();
}

/// <summary>
/// Pricing rule optimization suggestion DTO
/// </summary>
public record PricingRuleOptimizationSuggestionDto
{
    public Guid RuleId { get; init; }
    public string SuggestionType { get; init; } = string.Empty; // IncreaseDiscount, AdjustPeriod, ChangeTargeting
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public object? SuggestedChanges { get; init; }
    public decimal ExpectedImpact { get; init; }
    public int Priority { get; init; } // 1 = High, 2 = Medium, 3 = Low
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Pricing rule analytics DTO
/// </summary>
public record PricingRuleAnalyticsDto
{
    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public int TotalRules { get; init; }
    public int ActiveRules { get; init; }
    public int TotalApplications { get; init; }
    public MoneyDto TotalDiscountsGiven { get; init; } = null!;
    public decimal AverageDiscountPercentage { get; init; }
    public int UniqueCustomersServed { get; init; }
    public List<PricingRulePerformanceDto> TopPerformingRules { get; init; } = new();
    public List<PricingRuleConflictDto> Conflicts { get; init; } = new();
    public List<PricingRuleOptimizationSuggestionDto> Suggestions { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}
