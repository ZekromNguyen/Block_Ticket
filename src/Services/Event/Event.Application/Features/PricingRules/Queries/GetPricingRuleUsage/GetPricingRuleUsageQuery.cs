using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.PricingRules.Queries.GetPricingRuleUsage;

/// <summary>
/// Query to get usage statistics for a pricing rule
/// </summary>
public record GetPricingRuleUsageQuery : IRequest<PricingRuleUsageDto>
{
    public Guid PricingRuleId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Pricing rule usage DTO
/// </summary>
public record PricingRuleUsageDto
{
    public Guid PricingRuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public int TotalUses { get; init; }
    public int? MaxUses { get; init; }
    public int? RemainingUses { get; init; }
    public MoneyDto TotalDiscountAmount { get; init; } = null!;
    public MoneyDto AverageDiscountAmount { get; init; } = null!;
    public DateTime? FirstUsed { get; init; }
    public DateTime? LastUsed { get; init; }
    public List<PricingRuleUsageDetailDto> RecentUsages { get; init; } = new();
    public int TotalUsageCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
