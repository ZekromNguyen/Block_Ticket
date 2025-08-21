using Event.Domain.Configuration;
using Event.Domain.Interfaces;
using Event.Domain.Enums;
using Event.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Event.Infrastructure.Services;

/// <summary>
/// Background service that warms up frequently accessed cache data
/// </summary>
public class CacheWarmupService : BackgroundService, ICacheWarmupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly CacheConfiguration _config;

    public CacheWarmupService(
        IServiceProvider serviceProvider,
        IOptions<CacheConfiguration> config,
        ILogger<CacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Warmup.Enabled)
        {
            _logger.LogInformation("Cache warmup is disabled");
            return;
        }

        _logger.LogInformation("Cache warmup service starting with interval: {Interval}", _config.Warmup.Interval);

        try
        {
            // Initial warmup after a short delay
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await WarmupAsync(stoppingToken);

            // Schedule periodic warmup
            await ScheduleWarmupAsync(_config.Warmup.Interval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested during shutdown
            _logger.LogInformation("Cache warmup service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in cache warmup service");
            throw;
        }
    }

    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cache warmup operation");
        var startTime = DateTime.UtcNow;

        try
        {
            var tasks = _config.Warmup.DataTypes.Select(dataType => 
                WarmupDataTypeAsync(dataType, cancellationToken));
            
            await Task.WhenAll(tasks);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Cache warmup completed in {Duration}", duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warmup");
        }
    }

    public async Task WarmupDataTypeAsync(string dataType, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            switch (dataType.ToLowerInvariant())
            {
                case "popular-events":
                    await WarmupPopularEventsAsync(scope, cancellationToken);
                    break;
                    
                case "venue-seatmaps":
                    await WarmupVenueSeatMapsAsync(scope, cancellationToken);
                    break;
                    
                case "active-pricing-rules":
                    await WarmupActivePricingRulesAsync(scope, cancellationToken);
                    break;
                    
                case "event-search-results":
                    await WarmupEventSearchResultsAsync(scope, cancellationToken);
                    break;
                    
                case "availability-data":
                    await WarmupAvailabilityDataAsync(scope, cancellationToken);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown warmup data type: {DataType}", dataType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up data type: {DataType}", dataType);
        }
    }

    public async Task ScheduleWarmupAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(interval);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                await WarmupAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested during shutdown
            _logger.LogInformation("Cache warmup service cancelled");
        }
    }

    private async Task WarmupPopularEventsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up popular events");
        
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Get popular/featured events (published, not expired)
            // TODO: Replace with GetFeaturedEventsAsync when implemented
            var popularEvents = await eventRepository.GetPublishedEventsAsync(cancellationToken);

            var warmupTasks = popularEvents.Select(async eventEntity =>
            {
                try
                {
                    // Cache event details
                    var eventKey = $"event:catalog:{eventEntity.Id}";
                    await cacheService.SetAsync(eventKey, eventEntity, TimeSpan.FromMinutes(30), cancellationToken);
                    
                    // Cache event with full details
                    var fullEvent = await eventRepository.GetWithFullDetailsAsync(eventEntity.Id, cancellationToken);
                    if (fullEvent != null)
                    {
                        var fullEventKey = $"event:full:{eventEntity.Id}";
                        await cacheService.SetAsync(fullEventKey, fullEvent, TimeSpan.FromMinutes(15), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up event {EventId}", eventEntity.Id);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogDebug("Warmed up {Count} popular events", popularEvents.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up popular events");
        }
    }

    private async Task WarmupVenueSeatMapsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up venue seat maps");
        
        var venueRepository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Get venues with upcoming events
            // TODO: Replace with GetVenuesWithUpcomingEventsAsync when implemented
            var activeVenues = await venueRepository.GetAllAsync(cancellationToken);

            var warmupTasks = activeVenues.Select(async venue =>
            {
                try
                {
                    if (venue.SeatMap != null)
                    {
                        var seatMapKey = $"venue:seatmap:{venue.Id}";
                        await cacheService.SetAsync(seatMapKey, venue.SeatMap, TimeSpan.FromHours(1), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up seat map for venue {VenueId}", venue.Id);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogDebug("Warmed up {Count} venue seat maps", activeVenues.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up venue seat maps");
        }
    }

    private async Task WarmupActivePricingRulesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up active pricing rules");
        
        var pricingRuleRepository = scope.ServiceProvider.GetRequiredService<IPricingRuleRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Get events with upcoming dates
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            // TODO: Replace with GetUpcomingEventsAsync when implemented
            var upcomingEvents = await eventRepository.GetPublishedEventsAsync(cancellationToken);

            var warmupTasks = upcomingEvents.Select(async eventEntity =>
            {
                try
                {
                    var pricingRules = await pricingRuleRepository.GetActiveRulesForEventAsync(
                        eventEntity.Id, DateTime.UtcNow, cancellationToken);
                    
                    if (pricingRules.Any())
                    {
                        var rulesKey = $"event:pricing-rules:{eventEntity.Id}";
                        await cacheService.SetAsync(rulesKey, pricingRules, TimeSpan.FromMinutes(10), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up pricing rules for event {EventId}", eventEntity.Id);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogDebug("Warmed up pricing rules for {Count} events", upcomingEvents.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up active pricing rules");
        }
    }

    private async Task WarmupEventSearchResultsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up event search results");
        
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Common search scenarios to pre-cache
            var searchScenarios = new[]
            {
                new { Category = "music", Location = "popular", PageSize = 20 },
                new { Category = "sports", Location = "popular", PageSize = 20 },
                new { Category = "theater", Location = "popular", PageSize = 20 },
                new { Category = (string?)null, Location = "all", PageSize = 50 } // All events
            };

            var warmupTasks = searchScenarios.Select(async scenario =>
            {
                try
                {
                    // Build search parameters
                    var searchKey = $"search:events:{scenario.Category ?? "all"}:{scenario.Location}:{scenario.PageSize}";
                    
                    // Get search results
                    var categories = scenario.Category != null ? new List<string> { scenario.Category } : null;
                    var searchResults = await eventRepository.SearchEventsAsync(
                        searchTerm: null,
                        startDate: null,
                        endDate: null,
                        venueId: null,
                        categories: categories,
                        minPrice: null,
                        maxPrice: null,
                        hasAvailability: null,
                        skip: 0,
                        take: scenario.PageSize,
                        cancellationToken: cancellationToken);
                    
                    // Wrap tuple in an object to make it cacheable
                    var cacheableResult = new { 
                        Events = searchResults.Events, 
                        TotalCount = searchResults.TotalCount 
                    };
                    await cacheService.SetAsync(searchKey, cacheableResult, TimeSpan.FromMinutes(5), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up search results for scenario {@Scenario}", scenario);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogDebug("Warmed up {Count} search result scenarios", searchScenarios.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up event search results");
        }
    }

    private async Task WarmupAvailabilityDataAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up availability data");
        
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Get events starting soon (next 7 days)
            // TODO: Replace with GetUpcomingEventsAsync when implemented
            var upcomingEvents = await eventRepository.GetPublishedEventsAsync(cancellationToken);

            var warmupTasks = upcomingEvents.Where(e => 
                e.EventDate >= DateTime.UtcNow && 
                e.EventDate <= DateTime.UtcNow.AddDays(7))
                .Select(async eventEntity =>
            {
                try
                {
                    // Calculate and cache availability
                    var availabilityKey = $"event:availability:{eventEntity.Id}";
                    
                    // This would call a service to calculate current availability
                    var availability = new
                    {
                        EventId = eventEntity.Id,
                        TotalCapacity = eventEntity.TotalCapacity,
                        AvailableTickets = eventEntity.TotalCapacity, // Simplified
                        LastUpdated = DateTime.UtcNow
                    };
                    
                    await cacheService.SetAsync(availabilityKey, availability, TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm up availability for event {EventId}", eventEntity.Id);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogDebug("Warmed up availability for {Count} upcoming events", warmupTasks.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up availability data");
        }
    }
}
