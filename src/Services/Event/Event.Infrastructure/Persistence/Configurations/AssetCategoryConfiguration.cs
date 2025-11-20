using Event.Domain.Entities;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AssetCategory
/// </summary>
public class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        // Table configuration
        builder.ToTable("asset_categories");
        builder.HasKey(c => c.Id);

        // Basic properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.OrganizationId)
            .IsRequired();

        builder.Property(c => c.ParentCategoryId);

        builder.Property(c => c.Level)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.IconUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Color)
            .HasMaxLength(7); // Hex color format #FFFFFF

        builder.Property(c => c.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Value object configuration for Slug
        builder.OwnsOne(c => c.Slug, slug =>
        {
            slug.Property(s => s.Value)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("slug");
        });

        // Self-referencing relationship for hierarchy
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Relationship with MarketingAssets
        builder.HasMany<MarketingAsset>()
            .WithOne()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("ix_asset_categories_organization_id");

        // Note: Unique constraint on slug will be handled at database level if needed

        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("ix_asset_categories_parent_id");

        builder.HasIndex(c => new { c.OrganizationId, c.ParentCategoryId })
            .HasDatabaseName("ix_asset_categories_org_parent");

        builder.HasIndex(c => c.Level)
            .HasDatabaseName("ix_asset_categories_level");

        builder.HasIndex(c => c.Path)
            .HasDatabaseName("ix_asset_categories_path");

        builder.HasIndex(c => new { c.OrganizationId, c.ParentCategoryId, c.SortOrder })
            .HasDatabaseName("ix_asset_categories_org_parent_sort");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_asset_categories_active");

        // Full-text search index on name and description
        builder.HasIndex(c => new { c.Name, c.Description })
            .HasDatabaseName("ix_asset_categories_search");

        // Row-level security for multi-tenancy
        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("ix_asset_categories_rls");

        // Unique constraint for name within parent category
        builder.HasIndex(c => new { c.OrganizationId, c.ParentCategoryId, c.Name })
            .IsUnique()
            .HasDatabaseName("ix_asset_categories_unique_name_in_parent");
    }
}
