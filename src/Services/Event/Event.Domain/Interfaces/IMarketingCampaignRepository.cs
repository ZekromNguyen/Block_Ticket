using Event.Domain.Entities;
using Event.Domain.Enums;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for marketing campaigns
/// </summary>
public interface IMarketingCampaignRepository : IRepository<MarketingCampaign>
{
    /// <summary>
    /// Get campaigns by organization with filtering and pagination
    /// </summary>
    Task<(IEnumerable<MarketingCampaign> Campaigns, int TotalCount)> GetByOrganizationAsync(
        Guid organizationId,
        CampaignStatus? status = null,
        AssetUsageContext? context = null,
        DateTime? startDateAfter = null,
        DateTime? startDateBefore = null,
        bool? isABTest = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 20,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active campaigns for organization
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetActiveCampaignsAsync(
        Guid organizationId,
        AssetUsageContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns by status
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetByStatusAsync(
        CampaignStatus status,
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns targeting specific event
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsTargetingEventAsync(
        Guid eventId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns targeting specific venue
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsTargetingVenueAsync(
        Guid venueId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns using specific asset
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsUsingAssetAsync(
        Guid assetId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get A/B test campaigns
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetABTestCampaignsAsync(
        Guid organizationId,
        CampaignStatus? status = null,
        bool? hasWinner = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns scheduled to start
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsToStartAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns scheduled to end
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsToEndAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns exceeding budget
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsExceedingBudgetAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaign performance summary
    /// </summary>
    Task<Dictionary<string, object>> GetCampaignPerformanceSummaryAsync(
        Guid campaignId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization campaign statistics
    /// </summary>
    Task<Dictionary<string, object>> GetOrganizationCampaignStatsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaign variants for a campaign
    /// </summary>
    Task<IEnumerable<CampaignVariant>> GetCampaignVariantsAsync(
        Guid campaignId,
        VariantStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get winning variants from completed A/B tests
    /// </summary>
    Task<IEnumerable<CampaignVariant>> GetWinningVariantsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search campaigns by name or description
    /// </summary>
    Task<(IEnumerable<MarketingCampaign> Campaigns, int TotalCount)> SearchCampaignsAsync(
        string searchTerm,
        Guid? organizationId = null,
        CampaignStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaign metrics aggregated by date range
    /// </summary>
    Task<Dictionary<DateTime, Dictionary<string, double>>> GetCampaignMetricsByDateAsync(
        Guid campaignId,
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day", // day, week, month
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top performing campaigns
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetTopPerformingCampaignsAsync(
        Guid organizationId,
        string metric = "ConversionRate", // ConversionRate, ClickThroughRate, etc.
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if campaign name is unique within organization
    /// </summary>
    Task<bool> IsNameUniqueAsync(
        string name,
        Guid organizationId,
        Guid? excludeCampaignId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update campaign status
    /// </summary>
    Task<int> BulkUpdateStatusAsync(
        List<Guid> campaignIds,
        CampaignStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get campaigns with insufficient performance data
    /// </summary>
    Task<IEnumerable<MarketingCampaign>> GetCampaignsWithInsufficientDataAsync(
        Guid organizationId,
        int minimumImpressions = 100,
        CancellationToken cancellationToken = default);
}
