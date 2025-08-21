using Event.Domain.Configuration;
using Event.Application.Interfaces.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Event.Infrastructure.Services;

/// <summary>
/// Service that handles automatic cache invalidation based on domain events
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService, INotificationHandler<INotification>
{
    private readonly IAdvancedCacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly CacheConfiguration _config;
    private readonly ConcurrentDictionary<string, InvalidationStrategy> _strategies;

    public CacheInvalidationService(
        IAdvancedCacheService cacheService,
        IOptions<CacheConfiguration> config,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _config = config.Value;
        _logger = logger;
        _strategies = new ConcurrentDictionary<string, InvalidationStrategy>();

        // Load configured strategies
        LoadConfiguredStrategies();
    }

    public async Task InvalidateOnEventAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        if (!_config.Invalidation.Enabled)
        {
            return;
        }

        _logger.LogDebug("Processing cache invalidation for event type: {EventType}", eventType);

        var applicableStrategies = _strategies.Values
            .Where(s => s.TriggerEvents.Contains(eventType, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (!applicableStrategies.Any())
        {
            _logger.LogDebug("No invalidation strategies found for event type: {EventType}", eventType);
            return;
        }

        var invalidationTasks = applicableStrategies.Select(strategy => 
            ProcessInvalidationStrategyAsync(strategy, eventType, eventData, cancellationToken));

        await Task.WhenAll(invalidationTasks);
    }

    public Task RegisterStrategyAsync(InvalidationStrategy strategy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(strategy.DataType))
        {
            throw new ArgumentException("Strategy must have a data type", nameof(strategy));
        }

        _strategies.AddOrUpdate(strategy.DataType, strategy, (_, _) => strategy);
        _logger.LogInformation("Registered cache invalidation strategy for data type: {DataType}", strategy.DataType);
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<InvalidationStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<InvalidationStrategy>>(_strategies.Values);
    }

    public async Task Handle(INotification notification, CancellationToken cancellationToken)
    {
        var eventType = notification.GetType().Name;
        await InvalidateOnEventAsync(eventType, notification, cancellationToken);
    }

    private async Task ProcessInvalidationStrategyAsync(
        InvalidationStrategy strategy,
        string eventType,
        object eventData,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Applying invalidation strategy {DataType} for event {EventType}", 
                strategy.DataType, eventType);

            // Apply delay if configured
            if (strategy.Delay > TimeSpan.Zero)
            {
                await Task.Delay(strategy.Delay, cancellationToken);
            }

            // Process each invalidation pattern
            var invalidationTasks = strategy.InvalidationPatterns.Select(pattern =>
                ProcessInvalidationPatternAsync(pattern, eventData, cancellationToken));

            await Task.WhenAll(invalidationTasks);

            _logger.LogDebug("Completed invalidation strategy {DataType} for event {EventType}", 
                strategy.DataType, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invalidation strategy {DataType} for event {EventType}", 
                strategy.DataType, eventType);
        }
    }

    private async Task ProcessInvalidationPatternAsync(
        string pattern,
        object eventData,
        CancellationToken cancellationToken)
    {
        try
        {
            // Replace placeholders in the pattern with actual values from event data
            var resolvedPattern = ResolvePatternPlaceholders(pattern, eventData);
            
            _logger.LogDebug("Invalidating cache pattern: {Pattern}", resolvedPattern);

            // Use pattern-based invalidation
            await _cacheService.RemoveByPatternAsync(resolvedPattern, cancellationToken);

            // Also publish invalidation message for distributed scenarios
            await _cacheService.PublishInvalidationAsync(resolvedPattern, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invalidation pattern: {Pattern}", pattern);
        }
    }

    private string ResolvePatternPlaceholders(string pattern, object eventData)
    {
        var resolvedPattern = pattern;

        // Extract properties from event data using reflection
        var properties = eventData.GetType().GetProperties();

        foreach (var property in properties)
        {
            var placeholder = $"{{{property.Name}}}";
            if (resolvedPattern.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                var value = property.GetValue(eventData)?.ToString() ?? "";
                resolvedPattern = resolvedPattern.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Handle common nested properties
        if (eventData is IHasEventId hasEventId)
        {
            resolvedPattern = resolvedPattern.Replace("{EventId}", hasEventId.EventId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        if (eventData is IHasVenueId hasVenueId)
        {
            resolvedPattern = resolvedPattern.Replace("{VenueId}", hasVenueId.VenueId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        if (eventData is IHasOrganizationId hasOrgId)
        {
            resolvedPattern = resolvedPattern.Replace("{OrganizationId}", hasOrgId.OrganizationId.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return resolvedPattern;
    }

    private void LoadConfiguredStrategies()
    {
        foreach (var strategy in _config.Invalidation.Strategies)
        {
            _strategies.TryAdd(strategy.DataType, strategy);
        }

        // Add default strategies if none configured
        if (!_strategies.Any())
        {
            LoadDefaultStrategies();
        }

        _logger.LogInformation("Loaded {Count} cache invalidation strategies", _strategies.Count);
    }

    private void LoadDefaultStrategies()
    {
        // Event-related invalidations
        var eventStrategy = new InvalidationStrategy
        {
            DataType = "events",
            TriggerEvents = new List<string>
            {
                "EventCreated",
                "EventUpdated",
                "EventDeleted",
                "EventPublished",
                "EventCanceled"
            },
            InvalidationPatterns = new List<string>
            {
                "event:catalog:{EventId}*",
                "event:full:{EventId}*",
                "event:availability:{EventId}*",
                "search:events:*",
                "event:pricing:{EventId}*"
            }
        };

        // Venue-related invalidations
        var venueStrategy = new InvalidationStrategy
        {
            DataType = "venues",
            TriggerEvents = new List<string>
            {
                "VenueUpdated",
                "VenueDeleted",
                "SeatMapUpdated"
            },
            InvalidationPatterns = new List<string>
            {
                "venue:seatmap:{VenueId}*",
                "venue:details:{VenueId}*",
                "event:*" // Events might be affected by venue changes
            }
        };

        // Pricing-related invalidations
        var pricingStrategy = new InvalidationStrategy
        {
            DataType = "pricing",
            TriggerEvents = new List<string>
            {
                "PricingRuleCreated",
                "PricingRuleUpdated",
                "PricingRuleDeleted",
                "PricingRuleActivated",
                "PricingRuleDeactivated"
            },
            InvalidationPatterns = new List<string>
            {
                "event:pricing:{EventId}*",
                "event:pricing-rules:{EventId}*"
            }
        };

        // Reservation-related invalidations
        var reservationStrategy = new InvalidationStrategy
        {
            DataType = "reservations",
            TriggerEvents = new List<string>
            {
                "ReservationCreated",
                "ReservationConfirmed",
                "ReservationCanceled",
                "ReservationExpired",
                "TicketPurchased"
            },
            InvalidationPatterns = new List<string>
            {
                "event:availability:{EventId}*",
                "event:seats:{EventId}*"
            },
            Delay = TimeSpan.FromSeconds(5) // Small delay for eventual consistency
        };

        // Register default strategies
        _strategies.TryAdd(eventStrategy.DataType, eventStrategy);
        _strategies.TryAdd(venueStrategy.DataType, venueStrategy);
        _strategies.TryAdd(pricingStrategy.DataType, pricingStrategy);
        _strategies.TryAdd(reservationStrategy.DataType, reservationStrategy);
    }
}

/// <summary>
/// Interface for events that contain an event ID
/// </summary>
public interface IHasEventId
{
    Guid EventId { get; }
}

/// <summary>
/// Interface for events that contain a venue ID
/// </summary>
public interface IHasVenueId
{
    Guid VenueId { get; }
}

/// <summary>
/// Interface for events that contain an organization ID
/// </summary>
public interface IHasOrganizationId
{
    Guid OrganizationId { get; }
}
