namespace Shared.Contracts.Dtos;

/// <summary>
/// Currency policy for an event defining allowed currencies, conversion rules, and fees.
/// </summary>
public sealed record CurrencyPolicyDto(
    Guid EventId,
    string DefaultCurrency,
    IReadOnlyCollection<AllowedCurrency> AllowedCurrencies,
    decimal ServiceFeePercent,
    decimal ProcessingFeeFixed,
    IReadOnlyCollection<CurrencyFee> CurrencyFees);

/// <summary>
/// An allowed currency with optional conversion rules.
/// </summary>
public sealed record AllowedCurrency(
    string Code,
    string Name,
    decimal? ConversionRateToDefault,
    bool IsEnabled);

/// <summary>
/// Per-currency fee override.
/// </summary>
public sealed record CurrencyFee(
    string CurrencyCode,
    decimal ServiceFeePercent,
    decimal ProcessingFeeFixed);
