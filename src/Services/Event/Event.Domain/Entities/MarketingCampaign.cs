using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Marketing campaign aggregate root - represents a marketing campaign with A/B testing capabilities
/// </summary>
public class MarketingCampaign : BaseAuditableEntity
{
    private readonly List<CampaignVariant> _variants = new();
    private readonly List<Guid> _targetEventIds = new();
    private readonly List<Guid> _targetVenueIds = new();
    private readonly List<string> _targetAudiences = new();
    private readonly Dictionary<string, double> _metrics = new();

    // Basic Properties
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CampaignStatus Status { get; private set; }
    public Guid OrganizationId { get; private set; }

    // Scheduling
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public TimeZoneId TimeZone { get; private set; } = null!;

    // Targeting
    public AssetUsageContext PrimaryContext { get; private set; }
    public bool IsABTest { get; private set; }
    public double? ConfidenceThreshold { get; private set; } = 95.0; // For A/B testing

    // Performance Tracking
    public int TotalImpressions { get; private set; }
    public int TotalClicks { get; private set; }
    public int TotalConversions { get; private set; }
    public double ClickThroughRate => TotalImpressions > 0 ? (double)TotalClicks / TotalImpressions * 100 : 0;
    public double ConversionRate => TotalClicks > 0 ? (double)TotalConversions / TotalClicks * 100 : 0;

    // A/B Testing
    public Guid? WinningVariantId { get; private set; }
    public DateTime? TestCompletedAt { get; private set; }
    public double? StatisticalSignificance { get; private set; }

    // Budget and Cost
    public decimal? Budget { get; private set; }
    public decimal TotalSpent { get; private set; }
    public string Currency { get; private set; } = "USD";

    // Navigation Properties
    public IReadOnlyCollection<CampaignVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<Guid> TargetEventIds => _targetEventIds.AsReadOnly();
    public IReadOnlyCollection<Guid> TargetVenueIds => _targetVenueIds.AsReadOnly();
    public IReadOnlyCollection<string> TargetAudiences => _targetAudiences.AsReadOnly();
    public IReadOnlyDictionary<string, double> Metrics => _metrics.AsReadOnly();

    // Private constructor for EF Core
    private MarketingCampaign() { }

    public MarketingCampaign(
        string name,
        string description,
        Guid organizationId,
        DateTime startDate,
        DateTime? endDate,
        AssetUsageContext primaryContext,
        string timeZone = "UTC")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Campaign name cannot be empty");

        if (organizationId == Guid.Empty)
            throw new EventDomainException("Organization ID cannot be empty");

        if (startDate < DateTime.UtcNow.AddMinutes(-5)) // Allow 5 minutes tolerance
            throw new EventDomainException("Campaign start date cannot be in the past");

        if (endDate.HasValue && endDate.Value <= startDate)
            throw new EventDomainException("Campaign end date must be after start date");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        OrganizationId = organizationId;
        StartDate = startDate;
        EndDate = endDate;
        PrimaryContext = primaryContext;
        TimeZone = new TimeZoneId(timeZone);
        Status = CampaignStatus.Draft;
        IsABTest = false;
        TotalImpressions = 0;
        TotalClicks = 0;
        TotalConversions = 0;
        TotalSpent = 0;

