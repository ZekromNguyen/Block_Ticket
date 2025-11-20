using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Event.Infrastructure.Services;

public class InventorySnapshotService : IInventorySnapshotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<InventorySnapshotService> _logger;

    public InventorySnapshotService(IUnitOfWork unitOfWork, ICacheService cacheService, ILogger<InventorySnapshotService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task<Dictionary<Guid, int>> GetAvailabilitySnapshotAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<InventorySnapshot?> GetInventorySnapshotAsync(Guid eventId, ConsistencyMode mode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"inventory-snapshot:{eventId}";

        if (mode == ConsistencyMode.Stale)
        {
            var cachedSnapshot = await _cacheService.GetAsync<InventorySnapshot>(cacheKey, cancellationToken);
            if (cachedSnapshot != null)
            {
                _logger.LogInformation("Returning stale inventory snapshot for event {EventId} from cache.", eventId);
                return cachedSnapshot;
            }
        }

        _logger.LogInformation("Generating new inventory snapshot for event {EventId}.", eventId);

        var eventAggregate = await _unitOfWork.Events.GetWithFullDetailsAsync(eventId, cancellationToken);
        if (eventAggregate == null)
        {
            _logger.LogWarning("Event with ID {EventId} not found for inventory snapshot.", eventId);
            return null;
        }

        var venue = await _unitOfWork.Venues.GetWithSeatMapAsync(eventAggregate.VenueId, cancellationToken);

        var snapshot = new InventorySnapshot
        {
            EventId = eventId,
            GeneratedAtUtc = DateTime.UtcNow,
            SeatStatusSummary = new SeatStatusSummary
            {
                TotalSeats = venue?.TotalCapacity ?? 0,
                Available = venue?.Seats.Count(s => s.Status == Domain.Enums.SeatStatus.Available) ?? 0,
                Held = venue?.Seats.Count(s => s.Status == Domain.Enums.SeatStatus.Held) ?? 0,
                Sold = venue?.Seats.Count(s => s.Status == Domain.Enums.SeatStatus.Sold) ?? 0,
                Blocked = venue?.Seats.Count(s => s.Status == Domain.Enums.SeatStatus.Blocked) ?? 0
            },
            TicketTypeAvailability = eventAggregate.TicketTypes.Select(tt => new TicketTypeAvailability
            {
                TicketTypeId = tt.Id,
                Name = tt.Name,
                Total = tt.Capacity.Total,
                Available = tt.Capacity.Available,
                Held = tt.Capacity.Held,
                Sold = tt.Capacity.Sold
            }).ToList()
        };

        snapshot.ETag = GenerateETag(snapshot);

        await _cacheService.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(5), cancellationToken);
        _logger.LogInformation("Successfully generated and cached new inventory snapshot for event {EventId} with ETag {ETag}.", eventId, snapshot.ETag);

        return snapshot;
    }

    private string GenerateETag(InventorySnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public Task InvalidateInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

