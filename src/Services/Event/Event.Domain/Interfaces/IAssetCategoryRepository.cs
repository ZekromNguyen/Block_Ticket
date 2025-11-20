using Event.Domain.Entities;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for asset categories
/// </summary>
public interface IAssetCategoryRepository : IRepository<AssetCategory>
{
    /// <summary>
    /// Get categories by organization with hierarchy
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetByOrganizationAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get root categories (no parent) for organization
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetRootCategoriesAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get child categories of a parent category
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetChildCategoriesAsync(
        Guid parentCategoryId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category hierarchy tree for organization
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetCategoryTreeAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category by slug within organization
    /// </summary>
    Task<AssetCategory?> GetBySlugAsync(
        string slug,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category path from root to specified category
    /// </summary>
    Task<List<AssetCategory>> GetCategoryPathAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all descendant categories of a parent category
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetDescendantCategoriesAsync(
        Guid parentCategoryId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if category slug is unique within organization
    /// </summary>
    Task<bool> IsSlugUniqueAsync(
        string slug,
        Guid organizationId,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if category name is unique within parent category
    /// </summary>
    Task<bool> IsNameUniqueInParentAsync(
        string name,
        Guid organizationId,
        Guid? parentCategoryId = null,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get categories with asset counts
    /// </summary>
    Task<Dictionary<Guid, int>> GetCategoryAssetCountsAsync(
        Guid organizationId,
        bool includeDescendants = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get empty categories (no assets)
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetEmptyCategoriesAsync(
        Guid organizationId,
        bool includeDescendants = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search categories by name or description
    /// </summary>
    Task<IEnumerable<AssetCategory>> SearchCategoriesAsync(
        string searchTerm,
        Guid organizationId,
        bool includeInactive = false,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get categories ordered by sort order
    /// </summary>
    Task<IEnumerable<AssetCategory>> GetCategoriesOrderedAsync(
        Guid organizationId,
        Guid? parentCategoryId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update category sort orders
    /// </summary>
    Task UpdateSortOrdersAsync(
        Dictionary<Guid, int> categoryOrders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move category to new parent
    /// </summary>
    Task MoveCategoryAsync(
        Guid categoryId,
        Guid? newParentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get maximum sort order for categories in parent
    /// </summary>
    Task<int> GetMaxSortOrderAsync(
        Guid organizationId,
        Guid? parentCategoryId = null,
        CancellationToken cancellationToken = default);
}
