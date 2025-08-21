using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.PricingRules.Queries.TestPricingRule;

/// <summary>
/// Query to test a pricing rule against sample order data
/// </summary>
public record TestPricingRuleQuery : IRequest<PricingTestResultDto>
{
    public Guid PricingRuleId { get; init; }
    public List<TestOrderItemDto> OrderItems { get; init; } = new();
    public string? CustomerSegment { get; init; }
    public string? DiscountCode { get; init; }
    public DateTime? TestDate { get; init; }
}

/// <summary>
/// Test order item DTO
/// </summary>
public record TestOrderItemDto
{
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
}

/// <summary>
/// Pricing test result DTO
/// </summary>
public record PricingTestResultDto
{
    public bool IsApplicable { get; init; }
    public string? ReasonNotApplicable { get; init; }
    public MoneyDto OriginalAmount { get; init; } = null!;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto FinalAmount { get; init; } = null!;
    public List<TestOrderItemResultDto> ItemResults { get; init; } = new();
    public PricingRuleDto PricingRule { get; init; } = null!;
}

/// <summary>
/// Test order item result DTO
/// </summary>
public record TestOrderItemResultDto
{
    public Guid TicketTypeId { get; init; }
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;
    public MoneyDto LineTotal { get; init; } = null!;
    public MoneyDto DiscountAmount { get; init; } = null!;
    public MoneyDto FinalLineTotal { get; init; } = null!;
    public bool RuleApplied { get; init; }
}
