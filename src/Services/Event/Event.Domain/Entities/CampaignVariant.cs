using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Campaign variant entity - represents a variant in an A/B test campaign
/// </summary>
public class CampaignVariant : BaseAuditableEntity
{
    private readonly List<Guid> _assetIds = new();
    private readonly Dictionary<string, double> _metrics = new();

    // Basic Properties
    public Guid CampaignId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public VariantStatus Status { get; private set; }

    // A/B Testing
    public double TrafficPercentage { get; private set; }
    public bool IsControl { get; private set; }
    public bool IsWinner { get; private set; }

    // Assets
    public Guid PrimaryAssetId { get; private set; }

    // Performance Metrics
    public int TotalImpressions { get; private set; }
    public int TotalClicks { get; private set; }
    public int TotalConversions { get; private set; }
    public decimal TotalConversionValue { get; private set; }
    
    // Calculated Metrics
    public double ClickThroughRate => TotalImpressions > 0 ? (double)TotalClicks / TotalImpressions * 100 : 0;
    public double ConversionRate => TotalClicks > 0 ? (double)TotalConversions / TotalClicks * 100 : 0;
    public decimal AverageConversionValue => TotalConversions > 0 ? TotalConversionValue / TotalConversions : 0;

    // Statistical Data
    public double? ConfidenceLevel { get; private set; }
    public double? StatisticalSignificance { get; private set; }

    // Navigation Properties
    public MarketingCampaign Campaign { get; private set; } = null!;
    public IReadOnlyCollection<Guid> AssetIds => _assetIds.AsReadOnly();
    public IReadOnlyDictionary<string, double> Metrics => _metrics.AsReadOnly();

    // Private constructor for EF Core
    private CampaignVariant() { }

    public CampaignVariant(
        Guid campaignId,
        string name,
        string description,
        Guid primaryAssetId,
        double trafficPercentage,
        bool isControl = false)
    {
        if (campaignId == Guid.Empty)
            throw new EventDomainException("Campaign ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Variant name cannot be empty");

        if (primaryAssetId == Guid.Empty)
            throw new EventDomainException("Primary asset ID cannot be empty");

        if (trafficPercentage <= 0 || trafficPercentage > 100)
            throw new EventDomainException("Traffic percentage must be between 0 and 100");

        Id = Guid.NewGuid();
        CampaignId = campaignId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        PrimaryAssetId = primaryAssetId;
        TrafficPercentage = trafficPercentage;
        IsControl = isControl;
        Status = VariantStatus.Active;
        IsWinner = false;
        TotalImpressions = 0;
        TotalClicks = 0;
        TotalConversions = 0;
        TotalConversionValue = 0;

        _assetIds.Add(primaryAssetId);
    }

    public void UpdateBasicInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Variant name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public void SetTrafficPercentage(double percentage)
    {
        if (percentage <= 0 || percentage > 100)
            throw new EventDomainException("Traffic percentage must be between 0 and 100");

        TrafficPercentage = percentage;
    }

    public void SetStatus(VariantStatus status)
    {
        if (Status == status) return;

        Status = status;

        if (status == VariantStatus.Winner)
        {
            IsWinner = true;
        }
        else if (IsWinner && status != VariantStatus.Winner)
        {
            IsWinner = false;
        }
    }

    public void SetAsControl()
    {
        IsControl = true;
    }

    public void SetAsWinner(double? confidenceLevel = null, double? statisticalSignificance = null)
    {
        IsWinner = true;
        Status = VariantStatus.Winner;
        ConfidenceLevel = confidenceLevel;
        StatisticalSignificance = statisticalSignificance;
    }

    public void AddAsset(Guid assetId)
    {
        if (assetId != Guid.Empty && !_assetIds.Contains(assetId))
        {
            _assetIds.Add(assetId);
        }
    }

    public void RemoveAsset(Guid assetId)
    {
        if (assetId == PrimaryAssetId)
            throw new EventDomainException("Cannot remove primary asset from variant");

        _assetIds.Remove(assetId);
    }

    public void RecordImpression()
    {
        if (Status != VariantStatus.Active)
            return;

        TotalImpressions++;
    }

    public void RecordClick()
    {
        if (Status != VariantStatus.Active)
            return;

        TotalClicks++;
    }

    public void RecordConversion(decimal value = 0)
    {
        if (Status != VariantStatus.Active)
            return;

        TotalConversions++;
        TotalConversionValue += value;
    }

    public void UpdateMetric(string metricName, double value)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new EventDomainException("Metric name cannot be empty");

        _metrics[metricName.Trim()] = value;
    }

    public double GetMetric(string metricName)
    {
        return _metrics.TryGetValue(metricName, out var value) ? value : 0;
    }

    public void ResetMetrics()
    {
        TotalImpressions = 0;
        TotalClicks = 0;
        TotalConversions = 0;
        TotalConversionValue = 0;
        _metrics.Clear();
        ConfidenceLevel = null;
        StatisticalSignificance = null;
    }

    public bool HasSufficientData(int minimumImpressions = 100)
    {
        return TotalImpressions >= minimumImpressions;
    }

    public bool IsPerformingBetter(CampaignVariant other, string metric = "ConversionRate")
    {
        if (other == null) return true;

        return metric.ToLowerInvariant() switch
        {
            "conversionrate" => ConversionRate > other.ConversionRate,
            "clickthroughrate" => ClickThroughRate > other.ClickThroughRate,
            "averageconversionvalue" => AverageConversionValue > other.AverageConversionValue,
            _ => GetMetric(metric) > other.GetMetric(metric)
        };
    }

    public Dictionary<string, object> GetPerformanceSnapshot()
    {
        return new Dictionary<string, object>
        {
            ["TotalImpressions"] = TotalImpressions,
            ["TotalClicks"] = TotalClicks,
            ["TotalConversions"] = TotalConversions,
            ["ClickThroughRate"] = ClickThroughRate,
            ["ConversionRate"] = ConversionRate,
            ["AverageConversionValue"] = AverageConversionValue,
            ["TotalConversionValue"] = TotalConversionValue,
            ["ConfidenceLevel"] = ConfidenceLevel ?? 0,
            ["StatisticalSignificance"] = StatisticalSignificance ?? 0,
            ["IsWinner"] = IsWinner,
            ["Status"] = Status.ToString()
        };
    }
}
