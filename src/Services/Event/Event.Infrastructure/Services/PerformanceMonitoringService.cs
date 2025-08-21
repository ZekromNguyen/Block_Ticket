using Event.Domain.Models;
using Event.Application.Interfaces.Infrastructure;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of performance monitoring service
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    
    // In-memory buffer for high-frequency metrics
    private readonly ConcurrentQueue<PerformanceMetrics> _metricsBuffer = new();
    private readonly ConcurrentDictionary<string, RealTimeMetrics> _realTimeMetrics = new();
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _flushSemaphore = new(1, 1);

    private const int BufferFlushSize = 100;
    private const int FlushIntervalMs = 5000; // 5 seconds

    public PerformanceMonitoringService(
        ICacheService cacheService,
        ILogger<PerformanceMonitoringService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;

        // Set up periodic flushing of metrics buffer
        _flushTimer = new Timer(FlushMetricsBuffer, null, FlushIntervalMs, FlushIntervalMs);
    }

    public async Task RecordMetricAsync(PerformanceMetrics metric, CancellationToken cancellationToken = default)
    {
        try
        {
            // Add to buffer for batch processing
            _metricsBuffer.Enqueue(metric);

            // Update real-time metrics
            UpdateRealTimeMetrics(metric);

            // Cache recent metric for quick access
            var cacheKey = $"metric:{metric.ServiceName}:{metric.OperationName}:{metric.Timestamp:yyyy-MM-dd-HH-mm}";
            await _cacheService.SetAsync(cacheKey, metric, TimeSpan.FromMinutes(15), cancellationToken);

            // Flush buffer if it's getting full
            if (_metricsBuffer.Count >= BufferFlushSize)
            {
                _ = Task.Run(() => FlushMetricsBuffer(null));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metric for {ServiceName}.{OperationName}", 
                metric.ServiceName, metric.OperationName);
        }
    }

    public async Task RecordMetricsBatchAsync(IEnumerable<PerformanceMetrics> metrics, CancellationToken cancellationToken = default)
    {
        try
        {
            var metricsList = metrics.ToList();
            
            // Add to buffer
            foreach (var metric in metricsList)
            {
                _metricsBuffer.Enqueue(metric);
                UpdateRealTimeMetrics(metric);
            }

            // Cache metrics
            var cacheTasks = metricsList.Select(async metric =>
            {
                var cacheKey = $"metric:{metric.ServiceName}:{metric.OperationName}:{metric.Timestamp:yyyy-MM-dd-HH-mm}";
                await _cacheService.SetAsync(cacheKey, metric, TimeSpan.FromMinutes(15), cancellationToken);
            });

            await Task.WhenAll(cacheTasks);

            _logger.LogDebug("Recorded batch of {Count} performance metrics", metricsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metrics batch");
        }
    }

    public async Task<PerformanceSummary> GetPerformanceSummaryAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"summary:{serviceName}:{operationName}:{startTime:yyyy-MM-dd-HH}:{endTime:yyyy-MM-dd-HH}";
            
            // Try to get from cache first
            var cachedSummary = await _cacheService.GetAsync<PerformanceSummary>(cacheKey, cancellationToken);
            if (cachedSummary != null)
            {
                return cachedSummary;
            }

            // Calculate from stored metrics
            var metrics = await GetMetricsAsync(serviceName, operationName, startTime, endTime, int.MaxValue, cancellationToken);
            var metricsList = metrics.ToList();

            if (!metricsList.Any())
            {
                return new PerformanceSummary
                {
                    ServiceName = serviceName,
                    OperationName = operationName,
                    PeriodStart = startTime ?? DateTime.UtcNow.AddHours(-1),
                    PeriodEnd = endTime ?? DateTime.UtcNow
                };
            }

            var summary = CalculatePerformanceSummary(metricsList, serviceName, operationName, startTime, endTime);

            // Cache the summary for 5 minutes
            await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(5), cancellationToken);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance summary for {ServiceName}.{OperationName}", serviceName, operationName);
            throw;
        }
    }

    public async Task<RealTimeMetrics> GetRealTimeMetricsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_realTimeMetrics.TryGetValue(serviceName, out var metrics))
            {
                return metrics;
            }

            // Return default metrics if none exist
            return new RealTimeMetrics
            {
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time metrics for {ServiceName}", serviceName);
            throw;
        }
    }

    public async Task<IEnumerable<PerformanceMetrics>> GetMetricsAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddHours(-1);
            var end = endTime ?? DateTime.UtcNow;

            // Get metrics from cache (for recent data)
            var cachePattern = $"metric:{serviceName}:{operationName ?? "*"}:*";
            // Note: GetKeysAsync is not available in ICacheService, this would need to be implemented differently
            // For now, return empty list as fallback
            var cacheKeys = new List<string>();
            
            var cachedMetrics = new List<PerformanceMetrics>();
            foreach (var key in cacheKeys.Take(limit))
            {
                var metric = await _cacheService.GetAsync<PerformanceMetrics>(key, cancellationToken);
                if (metric != null && metric.Timestamp >= start && metric.Timestamp <= end)
                {
                    cachedMetrics.Add(metric);
                }
            }

            return cachedMetrics.OrderByDescending(m => m.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics for {ServiceName}.{OperationName}", serviceName, operationName);
            throw;
        }
    }

    public async Task<Dictionary<int, double>> GetResponseTimePercentilesAsync(
        string serviceName,
        string? operationName = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int[]? percentiles = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            percentiles ??= new[] { 50, 90, 95, 99 };
            
            var metrics = await GetMetricsAsync(serviceName, operationName, startTime, endTime, int.MaxValue, cancellationToken);
            var responseTimes = metrics.Select(m => m.DurationMs).OrderBy(d => d).ToArray();

            if (!responseTimes.Any())
            {
                return percentiles.ToDictionary(p => p, p => 0.0);
            }

            var result = new Dictionary<int, double>();
            foreach (var percentile in percentiles)
            {
                var index = (int)Math.Ceiling(responseTimes.Length * percentile / 100.0) - 1;
                index = Math.Max(0, Math.Min(index, responseTimes.Length - 1));
                result[percentile] = responseTimes[index];
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate response time percentiles for {ServiceName}.{OperationName}", serviceName, operationName);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetErrorMetricsAsync(
        string serviceName,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await GetMetricsAsync(serviceName, null, startTime, endTime, int.MaxValue, cancellationToken);
            
            return metrics
                .Where(m => m.IsFailure())
                .GroupBy(m => m.ExceptionType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error metrics for {ServiceName}", serviceName);
            throw;
        }
    }

    public async Task CleanupOldMetricsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            var pattern = "metric:*";
            // Note: GetKeysAsync is not available in ICacheService, cleanup would need different implementation
            // For now, skip cleanup as fallback
            var keys = new List<string>();
            
            var keysToDelete = new List<string>();
            foreach (var key in keys)
            {
                var metric = await _cacheService.GetAsync<PerformanceMetrics>(key, cancellationToken);
                if (metric != null && metric.Timestamp < cutoffTime)
                {
                    keysToDelete.Add(key);
                }
            }

            foreach (var key in keysToDelete)
            {
                await _cacheService.RemoveAsync(key, cancellationToken);
            }

            _logger.LogInformation("Cleaned up {Count} old performance metrics", keysToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old metrics");
        }
    }

    private void UpdateRealTimeMetrics(PerformanceMetrics metric)
    {
        var serviceName = metric.ServiceName;
        var now = DateTime.UtcNow;

        _realTimeMetrics.AddOrUpdate(serviceName, 
            new RealTimeMetrics 
            { 
                Timestamp = now,
                CurrentAvgResponseTimeMs = metric.DurationMs,
                CurrentErrorRatePercent = metric.IsFailure() ? 100.0 : 0.0,
                CurrentRps = 1.0
            },
            (key, existing) =>
            {
                // Simple moving average for real-time metrics
                var alpha = 0.1; // Smoothing factor
                existing.CurrentAvgResponseTimeMs = (1 - alpha) * existing.CurrentAvgResponseTimeMs + alpha * metric.DurationMs;
                
                // Update error rate
                if (metric.IsFailure())
                {
                    existing.CurrentErrorRatePercent = (1 - alpha) * existing.CurrentErrorRatePercent + alpha * 100.0;
                }
                else
                {
                    existing.CurrentErrorRatePercent = (1 - alpha) * existing.CurrentErrorRatePercent;
                }

                existing.Timestamp = now;
                return existing;
            });
    }

    private PerformanceSummary CalculatePerformanceSummary(
        IList<PerformanceMetrics> metrics,
        string serviceName,
        string? operationName,
        DateTime? startTime,
        DateTime? endTime)
    {
        var start = startTime ?? metrics.Min(m => m.Timestamp);
        var end = endTime ?? metrics.Max(m => m.Timestamp);
        var duration = (end - start).TotalSeconds;

        var successfulRequests = metrics.Count(m => m.IsSuccess());
        var failedRequests = metrics.Count(m => m.IsFailure());
        var totalRequests = metrics.Count;

        var responseTimes = metrics.Select(m => m.DurationMs).OrderBy(d => d).ToArray();

        return new PerformanceSummary
        {
            ServiceName = serviceName,
            OperationName = operationName,
            PeriodStart = start,
            PeriodEnd = end,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AverageResponseTimeMs = responseTimes.Any() ? responseTimes.Average() : 0,
            P50ResponseTimeMs = CalculatePercentile(responseTimes, 50),
            P95ResponseTimeMs = CalculatePercentile(responseTimes, 95),
            P99ResponseTimeMs = CalculatePercentile(responseTimes, 99),
            MaxResponseTimeMs = responseTimes.Any() ? responseTimes.Max() : 0,
            MinResponseTimeMs = responseTimes.Any() ? responseTimes.Min() : 0,
            RequestsPerSecond = duration > 0 ? totalRequests / duration : 0,
            ErrorRatePercent = totalRequests > 0 ? (failedRequests * 100.0 / totalRequests) : 0,
            AvailabilityPercent = totalRequests > 0 ? (successfulRequests * 100.0 / totalRequests) : 100,
            AverageCpuUsagePercent = metrics.Where(m => m.CpuUsagePercent.HasValue).Select(m => m.CpuUsagePercent!.Value).DefaultIfEmpty().Average(),
            AverageMemoryUsageBytes = (long?)metrics.Where(m => m.MemoryUsageBytes.HasValue).Select(m => m.MemoryUsageBytes!.Value).DefaultIfEmpty().Average(),
            CacheHitRatePercent = CalculateCacheHitRate(metrics),
            AverageDatabaseDurationMs = metrics.Where(m => m.DatabaseDurationMs.HasValue).Select(m => m.DatabaseDurationMs!.Value).DefaultIfEmpty().Average()
        };
    }

    private double CalculatePercentile(double[] sortedValues, int percentile)
    {
        if (!sortedValues.Any()) return 0;
        
        var index = (int)Math.Ceiling(sortedValues.Length * percentile / 100.0) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Length - 1));
        return sortedValues[index];
    }

    private double? CalculateCacheHitRate(IEnumerable<PerformanceMetrics> metrics)
    {
        var cacheMetrics = metrics.Where(m => m.CacheHit.HasValue).ToList();
        if (!cacheMetrics.Any()) return null;

        var hits = cacheMetrics.Count(m => m.CacheHit!.Value);
        return hits * 100.0 / cacheMetrics.Count;
    }

    private async void FlushMetricsBuffer(object? state)
    {
        if (!await _flushSemaphore.WaitAsync(100))
        {
            return; // Skip if another flush is in progress
        }

        try
        {
            var metricsToFlush = new List<PerformanceMetrics>();
            
            // Drain the buffer
            while (_metricsBuffer.TryDequeue(out var metric) && metricsToFlush.Count < BufferFlushSize * 2)
            {
                metricsToFlush.Add(metric);
            }

            if (metricsToFlush.Any())
            {
                // Here you would typically persist to a database
                // For now, we'll just log the metrics count
                _logger.LogDebug("Flushed {Count} performance metrics from buffer", metricsToFlush.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush metrics buffer");
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _flushSemaphore?.Dispose();
    }
}
