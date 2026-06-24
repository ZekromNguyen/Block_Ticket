using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Dtos;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Services;

/// <summary>
/// HTTP implementation of <see cref="ISeatMapAvailabilityService"/> that talks to the
/// Event Service's internal seat-hold endpoints. Snapshot reads are cached for 30s
/// to avoid hammering the Event Service on busy pages.
/// </summary>
public sealed class HttpSeatMapAvailabilityService : ISeatMapAvailabilityService
{
    private static readonly TimeSpan SnapshotCacheTtl = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HttpSeatMapAvailabilityService> _logger;

    public HttpSeatMapAvailabilityService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<HttpSeatMapAvailabilityService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SeatAvailabilitySnapshotDto?> GetSnapshotAsync(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken)
    {
        var cacheKey = $"seat-snapshot:{eventId}:{ticketTypeId}";
        if (_cache.TryGetValue<SeatAvailabilitySnapshotDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient("event");
        var response = await client.GetAsync($"/api/v1/internal/events/{eventId}/ticket-types/{ticketTypeId}/seat-snapshot", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Seat availability snapshot for event {EventId}, ticket type {TicketTypeId} returned {Status}",
                eventId, ticketTypeId, response.StatusCode);
            return null;
        }

        var snapshot = await response.Content.ReadFromJsonAsync<SeatAvailabilitySnapshotDto>(cancellationToken: cancellationToken);
        if (snapshot is not null)
        {
            _cache.Set(cacheKey, snapshot, SnapshotCacheTtl);
        }

        return snapshot;
    }

    public async Task<SeatHoldResponseDto?> HoldSeatsAsync(SeatHoldRequestDto request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("event");
        var response = await client.PostAsJsonAsync("/api/v1/internal/seat-holds", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Seat hold for event {EventId} owner {Owner} returned {Status}",
                request.EventId, request.HoldOwner, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SeatHoldResponseDto>(cancellationToken: cancellationToken);
    }

    public async Task ReleaseSeatsAsync(Guid eventId, Guid ticketTypeId, string holdOwner, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("event");
        var response = await client.DeleteAsync(
            $"/api/v1/internal/events/{eventId}/ticket-types/{ticketTypeId}/seat-holds/{holdOwner}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Seat release for event {EventId} owner {Owner} returned {Status}",
                eventId, holdOwner, response.StatusCode);
        }
    }

    public async Task ConfirmSeatsAsync(Guid eventId, Guid ticketTypeId, string holdOwner, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("event");
        var response = await client.PutAsync(
            $"/api/v1/internal/events/{eventId}/ticket-types/{ticketTypeId}/seat-holds/{holdOwner}/confirm",
            content: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Seat confirm for event {EventId} owner {Owner} returned {Status}",
                eventId, holdOwner, response.StatusCode);
        }
    }
}

/// <summary>
/// HTTP implementation of <see cref="ITicketResalePolicy"/> that consults Event Service
/// for the per-event resale policy and rejects listings above the allowed ceiling.
/// </summary>
public sealed class HttpTicketResalePolicy : ITicketResalePolicy
{
    private static readonly TimeSpan PolicyCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HttpTicketResalePolicy> _logger;

    public HttpTicketResalePolicy(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<HttpTicketResalePolicy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ResalePolicyDto> GetPolicyAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var cacheKey = $"resale-policy:{eventId}";
        if (_cache.TryGetValue<ResalePolicyDto>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient("event");
        var response = await client.GetAsync($"/api/v1/internal/events/{eventId}/resale-policy", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Resale policy lookup for event {EventId} returned {Status}; using default allow",
                eventId, response.StatusCode);
            return new ResalePolicyDto(eventId, true, null, null, null);
        }

        var policy = await response.Content.ReadFromJsonAsync<ResalePolicyDto>(cancellationToken: cancellationToken);
        var effective = policy ?? new ResalePolicyDto(eventId, true, null, null, null);
        _cache.Set(cacheKey, effective, PolicyCacheTtl);
        return effective;
    }

    public async Task<ResalePolicyCheckResult> CheckAsync(Guid eventId, decimal originalPrice, decimal requestedPrice, CancellationToken cancellationToken)
    {
        if (originalPrice <= 0)
        {
            return ResalePolicyCheckResult.Allow();
        }

        var policy = await GetPolicyAsync(eventId, cancellationToken);
        if (!policy.AllowResale)
        {
            return ResalePolicyCheckResult.Deny("Resale is not allowed for this event");
        }

        if (policy.MaxResalePrice is { } maxPrice && requestedPrice > maxPrice)
        {
            return ResalePolicyCheckResult.Deny($"Resale price exceeds event maximum of {maxPrice}");
        }

        if (policy.MinResalePrice is { } minPrice && requestedPrice < minPrice)
        {
            return ResalePolicyCheckResult.Deny($"Resale price is below event minimum of {minPrice}");
        }

        if (policy.MaxResalePercent is { } percent)
        {
            var ceiling = Math.Round(originalPrice * percent / 100m, 2, MidpointRounding.AwayFromZero);
            if (requestedPrice > ceiling)
            {
                return ResalePolicyCheckResult.Deny(
                    $"Resale price exceeds {percent}% of original price ({ceiling})");
            }
        }

        return ResalePolicyCheckResult.Allow();
    }
}