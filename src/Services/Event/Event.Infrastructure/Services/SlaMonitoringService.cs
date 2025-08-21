using Event.Domain.Models;
using Event.Application.Interfaces.Infrastructure;
using Event.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of SLA monitoring service
/// </summary>
public class SlaMonitoringService : ISlaMonitoringService
{
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SlaMonitoringService> _logger;

    // In-memory storage for SLA definitions (in production, this would be in a database)
    private readonly List<SlaDefinition> _slaDefinitions = new();
    private readonly List<SlaViolation> _slaViolations = new();
    private readonly object _lock = new();

    public SlaMonitoringService(
        IPerformanceMonitoringService performanceMonitoringService,
        ICacheService cacheService,
        ILogger<SlaMonitoringService> logger)
    {
        _performanceMonitoringService = performanceMonitoringService;
        _cacheService = cacheService;
        _logger = logger;

        // Initialize with default SLAs
        InitializeDefaultSlas();
    }

    public async Task<SlaDefinition> CreateSlaDefinitionAsync(SlaDefinition slaDefinition, CancellationToken cancellationToken = default)
    {
        try
        {
            slaDefinition.Id = Guid.NewGuid();
            slaDefinition.CreatedAt = DateTime.UtcNow;
            slaDefinition.UpdatedAt = DateTime.UtcNow;

            lock (_lock)
            {
                _slaDefinitions.Add(slaDefinition);
            }

            // Cache the SLA definition
            var cacheKey = $"sla:definition:{slaDefinition.Id}";
            await _cacheService.SetAsync(cacheKey, slaDefinition, TimeSpan.FromHours(24), cancellationToken);

            _logger.LogInformation("Created SLA definition: {SlaName} for service {ServiceName}", 
                slaDefinition.Name, slaDefinition.ServiceName);

            return slaDefinition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SLA definition for {ServiceName}", slaDefinition.ServiceName);
            throw;
        }
    }

