using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Dtos;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Services;

/// <summary>
/// HTTP implementation of <see cref="IPricingEvaluationService"/> that calls the
/// Event Service's internal pricing evaluation endpoint. Results are not cached
/// because pricing can change between requests.
/// </summary>
public sealed class HttpPricingEvaluationService : IPricingEvaluationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpPricingEvaluationService> _logger;

    public HttpPricingEvaluationService(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpPricingEvaluationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PricingEvaluationResult?> EvaluateAsync(PricingEvaluationRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("event");
        var response = await client.PostAsJsonAsync("/api/v1/internal/pricing/evaluate", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Pricing evaluation for event {EventId} returned {StatusCode}",
                request.EventId, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PricingEvaluationResult>(cancellationToken: cancellationToken);
    }
}

/// <summary>
/// HTTP implementation of <see cref="ICurrencyPolicyService"/> that reads per-event
/// currency policy from Event Service. Results are cached for 60s.
/// </summary>
public sealed class HttpCurrencyPolicyService : ICurrencyPolicyService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HttpCurrencyPolicyService> _logger;

    public HttpCurrencyPolicyService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<HttpCurrencyPolicyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CurrencyPolicyDto?> GetPolicyAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var cacheKey = $"currency-policy:{eventId}";
        if (_cache.TryGetValue<CurrencyPolicyDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient("event");
        var response = await client.GetAsync($"/api/v1/internal/events/{eventId}/currency-policy", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Currency policy for event {EventId} returned {StatusCode}; defaulting to USD",
                eventId, response.StatusCode);
            return null;
        }

        var policy = await response.Content.ReadFromJsonAsync<CurrencyPolicyDto>(cancellationToken: cancellationToken);
        if (policy is not null)
        {
            _cache.Set(cacheKey, policy, CacheTtl);
        }

        return policy;
    }

    public async Task<CurrencyValidationResult> ValidateAsync(Guid eventId, string currency, CancellationToken cancellationToken)
    {
        var policy = await GetPolicyAsync(eventId, cancellationToken);
        if (policy is null)
        {
            // No policy configured — allow with default currency assumption
            return CurrencyValidationResult.Ok(
                new CurrencyPolicyDto(eventId, "USD", new[] { new AllowedCurrency("USD", "US Dollar", null, true) }, 0, 0, Array.Empty<CurrencyFee>()));
        }

        if (!string.Equals(currency, policy.DefaultCurrency, StringComparison.OrdinalIgnoreCase))
        {
            var allowed = policy.AllowedCurrencies.FirstOrDefault(c =>
                string.Equals(c.Code, currency, StringComparison.OrdinalIgnoreCase));
            if (allowed is null || !allowed.IsEnabled)
            {
                return CurrencyValidationResult.Deny(
                    $"Currency '{currency}' is not allowed for this event. Allowed: {string.Join(", ", policy.AllowedCurrencies.Where(c => c.IsEnabled).Select(c => c.Code))}");
            }
        }

        return CurrencyValidationResult.Ok(policy);
    }
}

/// <summary>
/// HTTP implementation of <see cref="IRiskAssessmentService"/> that calls a local
/// risk engine. Falls back to approval when the service is unavailable.
/// </summary>
public sealed class HttpRiskAssessmentService : IRiskAssessmentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpRiskAssessmentService> _logger;

    public HttpRiskAssessmentService(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpRiskAssessmentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RiskAssessmentResult?> AssessAsync(RiskAssessmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("event");
            var response = await client.PostAsJsonAsync("/api/v1/internal/risk/assess", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Risk assessment for user {UserId} event {EventId} returned {StatusCode}; defaulting to approve",
                    request.UserId, request.EventId, response.StatusCode);
                return new RiskAssessmentResult(true, "Low", 0.0m, Array.Empty<RiskSignal>(), null);
            }

            return await response.Content.ReadFromJsonAsync<RiskAssessmentResult>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk assessment failed for user {UserId}; defaulting to approve", request.UserId);
            return new RiskAssessmentResult(true, "Low", 0.0m, Array.Empty<RiskSignal>(), null);
        }
    }
}
