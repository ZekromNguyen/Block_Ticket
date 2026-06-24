namespace Shared.Contracts.Dtos;

/// <summary>
/// Risk assessment result for a checkout request.
/// </summary>
public sealed record RiskAssessmentResult(
    bool Approved,
    string RiskLevel,
    decimal RiskScore,
    IReadOnlyCollection<RiskSignal> Signals,
    string? ReviewReason);

/// <summary>
/// An individual risk signal detected during assessment.
/// </summary>
public sealed record RiskSignal(
    string Type,
    string Description,
    decimal Score,
    string Severity);

/// <summary>
/// Risk assessment request sent before checkout confirmation.
/// </summary>
public sealed record RiskAssessmentRequest(
    Guid UserId,
    Guid EventId,
    decimal TotalAmount,
    string Currency,
    string PaymentMethod,
    string? UserIpAddress,
    int TicketQuantity,
    string? DiscountCode);
