using Shared.Contracts.Dtos;

namespace Ticketing.Application.Interfaces;

/// <summary>
/// Cross-service boundary into Event Service's pricing engine. Ticketing calls this
/// at reservation time to snapshot the evaluated prices, discounts, and applied rules
/// so the final charge is deterministic even if pricing rules change later.
/// </summary>
public interface IPricingEvaluationService
{
    Task<PricingEvaluationResult?> EvaluateAsync(PricingEvaluationRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Cross-service boundary for currency policy. Ticketing validates the buyer's
/// chosen currency against the event's allowed currencies and fee structure.
/// </summary>
public interface ICurrencyPolicyService
{
    Task<CurrencyPolicyDto?> GetPolicyAsync(Guid eventId, CancellationToken cancellationToken);
    Task<CurrencyValidationResult> ValidateAsync(Guid eventId, string currency, CancellationToken cancellationToken);
}

public sealed record CurrencyValidationResult(bool Allowed, string? Reason, CurrencyPolicyDto? Policy)
{
    public static CurrencyValidationResult Ok(CurrencyPolicyDto policy) => new(true, null, policy);
    public static CurrencyValidationResult Deny(string reason) => new(false, reason, null);
}

/// <summary>
/// Risk assessment boundary used by Ticketing before confirming payment.
/// </summary>
public interface IRiskAssessmentService
{
    Task<RiskAssessmentResult?> AssessAsync(RiskAssessmentRequest request, CancellationToken cancellationToken);
}