    public async Task UpdateSlaDefinitionAsync(SlaDefinition slaDefinition, CancellationToken cancellationToken = default)
    {
        try
        {
            slaDefinition.UpdatedAt = DateTime.UtcNow;

            lock (_lock)
            {
                var existingIndex = _slaDefinitions.FindIndex(s => s.Id == slaDefinition.Id);
                if (existingIndex >= 0)
                {
                    _slaDefinitions[existingIndex] = slaDefinition;
                }
                else
                {
                    throw new InvalidOperationException($"SLA definition {slaDefinition.Id} not found");
                }
            }

            // Update cache
            var cacheKey = $"sla:definition:{slaDefinition.Id}";
            await _cacheService.SetAsync(cacheKey, slaDefinition, TimeSpan.FromHours(24), cancellationToken);

            _logger.LogInformation("Updated SLA definition: {SlaName}", slaDefinition.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SLA definition {SlaId}", slaDefinition.Id);
            throw;
        }
    }

    public async Task<IEnumerable<SlaDefinition>> GetActiveSlaDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cacheKey = "sla:definitions:active";
            var cachedDefinitions = await _cacheService.GetAsync<List<SlaDefinition>>(cacheKey, cancellationToken);
            if (cachedDefinitions != null)
            {
                return cachedDefinitions;
            }

            List<SlaDefinition> activeDefinitions;
            lock (_lock)
            {
                activeDefinitions = _slaDefinitions.Where(s => s.IsActive).ToList();
            }

            // Cache for 10 minutes
            await _cacheService.SetAsync(cacheKey, activeDefinitions, TimeSpan.FromMinutes(10), cancellationToken);

            return activeDefinitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active SLA definitions");
            throw;
        }
    }

    public async Task CheckSlaComplianceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSlas = await GetActiveSlaDefinitionsAsync(cancellationToken);
            var checkTasks = activeSlas.Select(sla => CheckSlaComplianceAsync(sla, cancellationToken));
            
            await Task.WhenAll(checkTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SLA compliance");
        }
    }

    public async Task<IEnumerable<SlaViolation>> GetSlaViolationsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? serviceName = null,
        bool includeResolved = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddDays(-7);
            var end = endTime ?? DateTime.UtcNow;

            lock (_lock)
            {
                return _slaViolations
                    .Where(v => v.ViolationTime >= start && v.ViolationTime <= end)
                    .Where(v => serviceName == null || GetSlaDefinition(v.SlaDefinitionId)?.ServiceName == serviceName)
                    .Where(v => includeResolved || v.ResolvedTime == null)
                    .OrderByDescending(v => v.ViolationTime)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SLA violations");
            throw;
        }
    }

    public async Task<SlaComplianceReport> GetSlaComplianceReportAsync(
        Guid slaDefinitionId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var slaDefinition = GetSlaDefinition(slaDefinitionId);
            if (slaDefinition == null)
            {
                throw new InvalidOperationException($"SLA definition {slaDefinitionId} not found");
            }

            // Get performance metrics for the period
            var summary = await _performanceMonitoringService.GetPerformanceSummaryAsync(
                slaDefinition.ServiceName,
                slaDefinition.OperationName,
                startTime,
                endTime,
                cancellationToken);

            // Get violations for the period
            var violations = await GetSlaViolationsAsync(startTime, endTime, slaDefinition.ServiceName, true, cancellationToken);
            var relevantViolations = violations.Where(v => v.SlaDefinitionId == slaDefinitionId).ToList();

            // Calculate compliance metrics
            var totalDowntime = relevantViolations
                .Where(v => v.ResolvedTime.HasValue)
                .Sum(v => (v.ResolvedTime!.Value - v.ViolationTime).TotalMilliseconds);

            var periodDuration = (endTime - startTime).TotalMilliseconds;
            var uptimePercentage = periodDuration > 0 ? ((periodDuration - totalDowntime) / periodDuration) * 100.0 : 100.0;

            var isCompliant = CheckCompliance(slaDefinition, summary, uptimePercentage);

            return new SlaComplianceReport
            {
                SlaDefinitionId = slaDefinitionId,
                SlaName = slaDefinition.Name,
                ReportPeriodStart = startTime,
                ReportPeriodEnd = endTime,
                IsCompliant = isCompliant,
                CompliancePercentage = CalculateCompliancePercentage(slaDefinition, summary, uptimePercentage),
                TotalDowntime = TimeSpan.FromMilliseconds(totalDowntime),
                ViolationCount = relevantViolations.Count,
                ActualAvailability = summary.AvailabilityPercent,
                ActualResponseTimeP95 = summary.P95ResponseTimeMs,
                ActualErrorRate = summary.ErrorRatePercent,
                ActualThroughput = summary.RequestsPerSecond,
                Violations = relevantViolations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SLA compliance report for {SlaDefinitionId}", slaDefinitionId);
            throw;
        }
    }

    public async Task ResolveSlaViolationAsync(Guid violationId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lock)
            {
                var violation = _slaViolations.FirstOrDefault(v => v.Id == violationId);
                if (violation != null && violation.ResolvedTime == null)
                {
                    violation.ResolvedTime = DateTime.UtcNow;
                    _logger.LogInformation("Resolved SLA violation {ViolationId}", violationId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve SLA violation {ViolationId}", violationId);
            throw;
        }
    }

    private async Task CheckSlaComplianceAsync(SlaDefinition sla, CancellationToken cancellationToken)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-sla.TimeWindowMinutes);

            // Get performance metrics for the time window
            var summary = await _performanceMonitoringService.GetPerformanceSummaryAsync(
                sla.ServiceName,
                sla.OperationName,
                startTime,
                endTime,
                cancellationToken);

            // Check for violations
            await CheckResponseTimeViolation(sla, summary);
            await CheckAvailabilityViolation(sla, summary);
            await CheckErrorRateViolation(sla, summary);
            await CheckThroughputViolation(sla, summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check compliance for SLA {SlaName}", sla.Name);
        }
    }

    private async Task CheckResponseTimeViolation(SlaDefinition sla, PerformanceSummary summary)
    {
        var actualResponseTime = sla.ResponseTimePercentile switch
        {
            50 => summary.P50ResponseTimeMs,
            95 => summary.P95ResponseTimeMs,
            99 => summary.P99ResponseTimeMs,
            _ => summary.AverageResponseTimeMs
        };

        if (actualResponseTime > sla.TargetResponseTimeMs)
        {
            await CreateSlaViolationAsync(sla, ViolationTypes.ResponseTime, sla.TargetResponseTimeMs, actualResponseTime,
                $"P{sla.ResponseTimePercentile} response time {actualResponseTime:F1}ms exceeds target {sla.TargetResponseTimeMs:F1}ms");
        }
    }

    private async Task CheckAvailabilityViolation(SlaDefinition sla, PerformanceSummary summary)
    {
        if (summary.AvailabilityPercent < sla.TargetAvailabilityPercent)
        {
            await CreateSlaViolationAsync(sla, ViolationTypes.Availability, sla.TargetAvailabilityPercent, summary.AvailabilityPercent,
                $"Availability {summary.AvailabilityPercent:F2}% below target {sla.TargetAvailabilityPercent:F2}%");
        }
    }

    private async Task CheckErrorRateViolation(SlaDefinition sla, PerformanceSummary summary)
    {
        if (summary.ErrorRatePercent > sla.TargetErrorRatePercent)
        {
            await CreateSlaViolationAsync(sla, ViolationTypes.ErrorRate, sla.TargetErrorRatePercent, summary.ErrorRatePercent,
                $"Error rate {summary.ErrorRatePercent:F2}% exceeds target {sla.TargetErrorRatePercent:F2}%");
        }
    }

    private async Task CheckThroughputViolation(SlaDefinition sla, PerformanceSummary summary)
    {
        if (sla.TargetThroughputRps.HasValue && summary.RequestsPerSecond < sla.TargetThroughputRps.Value)
        {
            await CreateSlaViolationAsync(sla, ViolationTypes.Throughput, sla.TargetThroughputRps.Value, summary.RequestsPerSecond,
                $"Throughput {summary.RequestsPerSecond:F1} RPS below target {sla.TargetThroughputRps.Value:F1} RPS");
        }
    }

    private async Task CreateSlaViolationAsync(SlaDefinition sla, string violationType, double expectedValue, double actualValue, string details)
    {
        // Check if there's already an active violation of this type
        lock (_lock)
        {
            var existingViolation = _slaViolations.FirstOrDefault(v => 
                v.SlaDefinitionId == sla.Id && 
                v.ViolationType == violationType && 
                v.ResolvedTime == null &&
                v.ViolationTime > DateTime.UtcNow.AddMinutes(-sla.TimeWindowMinutes));

            if (existingViolation != null)
            {
                // Update existing violation
                existingViolation.ActualValue = actualValue;
                existingViolation.Details = details;
                return;
            }

            // Create new violation
            var violation = new SlaViolation
            {
                Id = Guid.NewGuid(),
                SlaDefinitionId = sla.Id,
                ViolationType = violationType,
                ExpectedValue = expectedValue,
                ActualValue = actualValue,
                Details = details,
                Severity = DetermineSeverity(expectedValue, actualValue, sla.AlertThresholdPercent),
                ViolationTime = DateTime.UtcNow,
                SlaDefinition = sla
            };

            _slaViolations.Add(violation);

            _logger.LogWarning("SLA violation detected: {SlaName} - {ViolationType} - Expected: {Expected}, Actual: {Actual}",
                sla.Name, violationType, expectedValue, actualValue);
        }
    }

    private string DetermineSeverity(double expectedValue, double actualValue, double alertThreshold)
    {
        var deviationPercent = Math.Abs((actualValue - expectedValue) / expectedValue) * 100.0;

        if (deviationPercent >= 50)
            return SeverityLevels.Critical;
        if (deviationPercent >= 25)
            return SeverityLevels.High;
        if (deviationPercent >= alertThreshold)
            return SeverityLevels.Medium;
        return SeverityLevels.Low;
    }

    private bool CheckCompliance(SlaDefinition sla, PerformanceSummary summary, double uptimePercentage)
    {
        var responseTimeCompliant = sla.ResponseTimePercentile switch
        {
            50 => summary.P50ResponseTimeMs <= sla.TargetResponseTimeMs,
            95 => summary.P95ResponseTimeMs <= sla.TargetResponseTimeMs,
            99 => summary.P99ResponseTimeMs <= sla.TargetResponseTimeMs,
            _ => summary.AverageResponseTimeMs <= sla.TargetResponseTimeMs
        };

        var availabilityCompliant = summary.AvailabilityPercent >= sla.TargetAvailabilityPercent;
        var errorRateCompliant = summary.ErrorRatePercent <= sla.TargetErrorRatePercent;
        var throughputCompliant = !sla.TargetThroughputRps.HasValue || summary.RequestsPerSecond >= sla.TargetThroughputRps.Value;

        return responseTimeCompliant && availabilityCompliant && errorRateCompliant && throughputCompliant;
    }

    private double CalculateCompliancePercentage(SlaDefinition sla, PerformanceSummary summary, double uptimePercentage)
    {
        var factors = new List<double>();

        // Response time compliance
        var targetResponseTime = sla.TargetResponseTimeMs;
        var actualResponseTime = sla.ResponseTimePercentile switch
        {
            50 => summary.P50ResponseTimeMs,
            95 => summary.P95ResponseTimeMs,
            99 => summary.P99ResponseTimeMs,
            _ => summary.AverageResponseTimeMs
        };
        factors.Add(Math.Min(100.0, (targetResponseTime / Math.Max(actualResponseTime, 1)) * 100.0));

        // Availability compliance
        factors.Add(Math.Min(100.0, (summary.AvailabilityPercent / sla.TargetAvailabilityPercent) * 100.0));

        // Error rate compliance (inverted)
        if (summary.ErrorRatePercent > 0)
        {
            factors.Add(Math.Min(100.0, (sla.TargetErrorRatePercent / summary.ErrorRatePercent) * 100.0));
        }
        else
        {
            factors.Add(100.0);
        }

        // Throughput compliance
        if (sla.TargetThroughputRps.HasValue && sla.TargetThroughputRps.Value > 0)
        {
            factors.Add(Math.Min(100.0, (summary.RequestsPerSecond / sla.TargetThroughputRps.Value) * 100.0));
        }

        return factors.Any() ? factors.Average() : 100.0;
    }

    private SlaDefinition? GetSlaDefinition(Guid slaDefinitionId)
    {
        lock (_lock)
        {
            return _slaDefinitions.FirstOrDefault(s => s.Id == slaDefinitionId);
        }
    }

    private void InitializeDefaultSlas()
    {
        var defaultSlas = new[]
        {
            new SlaDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Event API Response Time",
                ServiceName = "Event.API",
                TargetResponseTimeMs = 500,
                TargetAvailabilityPercent = 99.9,
                TargetErrorRatePercent = 1.0,
                ResponseTimePercentile = 95,
                TimeWindowMinutes = 5,
                IsActive = true
            },
            new SlaDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Event Search Performance",
                ServiceName = "Event.API",
                OperationName = "SearchEvents",
                TargetResponseTimeMs = 200,
                TargetAvailabilityPercent = 99.5,
                TargetErrorRatePercent = 0.5,
                TargetThroughputRps = 100,
                ResponseTimePercentile = 95,
                TimeWindowMinutes = 5,
                IsActive = true
            },
            new SlaDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Event Reservation Performance",
                ServiceName = "Event.API",
                OperationName = "CreateReservation",
                TargetResponseTimeMs = 1000,
                TargetAvailabilityPercent = 99.9,
                TargetErrorRatePercent = 0.1,
                ResponseTimePercentile = 95,
                TimeWindowMinutes = 5,
                IsActive = true
            }
        };

        lock (_lock)
        {
            _slaDefinitions.AddRange(defaultSlas);
        }

        _logger.LogInformation("Initialized {Count} default SLA definitions", defaultSlas.Length);
    }
}
