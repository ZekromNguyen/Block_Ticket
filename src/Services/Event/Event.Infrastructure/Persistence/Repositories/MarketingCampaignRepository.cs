using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for MarketingCampaign
/// </summary>
public class MarketingCampaignRepository : OrganizationAwareRepository<MarketingCampaign>, IMarketingCampaignRepository
{
    public MarketingCampaignRepository(
        EventDbContext context,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<MarketingCampaignRepository> logger)
        : base(context, organizationContextProvider, logger)
    {
    }

    public async Task<(IEnumerable<MarketingCampaign> Campaigns, int TotalCount)> GetByOrganizationAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        // Apply filters
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (context.HasValue)
            query = query.Where(c => c.PrimaryContext == context.Value);

        if (startDateAfter.HasValue)
            query = query.Where(c => c.StartDate >= startDateAfter.Value);

        if (startDateBefore.HasValue)
            query = query.Where(c => c.StartDate <= startDateBefore.Value);

        if (isABTest.HasValue)
            query = query.Where(c => c.IsABTest == isABTest.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) || 
                                   c.Description.ToLower().Contains(searchLower));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "name" => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "status" => sortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "startdate" => sortDescending ? query.OrderByDescending(c => c.StartDate) : query.OrderBy(c => c.StartDate),
            "enddate" => sortDescending ? query.OrderByDescending(c => c.EndDate) : query.OrderBy(c => c.EndDate),
            "impressions" => sortDescending ? query.OrderByDescending(c => c.TotalImpressions) : query.OrderBy(c => c.TotalImpressions),
            "clicks" => sortDescending ? query.OrderByDescending(c => c.TotalClicks) : query.OrderBy(c => c.TotalClicks),
            "conversions" => sortDescending ? query.OrderByDescending(c => c.TotalConversions) : query.OrderBy(c => c.TotalConversions),
            "spent" => sortDescending ? query.OrderByDescending(c => c.TotalSpent) : query.OrderBy(c => c.TotalSpent),
            "updatedat" => sortDescending ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
            _ => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        // Apply pagination
        var campaigns = await query
            .Skip(skip)
            .Take(take)
            .Include(c => c.Variants)
            .ToListAsync(cancellationToken);

        return (campaigns, totalCount);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetActiveCampaignsAsync(
        Guid organizationId,
        AssetUsageContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.Status == CampaignStatus.Active);

        if (context.HasValue)
            query = query.Where(c => c.PrimaryContext == context.Value);

        return await query
            .Include(c => c.Variants)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetByStatusAsync(
        CampaignStatus status,
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.Status == status);

        if (organizationId.HasValue)
            query = query.Where(c => c.OrganizationId == organizationId.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Include(c => c.Variants)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsTargetingEventAsync(
        Guid eventId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => EF.Functions.JsonContains(
            EF.Property<string>(c, "_targetEventIds"), $"\"{eventId}\""));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .Include(c => c.Variants)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsTargetingVenueAsync(
        Guid venueId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => EF.Functions.JsonContains(
            EF.Property<string>(c, "_targetVenueIds"), $"\"{venueId}\""));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .Include(c => c.Variants)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsUsingAssetAsync(
        Guid assetId,
        CampaignStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        // This requires joining with CampaignVariant to check asset usage
        var query = from campaign in DbSet
                    join variant in Context.Set<CampaignVariant>() on campaign.Id equals variant.CampaignId
                    where variant.PrimaryAssetId == assetId || 
                          EF.Functions.JsonContains(EF.Property<string>(variant, "_assetIds"), $"\"{assetId}\"")
                    select campaign;

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .Distinct()
            .Include(c => c.Variants)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetABTestCampaignsAsync(
        Guid organizationId,
        CampaignStatus? status = null,
        bool? hasWinner = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.IsABTest);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (hasWinner.HasValue)
        {
            if (hasWinner.Value)
                query = query.Where(c => c.WinningVariantId.HasValue);
            else
                query = query.Where(c => !c.WinningVariantId.HasValue);
        }

        return await query
            .Include(c => c.Variants)
            .OrderByDescending(c => c.TestCompletedAt ?? c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsToStartAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CampaignStatus.Scheduled && c.StartDate <= beforeDate)
            .Include(c => c.Variants)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsToEndAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CampaignStatus.Active && c.EndDate.HasValue && c.EndDate.Value <= beforeDate)
            .Include(c => c.Variants)
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsExceedingBudgetAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.Budget.HasValue && c.TotalSpent > c.Budget.Value);

        if (organizationId.HasValue)
            query = query.Where(c => c.OrganizationId == organizationId.Value);

        return await query
            .Include(c => c.Variants)
            .OrderByDescending(c => c.TotalSpent - c.Budget)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetCampaignPerformanceSummaryAsync(
        Guid campaignId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var campaign = await DbSet
            .Include(c => c.Variants)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
            return new Dictionary<string, object>();

        var summary = new Dictionary<string, object>
        {
            ["CampaignId"] = campaignId,
            ["Name"] = campaign.Name,
            ["Status"] = campaign.Status.ToString(),
            ["IsABTest"] = campaign.IsABTest,
            ["TotalImpressions"] = campaign.TotalImpressions,
            ["TotalClicks"] = campaign.TotalClicks,
            ["TotalConversions"] = campaign.TotalConversions,
            ["ClickThroughRate"] = campaign.ClickThroughRate,
            ["ConversionRate"] = campaign.ConversionRate,
            ["TotalSpent"] = campaign.TotalSpent,
            ["Budget"] = campaign.Budget,
            ["RemainingBudget"] = campaign.RemainingBudget(),
            ["StartDate"] = campaign.StartDate,
            ["EndDate"] = campaign.EndDate,
            ["VariantCount"] = campaign.Variants.Count
        };

        if (campaign.IsABTest)
        {
            summary["WinningVariantId"] = campaign.WinningVariantId;
            summary["TestCompletedAt"] = campaign.TestCompletedAt;
            summary["StatisticalSignificance"] = campaign.StatisticalSignificance;

            var variantPerformance = campaign.Variants.Select(v => new
            {
                VariantId = v.Id,
                Name = v.Name,
                TrafficPercentage = v.TrafficPercentage,
                Impressions = v.TotalImpressions,
                Clicks = v.TotalClicks,
                Conversions = v.TotalConversions,
                ClickThroughRate = v.ClickThroughRate,
                ConversionRate = v.ConversionRate,
                IsWinner = v.IsWinner,
                IsControl = v.IsControl
            }).ToList();

            summary["VariantPerformance"] = variantPerformance;
        }

        return summary;
    }

    public async Task<Dictionary<string, object>> GetOrganizationCampaignStatsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        if (fromDate.HasValue)
            query = query.Where(c => c.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(c => c.CreatedAt <= toDate.Value);

        var campaigns = await query.Include(c => c.Variants).ToListAsync(cancellationToken);

        var stats = new Dictionary<string, object>
        {
            ["OrganizationId"] = organizationId,
            ["TotalCampaigns"] = campaigns.Count,
            ["CampaignsByStatus"] = campaigns.GroupBy(c => c.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["ABTestCampaigns"] = campaigns.Count(c => c.IsABTest),
            ["CompletedABTests"] = campaigns.Count(c => c.IsABTest && c.WinningVariantId.HasValue),
            ["TotalImpressions"] = campaigns.Sum(c => c.TotalImpressions),
            ["TotalClicks"] = campaigns.Sum(c => c.TotalClicks),
            ["TotalConversions"] = campaigns.Sum(c => c.TotalConversions),
            ["TotalSpent"] = campaigns.Sum(c => c.TotalSpent),
            ["AverageClickThroughRate"] = campaigns.Any() ? campaigns.Average(c => c.ClickThroughRate) : 0,
            ["AverageConversionRate"] = campaigns.Any() ? campaigns.Average(c => c.ConversionRate) : 0,
            ["ActiveCampaigns"] = campaigns.Count(c => c.Status == CampaignStatus.Active),
            ["CampaignsExceedingBudget"] = campaigns.Count(c => c.Budget.HasValue && c.TotalSpent > c.Budget.Value)
        };

        return stats;
    }

    public async Task<IEnumerable<CampaignVariant>> GetCampaignVariantsAsync(
        Guid campaignId,
        VariantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<CampaignVariant>().Where(v => v.CampaignId == campaignId);

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        return await query
            .OrderBy(v => v.IsControl ? 0 : 1) // Control variant first
            .ThenByDescending(v => v.TrafficPercentage)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CampaignVariant>> GetWinningVariantsAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = from variant in Context.Set<CampaignVariant>()
                    join campaign in DbSet on variant.CampaignId equals campaign.Id
                    where campaign.OrganizationId == organizationId && variant.IsWinner
                    select variant;

        if (fromDate.HasValue)
            query = query.Where(v => v.Campaign.TestCompletedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(v => v.Campaign.TestCompletedAt <= toDate.Value);

        return await query
            .Include(v => v.Campaign)
            .OrderByDescending(v => v.Campaign.TestCompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<MarketingCampaign> Campaigns, int TotalCount)> SearchCampaignsAsync(
        string searchTerm,
        Guid? organizationId = null,
        CampaignStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(c => c.OrganizationId == organizationId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) ||
                                   c.Description.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(c => c.Variants)
            .ToListAsync(cancellationToken);

        return (campaigns, totalCount);
    }

    public async Task<Dictionary<DateTime, Dictionary<string, double>>> GetCampaignMetricsByDateAsync(
        Guid campaignId,
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day",
        CancellationToken cancellationToken = default)
    {
        // This would typically require a separate metrics tracking table
        // For now, return a simplified implementation
        var campaign = await GetByIdAsync(campaignId, cancellationToken);
        if (campaign == null)
            return new Dictionary<DateTime, Dictionary<string, double>>();

        var result = new Dictionary<DateTime, Dictionary<string, double>>();
        var current = fromDate.Date;

        while (current <= toDate.Date)
        {
            result[current] = new Dictionary<string, double>
            {
                ["Impressions"] = 0,
                ["Clicks"] = 0,
                ["Conversions"] = 0,
                ["Spent"] = 0
            };

            current = groupBy.ToLower() switch
            {
                "week" => current.AddDays(7),
                "month" => current.AddMonths(1),
                _ => current.AddDays(1)
            };
        }

        return result;
    }

    public async Task<IEnumerable<MarketingCampaign>> GetTopPerformingCampaignsAsync(
        Guid organizationId,
        string metric = "ConversionRate",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        if (fromDate.HasValue)
            query = query.Where(c => c.StartDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(c => c.EndDate == null || c.EndDate.Value <= toDate.Value);

        query = metric.ToLower() switch
        {
            "clickthroughrate" => query.OrderByDescending(c => c.TotalImpressions > 0 ? (double)c.TotalClicks / c.TotalImpressions : 0),
            "conversionrate" => query.OrderByDescending(c => c.TotalClicks > 0 ? (double)c.TotalConversions / c.TotalClicks : 0),
            "impressions" => query.OrderByDescending(c => c.TotalImpressions),
            "clicks" => query.OrderByDescending(c => c.TotalClicks),
            "conversions" => query.OrderByDescending(c => c.TotalConversions),
            "spent" => query.OrderByDescending(c => c.TotalSpent),
            _ => query.OrderByDescending(c => c.TotalClicks > 0 ? (double)c.TotalConversions / c.TotalClicks : 0)
        };

        return await query
            .Take(limit)
            .Include(c => c.Variants)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(
        string name,
        Guid organizationId,
        Guid? excludeCampaignId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.Name == name);

        if (excludeCampaignId.HasValue)
            query = query.Where(c => c.Id != excludeCampaignId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(
        List<Guid> campaignIds,
        CampaignStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var campaigns = await DbSet
            .Where(c => campaignIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var campaign in campaigns)
        {
            campaign.SetStatus(newStatus);
        }

        return await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingCampaign>> GetCampaignsWithInsufficientDataAsync(
        Guid organizationId,
        int minimumImpressions = 100,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.OrganizationId == organizationId &&
                       c.Status == CampaignStatus.Active &&
                       c.TotalImpressions < minimumImpressions)
            .Include(c => c.Variants)
            .OrderBy(c => c.TotalImpressions)
            .ToListAsync(cancellationToken);
    }
}
