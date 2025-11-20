using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Asset category entity - represents a hierarchical category for organizing marketing assets
/// </summary>
public class AssetCategory : BaseAuditableEntity
{
    private readonly List<AssetCategory> _children = new();
    private readonly List<MarketingAsset> _assets = new();

    // Basic Properties
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public Guid OrganizationId { get; private set; }
    
    // Hierarchy
    public Guid? ParentCategoryId { get; private set; }
    public int Level { get; private set; }
    public string Path { get; private set; } = string.Empty; // e.g., "/events/banners/social"
    
    // Display Properties
    public string? IconUrl { get; private set; }
    public string? Color { get; private set; } // Hex color for UI
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation Properties
    public AssetCategory? ParentCategory { get; private set; }
    public IReadOnlyCollection<AssetCategory> Children => _children.AsReadOnly();
    public IReadOnlyCollection<MarketingAsset> Assets => _assets.AsReadOnly();

    // Private constructor for EF Core
    private AssetCategory() { }

    public AssetCategory(
        string name,
        string description,
        Guid organizationId,
        Guid? parentCategoryId = null,
        string? iconUrl = null,
        string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Category name cannot be empty");

        if (organizationId == Guid.Empty)
            throw new EventDomainException("Organization ID cannot be empty");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Slug = new Slug(name);
        OrganizationId = organizationId;
        ParentCategoryId = parentCategoryId;
        IconUrl = iconUrl?.Trim();
        Color = ValidateColor(color);
        Level = 0; // Will be calculated when parent is set
        Path = "/"; // Will be calculated when parent is set
        SortOrder = 0;
        IsActive = true;
    }

    public void UpdateBasicInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Category name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Slug = new Slug(name);
    }

    public void SetParent(AssetCategory? parentCategory)
    {
        if (parentCategory?.Id == Id)
            throw new EventDomainException("Category cannot be its own parent");

        // Check for circular reference
        if (parentCategory != null && WouldCreateCircularReference(parentCategory))
            throw new EventDomainException("Setting this parent would create a circular reference");

        ParentCategoryId = parentCategory?.Id;
        ParentCategory = parentCategory;
        
        // Recalculate level and path
        RecalculateHierarchy();
    }

    public void SetDisplayProperties(string? iconUrl, string? color, int sortOrder)
    {
        IconUrl = iconUrl?.Trim();
        Color = ValidateColor(color);
        SortOrder = sortOrder;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        
        // Deactivate all children
        foreach (var child in _children)
        {
            child.Deactivate();
        }
    }

    public void AddChild(AssetCategory childCategory)
    {
        if (childCategory == null)
            throw new EventDomainException("Child category cannot be null");

        if (childCategory.Id == Id)
            throw new EventDomainException("Category cannot be its own child");

        if (!_children.Contains(childCategory))
        {
            _children.Add(childCategory);
            childCategory.SetParent(this);
        }
    }

    public void RemoveChild(AssetCategory childCategory)
    {
        if (childCategory != null)
        {
            _children.Remove(childCategory);
            childCategory.SetParent(null);
        }
    }

    public bool HasChildren() => _children.Any();
    
    public bool HasAssets() => _assets.Any();
    
    public int GetTotalAssetCount()
    {
        return _assets.Count + _children.Sum(c => c.GetTotalAssetCount());
    }

    public List<AssetCategory> GetAllDescendants()
    {
        var descendants = new List<AssetCategory>();
        
        foreach (var child in _children)
        {
            descendants.Add(child);
            descendants.AddRange(child.GetAllDescendants());
        }
        
        return descendants;
    }

    public List<AssetCategory> GetPathToRoot()
    {
        var path = new List<AssetCategory> { this };
        var current = ParentCategory;
        
        while (current != null)
        {
            path.Insert(0, current);
            current = current.ParentCategory;
        }
        
        return path;
    }

    private void RecalculateHierarchy()
    {
        if (ParentCategory == null)
        {
            Level = 0;
            Path = $"/{Slug.Value}";
        }
        else
        {
            Level = ParentCategory.Level + 1;
            Path = $"{ParentCategory.Path}/{Slug.Value}";
        }

        // Recalculate for all children
        foreach (var child in _children)
        {
            child.RecalculateHierarchy();
        }
    }

    private bool WouldCreateCircularReference(AssetCategory potentialParent)
    {
        var current = potentialParent;
        while (current != null)
        {
            if (current.Id == Id)
                return true;
            current = current.ParentCategory;
        }
        return false;
    }

    private static string? ValidateColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        var trimmedColor = color.Trim();
        
        // Validate hex color format
        if (trimmedColor.StartsWith("#") && (trimmedColor.Length == 7 || trimmedColor.Length == 4))
        {
            var hexPart = trimmedColor[1..];
            if (hexPart.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                return trimmedColor.ToUpperInvariant();
            }
        }

        throw new EventDomainException($"Invalid color format: {color}. Use hex format like #FF0000");
    }
}
