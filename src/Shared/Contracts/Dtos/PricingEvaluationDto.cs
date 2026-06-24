namespace Shared.Contracts.Dtos;

/// <summary>
/// Request to evaluate pricing rules for a set of reservation items at checkout time.
/// </summary>
public sealed record PricingEvaluationRequest(
    Guid EventId,
    IReadOnlyCollection<PricingLineItem> Items,
    string? DiscountCode,
    Guid? UserId,
    string Currency);

/// <summary>
/// A single line item to evaluate pricing for.
/// </summary>
public sealed record PricingLineItem(
    Guid TicketTypeId,
    string TicketTypeName,
    decimal BaseUnitPrice,
    int Quantity);

/// <summary>
/// The result of evaluating pricing rules for a reservation.
/// </summary>
public sealed record PricingEvaluationResult(
    Guid EventId,
    string Currency,
    IReadOnlyCollection<PricedLineItem> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ServiceFee,
    decimal ProcessingFee,
    decimal TotalAmount,
    IReadOnlyCollection<AppliedPricingRule> AppliedRules);

/// <summary>
/// A line item with its final evaluated price.
/// </summary>
public sealed record PricedLineItem(
    Guid TicketTypeId,
    string TicketTypeName,
    decimal OriginalUnitPrice,
    decimal FinalUnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal DiscountAmount);

/// <summary>
/// A pricing rule that was applied during evaluation.
/// </summary>
public sealed record AppliedPricingRule(
    Guid RuleId,
    string Name,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal EffectiveDiscount);
