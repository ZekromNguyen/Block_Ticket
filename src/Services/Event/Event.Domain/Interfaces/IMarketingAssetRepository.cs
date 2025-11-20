using Event.Domain.Entities;
using Event.Domain.Enums;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for marketing assets
/// </summary>
public interface IMarketingAssetRepository : IRepository<MarketingAsset>
{
    /// <summary>
    /// Get assets by organization with filtering and pagination
    /// </summary>
    Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByOrganizationAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by category with pagination
    /// </summary>
    Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByCategoryAsync(
        Guid categoryId,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by type with pagination
    /// </summary>
    Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> GetByTypeAsync(
        AssetType type,
        Guid? organizationId = null,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by usage context
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetByUsageContextAsync(
        AssetUsageContext context,
        Guid? organizationId = null,
        AssetStatus? status = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by tags
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetByTagsAsync(
        List<string> tags,
        Guid? organizationId = null,
        bool matchAll = false,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recently used assets
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetRecentlyUsedAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get popular assets by usage count
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetPopularAssetsAsync(
        Guid organizationId,
        AssetType? type = null,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets requiring approval
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetAssetsRequiringApprovalAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired assets
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetExpiredAssetsAsync(
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by file checksum (for duplicate detection)
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetByChecksumAsync(
        string checksum,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get asset versions for a specific asset
    /// </summary>
    Task<IEnumerable<AssetVersion>> GetAssetVersionsAsync(
        Guid assetId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific asset version
    /// </summary>
    Task<AssetVersion?> GetAssetVersionAsync(
        Guid assetId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search assets with full-text search
    /// </summary>
    Task<(IEnumerable<MarketingAsset> Assets, int TotalCount)> SearchAssetsAsync(
        string searchTerm,
        Guid? organizationId = null,
        AssetType? type = null,
        AssetStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get asset usage statistics
    /// </summary>
    Task<Dictionary<string, object>> GetAssetUsageStatsAsync(
        Guid assetId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization asset statistics
    /// </summary>
    Task<Dictionary<string, object>> GetOrganizationAssetStatsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if asset name is unique within organization
    /// </summary>
    Task<bool> IsNameUniqueAsync(
        string name,
        Guid organizationId,
        Guid? excludeAssetId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets that are about to expire
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetAssetsExpiringAsync(
        DateTime beforeDate,
        Guid? organizationId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update asset status
    /// </summary>
    Task<int> BulkUpdateStatusAsync(
        List<Guid> assetIds,
        AssetStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get assets by parent asset (variations/derivatives)
    /// </summary>
    Task<IEnumerable<MarketingAsset>> GetAssetVariationsAsync(
        Guid parentAssetId,
        CancellationToken cancellationToken = default);
}
