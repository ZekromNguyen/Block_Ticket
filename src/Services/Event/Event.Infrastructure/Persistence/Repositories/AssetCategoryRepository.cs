using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AssetCategory
/// </summary>
public class AssetCategoryRepository : OrganizationAwareRepository<AssetCategory>, IAssetCategoryRepository
{
    public AssetCategoryRepository(
        EventDbContext context,
        IOrganizationContextProvider organizationContextProvider,
        ILogger<AssetCategoryRepository> logger)
        : base(context, organizationContextProvider, logger)
    {
    }

    public async Task<IEnumerable<AssetCategory>> GetByOrganizationAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .Include(c => c.Children)
            .OrderBy(c => c.Level)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetCategory>> GetRootCategoriesAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.ParentCategoryId == null);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetCategory>> GetChildCategoriesAsync(
        Guid parentCategoryId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.ParentCategoryId == parentCategoryId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetCategory>> GetCategoryTreeAsync(
        Guid organizationId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var allCategories = await query
            .Include(c => c.Children)
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Level)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        // Return only root categories with their children populated
        return allCategories.Where(c => c.ParentCategoryId == null);
    }

    public async Task<AssetCategory?> GetBySlugAsync(
        string slug,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Children)
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Slug.Value == slug, cancellationToken);
    }

    public async Task<List<AssetCategory>> GetCategoryPathAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await DbSet
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category == null)
            return new List<AssetCategory>();

        var path = new List<AssetCategory> { category };
        var current = category.ParentCategory;

        while (current != null)
        {
            path.Insert(0, current);
            current = await DbSet
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == current.ParentCategoryId, cancellationToken);
        }

        return path;
    }

    public async Task<IEnumerable<AssetCategory>> GetDescendantCategoriesAsync(
        Guid parentCategoryId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var parent = await DbSet
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == parentCategoryId, cancellationToken);

        if (parent == null)
            return Enumerable.Empty<AssetCategory>();

        var descendants = new List<AssetCategory>();
        await CollectDescendants(parent, descendants, includeInactive, cancellationToken);

        return descendants;
    }

    private async Task CollectDescendants(
        AssetCategory parent,
        List<AssetCategory> descendants,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var children = await GetChildCategoriesAsync(parent.Id, includeInactive, cancellationToken);

        foreach (var child in children)
        {
            descendants.Add(child);
            await CollectDescendants(child, descendants, includeInactive, cancellationToken);
        }
    }

    public async Task<bool> IsSlugUniqueAsync(
        string slug,
        Guid organizationId,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.Slug.Value == slug);

        if (excludeCategoryId.HasValue)
            query = query.Where(c => c.Id != excludeCategoryId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsNameUniqueInParentAsync(
        string name,
        Guid organizationId,
        Guid? parentCategoryId = null,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && 
                                   c.Name == name && 
                                   c.ParentCategoryId == parentCategoryId);

        if (excludeCategoryId.HasValue)
            query = query.Where(c => c.Id != excludeCategoryId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetCategoryAssetCountsAsync(
        Guid organizationId,
        bool includeDescendants = true,
        CancellationToken cancellationToken = default)
    {
        var categories = await GetByOrganizationAsync(organizationId, false, cancellationToken);
        var assetCounts = new Dictionary<Guid, int>();

        foreach (var category in categories)
        {
            var count = await Context.Set<MarketingAsset>()
                .CountAsync(a => a.CategoryId == category.Id, cancellationToken);

            if (includeDescendants)
            {
                var descendants = await GetDescendantCategoriesAsync(category.Id, false, cancellationToken);
                foreach (var descendant in descendants)
                {
                    count += await Context.Set<MarketingAsset>()
                        .CountAsync(a => a.CategoryId == descendant.Id, cancellationToken);
                }
            }

            assetCounts[category.Id] = count;
        }

        return assetCounts;
    }

    public async Task<IEnumerable<AssetCategory>> GetEmptyCategoriesAsync(
        Guid organizationId,
        bool includeDescendants = true,
        CancellationToken cancellationToken = default)
    {
        var assetCounts = await GetCategoryAssetCountsAsync(organizationId, includeDescendants, cancellationToken);
        var emptyCategoryIds = assetCounts.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key);

        return await DbSet
            .Where(c => c.OrganizationId == organizationId && emptyCategoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetCategory>> SearchCategoriesAsync(
        string searchTerm,
        Guid organizationId,
        bool includeInactive = false,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) || 
                                   c.Description.ToLower().Contains(searchLower));
        }

        return await query
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.Level)
            .ThenBy(c => c.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AssetCategory>> GetCategoriesOrderedAsync(
        Guid organizationId,
        Guid? parentCategoryId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.ParentCategoryId == parentCategoryId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateSortOrdersAsync(
        Dictionary<Guid, int> categoryOrders,
        CancellationToken cancellationToken = default)
    {
        var categoryIds = categoryOrders.Keys.ToList();
        var categories = await DbSet
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var category in categories)
        {
            if (categoryOrders.TryGetValue(category.Id, out var newSortOrder))
            {
                category.SetDisplayProperties(category.IconUrl, category.Color, newSortOrder);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveCategoryAsync(
        Guid categoryId,
        Guid? newParentId,
        CancellationToken cancellationToken = default)
    {
        var category = await DbSet
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category == null)
            return;

        AssetCategory? newParent = null;
        if (newParentId.HasValue)
        {
            newParent = await GetByIdAsync(newParentId.Value, cancellationToken);
        }

        category.SetParent(newParent);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetMaxSortOrderAsync(
        Guid organizationId,
        Guid? parentCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.OrganizationId == organizationId && c.ParentCategoryId == parentCategoryId);

        if (!await query.AnyAsync(cancellationToken))
            return 0;

        return await query.MaxAsync(c => c.SortOrder, cancellationToken);
    }
}