        AddDomainEvent(new MarketingCampaignCreatedDomainEvent(Id, Name, OrganizationId, StartDate, EndDate));
    }

    public void UpdateBasicInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Campaign name cannot be empty");

        if (Status == CampaignStatus.Active)
            throw new EventDomainException("Cannot update active campaign basic info");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public void UpdateSchedule(DateTime startDate, DateTime? endDate)
    {
        if (Status == CampaignStatus.Active || Status == CampaignStatus.Completed)
            throw new EventDomainException("Cannot update schedule of active or completed campaign");

        if (startDate < DateTime.UtcNow.AddMinutes(-5))
            throw new EventDomainException("Campaign start date cannot be in the past");

        if (endDate.HasValue && endDate.Value <= startDate)
            throw new EventDomainException("Campaign end date must be after start date");

        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetBudget(decimal budget, string currency = "USD")
    {
        if (budget < 0)
            throw new EventDomainException("Budget cannot be negative");

        Budget = budget;
        Currency = currency?.Trim().ToUpperInvariant() ?? "USD";
    }

    public void SetStatus(CampaignStatus status)
    {
        if (Status == status) return;

        ValidateStatusTransition(status);

        var oldStatus = Status;
        Status = status;

        if (status == CampaignStatus.Active)
        {
            ValidateCanActivate();
        }
        else if (status == CampaignStatus.Completed)
        {
            CompleteABTestIfRunning();
        }

        AddDomainEvent(new MarketingCampaignStatusChangedDomainEvent(Id, oldStatus, status));
    }

    public CampaignVariant AddVariant(string name, string description, Guid primaryAssetId, double trafficPercentage = 50.0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Variant name cannot be empty");

        if (primaryAssetId == Guid.Empty)
            throw new EventDomainException("Primary asset ID cannot be empty");

        if (trafficPercentage <= 0 || trafficPercentage > 100)
            throw new EventDomainException("Traffic percentage must be between 0 and 100");

        if (Status == CampaignStatus.Active || Status == CampaignStatus.Completed)
            throw new EventDomainException("Cannot add variants to active or completed campaign");

        // Validate total traffic doesn't exceed 100%
        var totalTraffic = _variants.Sum(v => v.TrafficPercentage) + trafficPercentage;
        if (totalTraffic > 100)
            throw new EventDomainException($"Total traffic percentage would exceed 100% (current: {totalTraffic}%)");

        var variant = new CampaignVariant(Id, name, description, primaryAssetId, trafficPercentage);
        _variants.Add(variant);

        // Enable A/B testing if we have more than one variant
        if (_variants.Count > 1)
        {
            IsABTest = true;
        }

        AddDomainEvent(new CampaignVariantCreatedDomainEvent(Id, variant.Id, name, trafficPercentage));

        return variant;
    }

    public void RemoveVariant(Guid variantId)
    {
        if (Status == CampaignStatus.Active || Status == CampaignStatus.Completed)
            throw new EventDomainException("Cannot remove variants from active or completed campaign");

        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant != null)
        {
            _variants.Remove(variant);

            // Disable A/B testing if we have only one variant left
            if (_variants.Count <= 1)
            {
                IsABTest = false;
            }
        }
    }

    public void AddTargetEvent(Guid eventId)
    {
        if (eventId != Guid.Empty && !_targetEventIds.Contains(eventId))
        {
            _targetEventIds.Add(eventId);
        }
    }

    public void RemoveTargetEvent(Guid eventId)
    {
        _targetEventIds.Remove(eventId);
    }

    public void AddTargetVenue(Guid venueId)
    {
        if (venueId != Guid.Empty && !_targetVenueIds.Contains(venueId))
        {
            _targetVenueIds.Add(venueId);
        }
    }

    public void RemoveTargetVenue(Guid venueId)
    {
        _targetVenueIds.Remove(venueId);
    }

    public void AddTargetAudience(string audience)
    {
        if (!string.IsNullOrWhiteSpace(audience) && !_targetAudiences.Contains(audience))
        {
            _targetAudiences.Add(audience.Trim());
        }
    }

    public void RemoveTargetAudience(string audience)
    {
        if (!string.IsNullOrWhiteSpace(audience))
        {
            _targetAudiences.Remove(audience.Trim());
        }
    }

    public void RecordImpression(Guid? variantId = null)
    {
        TotalImpressions++;
        
        if (variantId.HasValue)
        {
            var variant = _variants.FirstOrDefault(v => v.Id == variantId.Value);
            variant?.RecordImpression();
        }
    }

    public void RecordClick(Guid? variantId = null)
    {
        TotalClicks++;
        
        if (variantId.HasValue)
        {
            var variant = _variants.FirstOrDefault(v => v.Id == variantId.Value);
            variant?.RecordClick();
        }
    }

    public void RecordConversion(Guid? variantId = null, decimal value = 0)
    {
        TotalConversions++;
        
        if (variantId.HasValue)
        {
            var variant = _variants.FirstOrDefault(v => v.Id == variantId.Value);
            variant?.RecordConversion(value);
        }
    }

    public void UpdateMetric(string metricName, double value)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new EventDomainException("Metric name cannot be empty");

        _metrics[metricName.Trim()] = value;
        AddDomainEvent(new CampaignMetricsUpdatedDomainEvent(Id, _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), DateTime.UtcNow));
    }

    public void AddSpent(decimal amount)
    {
        if (amount < 0)
            throw new EventDomainException("Spent amount cannot be negative");

        TotalSpent += amount;

        if (Budget.HasValue && TotalSpent > Budget.Value)
        {
            // Optionally pause campaign if budget exceeded
            if (Status == CampaignStatus.Active)
            {
                SetStatus(CampaignStatus.Paused);
            }
        }
    }

    private void ValidateStatusTransition(CampaignStatus newStatus)
    {
        var validTransitions = Status switch
        {
            CampaignStatus.Draft => new[] { CampaignStatus.Scheduled, CampaignStatus.Active, CampaignStatus.Cancelled },
            CampaignStatus.Scheduled => new[] { CampaignStatus.Active, CampaignStatus.Cancelled },
            CampaignStatus.Active => new[] { CampaignStatus.Paused, CampaignStatus.Completed, CampaignStatus.Cancelled },
            CampaignStatus.Paused => new[] { CampaignStatus.Active, CampaignStatus.Completed, CampaignStatus.Cancelled },
            CampaignStatus.Completed => new[] { CampaignStatus.Archived },
            CampaignStatus.Cancelled => new[] { CampaignStatus.Archived },
            CampaignStatus.Archived => Array.Empty<CampaignStatus>(),
            _ => Array.Empty<CampaignStatus>()
        };

        if (!validTransitions.Contains(newStatus))
        {
            throw new EventDomainException($"Cannot transition from {Status} to {newStatus}");
        }
    }

    private void ValidateCanActivate()
    {
        if (!_variants.Any())
            throw new EventDomainException("Campaign must have at least one variant to activate");

        if (IsABTest && _variants.Count < 2)
            throw new EventDomainException("A/B test campaign must have at least two variants");

        var totalTraffic = _variants.Sum(v => v.TrafficPercentage);
        if (Math.Abs(totalTraffic - 100) > 0.01) // Allow small floating point differences
            throw new EventDomainException($"Total traffic percentage must equal 100% (current: {totalTraffic}%)");
    }

    private void CompleteABTestIfRunning()
    {
        if (IsABTest && !WinningVariantId.HasValue && _variants.Count > 1)
        {
            // Determine winning variant based on conversion rate
            var winningVariant = _variants
                .Where(v => v.TotalImpressions > 0)
                .OrderByDescending(v => v.ConversionRate)
                .FirstOrDefault();

            if (winningVariant != null)
            {
                WinningVariantId = winningVariant.Id;
                TestCompletedAt = DateTime.UtcNow;
                
                // Calculate statistical significance (simplified)
                StatisticalSignificance = CalculateStatisticalSignificance();

                var metrics = _variants.ToDictionary(v => v.Id.ToString(), v => v.ConversionRate);
                AddDomainEvent(new ABTestResultsAvailableDomainEvent(Id, winningVariant.Id, StatisticalSignificance.Value, metrics));
            }
        }
    }

    private double CalculateStatisticalSignificance()
    {
        // Simplified statistical significance calculation
        // In a real implementation, you would use proper statistical tests
        if (_variants.Count < 2) return 0;

        var sortedVariants = _variants.OrderByDescending(v => v.ConversionRate).ToList();
        if (sortedVariants.Count < 2) return 0;

        var winner = sortedVariants[0];
        var runnerUp = sortedVariants[1];

        if (winner.TotalImpressions < 100 || runnerUp.TotalImpressions < 100)
            return 0; // Not enough data

        // Simple confidence calculation based on sample size and difference
        var difference = Math.Abs(winner.ConversionRate - runnerUp.ConversionRate);
        var sampleSizeScore = Math.Min(winner.TotalImpressions + runnerUp.TotalImpressions, 10000) / 10000.0;
        
        return Math.Min(95, difference * sampleSizeScore * 100);
    }

    public bool IsActive() => Status == CampaignStatus.Active;
    public bool IsCompleted() => Status == CampaignStatus.Completed;
    public bool HasBudgetRemaining() => !Budget.HasValue || TotalSpent < Budget.Value;
    public decimal? RemainingBudget() => Budget.HasValue ? Budget.Value - TotalSpent : null;
}
