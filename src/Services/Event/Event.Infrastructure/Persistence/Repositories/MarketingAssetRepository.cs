using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for MarketingAsset
/// </summary>
public class MarketingAssetRepository : OrganizationAwareRepository<MarketingAsset>, IMarketingAssetRepository
{
    public MarketingAssetRepository(
        EventDbContext context,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<MarketingAssetRepository> logger)
        : base(context, organizationContextProvider, logger)
    {
    }

    public async Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByOrganizationAsync(
        Guid organizationId,
        AssetType? type = null,
        AssetStatus? status = null,
        Guid? categoryId = null,
        string? searchTerm = null,
        List<string>? tags = null,
        List<AssetUsageContext>? usageContexts = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int skip = 0,
        int take = 20,
        string sortBy = "CreatedAt",
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.OrganizationId == organizationId);

        // Apply filters
        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(a => a.Name.ToLower().Contains(searchLower) || 
                                   a.Description.ToLower().Contains(searchLower));
        }

        if (tags != null && tags.Any())
        {
            // This would require a more complex query for JSON array contains
            // For now, we'll use a simplified approach
            foreach (var tag in tags)
            {
                var tagLower = tag.ToLower();
                query = query.Where(a => EF.Functions.JsonContains(
                    EF.Property<string>(a, "_tags"), $"\"{tagLower}\""));
            }
        }

        if (usageContexts != null && usageContexts.Any())
        {
            foreach (var context in usageContexts)
            {
                query = query.Where(a => EF.Functions.JsonContains(
                    EF.Property<string>(a, "_usageContexts"), $"\"{context}\""));
            }
        }

        if (createdAfter.HasValue)
            query = query.Where(a => a.CreatedAt >= createdAfter.Value);

        if (createdBefore.HasValue)
            query = query.Where(a => a.CreatedAt <= createdBefore.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "name" => sortDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
            "type" => sortDescending ? query.OrderByDescending(a => a.Type) : query.OrderBy(a => a.Type),
            "status" => sortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "usagecount" => sortDescending ? query.OrderByDescending(a => a.UsageCount) : query.OrderBy(a => a.UsageCount),
            "lastused" => sortDescending ? query.OrderByDescending(a => a.LastUsedAt) : query.OrderBy(a => a.LastUsedAt),
            "updatedat" => sortDescending ? query.OrderByDescending(a => a.UpdatedAt) : query.OrderBy(a => a.UpdatedAt),
            _ => sortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        // Apply pagination
        var assets = await query
            .Skip(skip)
            .Take(take)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);

        return (assets, totalCount);
    }

    public async Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByCategoryAsync(
        Guid categoryId,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.CategoryId == categoryId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);

        return (assets, totalCount);
    }

    public async Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByTypeAsync(
        AssetType type,
        Guid? organizationId = null,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.Type == type);

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);

        return (assets, totalCount);
    }

    public async Task<IEnumerable<MarketingAsset>> GetByUsageContextAsync(
        AssetUsageContext context,
        Guid? organizationId = null,
        AssetStatus? status = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => EF.Functions.JsonContains(
            EF.Property<string>(a, "_usageContexts"), $"\"{context}\""));

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.LastUsedAt ?? a.CreatedAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetByTagsAsync(
        List<string> tags,
        Guid? organizationId = null,
        bool matchAll = false,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        if (matchAll)
        {
            // All tags must be present
            foreach (var tag in tags)
            {
                var tagLower = tag.ToLower();
                query = query.Where(a => EF.Functions.JsonContains(
                    EF.Property<string>(a, "_tags"), $"\"{tagLower}\""));
            }
        }
        else
        {
            // Any tag can be present - this is more complex with JSON
            // For now, we'll use a simplified approach
            if (tags.Any())
            {
                var firstTag = tags.First().ToLower();
                var tagQuery = query.Where(a => EF.Functions.JsonContains(
                    EF.Property<string>(a, "_tags"), $"\"{firstTag}\""));

                foreach (var tag in tags.Skip(1))
                {
                    var tagLower = tag.ToLower();
                    var additionalQuery = DbSet.Where(a => EF.Functions.JsonContains(
                        EF.Property<string>(a, "_tags"), $"\"{tagLower}\""));
                    
                    if (organizationId.HasValue)
                        additionalQuery = additionalQuery.Where(a => a.OrganizationId == organizationId.Value);
                    
                    tagQuery = tagQuery.Union(additionalQuery);
                }
                query = tagQuery;
            }
        }

        return await query
            .OrderByDescending(a => a.UsageCount)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetRecentlyUsedAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.OrganizationId == organizationId && a.LastUsedAt.HasValue)
            .OrderByDescending(a => a.LastUsedAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetPopularAssetsAsync(
        Guid organizationId,
        AssetType? type = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.OrganizationId == organizationId && a.UsageCount > 0);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        return await query
            .OrderByDescending(a => a.UsageCount)
            .ThenByDescending(a => a.LastUsedAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetAssetsRequiringApprovalAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.OrganizationId == organizationId && a.Status == AssetStatus.UnderReview)
            .OrderBy(a => a.CreatedAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetExpiredAssetsAsync(
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value <= DateTime.UtcNow);

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        return await query
            .OrderBy(a => a.ExpiresAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetByChecksumAsync(
        string checksum,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.FileInfo.Checksum == checksum);

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        return await query
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetVersion>> GetAssetVersionsAsync(
        Guid assetId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AssetVersion>()
            .Where(v => v.AssetId == assetId)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssetVersion?> GetAssetVersionAsync(
        Guid assetId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AssetVersion>()
            .FirstOrDefaultAsync(v => v.AssetId == assetId && v.VersionNumber == versionNumber, cancellationToken);
    }

    public async Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> SearchAssetsAsync(
        string searchTerm,
        Guid? organizationId = null,
        AssetType? type = null,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(searchLower) ||
                a.Description.ToLower().Contains(searchLower) ||
                EF.Functions.JsonContains(EF.Property<string>(a, "_tags"), $"\"{searchLower}\""));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);

        return (assets, totalCount);
    }

    public async Task<Dictionary<string, object>> GetAssetUsageStatsAsync(
        Guid assetId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var asset = await GetByIdAsync(assetId, cancellationToken);
        if (asset == null)
            return new Dictionary<string, object>();

        var stats = new Dictionary<string, object>
        {
            ["AssetId"] = assetId,
            ["TotalUsageCount"] = asset.UsageCount,
            ["LastUsedAt"] = asset.LastUsedAt,
            ["CurrentVersion"] = asset.CurrentVersion,
            ["TotalVersions"] = asset.Versions.Count,
            ["Status"] = asset.Status.ToString(),
            ["Type"] = asset.Type.ToString()
        };

        // Add version-specific stats
        var versionStats = asset.Versions.Select(v => new
        {
            Version = v.VersionNumber,
            UsageCount = v.UsageCount,
            LastUsed = v.LastUsedAt,
            IsProcessed = v.IsProcessed
        }).ToList();

        stats["VersionStats"] = versionStats;

        return stats;
    }

    public async Task<Dictionary<string, object>> GetOrganizationAssetStatsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var assets = await DbSet
            .Where(a => a.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        var stats = new Dictionary<string, object>
        {
            ["OrganizationId"] = organizationId,
            ["TotalAssets"] = assets.Count,
            ["AssetsByType"] = assets.GroupBy(a => a.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["AssetsByStatus"] = assets.GroupBy(a => a.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["TotalUsageCount"] = assets.Sum(a => a.UsageCount),
            ["AverageUsageCount"] = assets.Any() ? assets.Average(a => a.UsageCount) : 0,
            ["MostUsedAsset"] = assets.OrderByDescending(a => a.UsageCount).FirstOrDefault()?.Name,
            ["RecentlyCreated"] = assets.Count(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
            ["ExpiredAssets"] = assets.Count(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value <= DateTime.UtcNow),
            ["AssetsRequiringApproval"] = assets.Count(a => a.Status == AssetStatus.UnderReview)
        };

        return stats;
    }

    public async Task<bool> IsNameUniqueAsync(
        string name,
        Guid organizationId,
        Guid? excludeAssetId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.OrganizationId == organizationId && a.Name == name);

        if (excludeAssetId.HasValue)
            query = query.Where(a => a.Id != excludeAssetId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetAssetsExpiringAsync(
        DateTime beforeDate,
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value <= beforeDate && a.ExpiresAt.Value > DateTime.UtcNow);

        if (organizationId.HasValue)
            query = query.Where(a => a.OrganizationId == organizationId.Value);

        return await query
            .OrderBy(a => a.ExpiresAt)
            .Take(limit)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(
        List<Guid> assetIds,
        AssetStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var assets = await DbSet
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        foreach (var asset in assets)
        {
            asset.SetStatus(newStatus);
        }

        return await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<MarketingAsset>> GetAssetVariationsAsync(
        Guid parentAssetId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(a => a.ParentAssetId == parentAssetId)
            .Include(a => a.Versions)
            .ToListAsync(cancellationToken);
    }
}
