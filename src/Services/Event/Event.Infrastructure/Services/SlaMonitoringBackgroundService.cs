using Event.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Background service that continuously monitors SLA compliance
/// </summary>
public class SlaMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SlaMonitoringBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

    public SlaMonitoringBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SlaMonitoringBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSlaChecks(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during SLA monitoring cycle");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("SLA Monitoring Background Service stopped");
    }

    private async Task PerformSlaChecks(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var slaMonitoringService = scope.ServiceProvider.GetRequiredService<ISlaMonitoringService>();

        try
        {
            await slaMonitoringService.CheckSlaComplianceAsync(cancellationToken);
            _logger.LogDebug("SLA compliance check completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform SLA compliance check");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SLA Monitoring Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Background service for cleaning up old performance metrics
/// </summary>
public class MetricsCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<MetricsCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Cleanup every 6 hours
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30); // Keep metrics for 30 days

    public MetricsCleanupBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<MetricsCleanupBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics Cleanup Background Service started");

        // Wait a bit before starting the first cleanup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanup(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during metrics cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("Metrics Cleanup Background Service stopped");
    }

    private async Task PerformCleanup(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var performanceMonitoringService = scope.ServiceProvider.GetRequiredService<IPerformanceMonitoringService>();

        try
        {
            await performanceMonitoringService.CleanupOldMetricsAsync(_retentionPeriod, cancellationToken);
            _logger.LogInformation("Performance metrics cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old performance metrics");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Metrics Cleanup Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Background service for collecting system-level performance metrics
/// </summary>
public class SystemMetricsCollectionService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SystemMetricsCollectionService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1); // Collect every minute

    public SystemMetricsCollectionService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SystemMetricsCollectionService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System Metrics Collection Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectSystemMetrics(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during system metrics collection");
            }

            try
            {
                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("System Metrics Collection Service stopped");
    }

    private async Task CollectSystemMetrics(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var performanceMonitoringService = scope.ServiceProvider.GetRequiredService<IPerformanceMonitoringService>();

        try
        {
            // Collect system-level metrics
            var systemMetric = CreateSystemMetric();
            await performanceMonitoringService.RecordMetricAsync(systemMetric, cancellationToken);

            _logger.LogDebug("System metrics collected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
        }
    }

    private Event.Domain.Models.PerformanceMetrics CreateSystemMetric()
    {
        // Get system performance counters
        var process = System.Diagnostics.Process.GetCurrentProcess();
        
        var cpuUsage = GetCpuUsage();
        var memoryUsage = GC.GetTotalMemory(false);
        var workingSet = process.WorkingSet64;

        return new Event.Domain.Models.PerformanceMetrics
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ServiceName = "Event.API",
            OperationName = "SystemMetrics",
            DurationMs = 0, // System metrics don't have duration
            CpuUsagePercent = cpuUsage,
            MemoryUsageBytes = memoryUsage,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                WorkingSetBytes = workingSet,
                ProcessId = process.Id,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GCGeneration0Collections = GC.CollectionCount(0),
                GCGeneration1Collections = GC.CollectionCount(1),
                GCGeneration2Collections = GC.CollectionCount(2)
            })
        };
    }

    private double? GetCpuUsage()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            
            // Note: Getting accurate CPU usage requires two measurements over time
            // This is a simplified approach that may not be 100% accurate
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            System.Threading.Thread.Sleep(100); // Wait 100ms
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Min(100.0, Math.Max(0.0, cpuUsageTotal * 100));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU usage");
            return null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("System Metrics Collection Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
