using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for MarketingAsset
/// </summary>
public class MarketingAssetConfiguration : IEntityTypeConfiguration<MarketingAsset>
{
    public void Configure(EntityTypeBuilder<MarketingAsset> builder)
    {
        // Table configuration
        builder.ToTable("marketing_assets");
        builder.HasKey(a => a.Id);

        // Basic properties
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.OrganizationId)
            .IsRequired();

        builder.Property(a => a.CategoryId);

        builder.Property(a => a.CurrentVersion)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(a => a.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.LastUsedAt);

        builder.Property(a => a.ParentAssetId);

        builder.Property(a => a.ApprovalWorkflowId);

        builder.Property(a => a.ApprovedAt);

        builder.Property(a => a.ApprovedBy)
            .HasMaxLength(100);

        builder.Property(a => a.ExpiresAt);

        // Enum configurations
        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Value object configurations
        builder.OwnsOne(a => a.FileInfo, fileInfo =>
        {
            fileInfo.Property(f => f.FileName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("file_name");

            fileInfo.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("content_type");

            fileInfo.Property(f => f.FileSizeBytes)
                .IsRequired()
                .HasColumnName("file_size_bytes");

            fileInfo.Property(f => f.FileExtension)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("file_extension");

            fileInfo.Property(f => f.Checksum)
                .HasMaxLength(64)
                .HasColumnName("file_checksum");
        });

        builder.OwnsOne(a => a.Dimensions, dimensions =>
        {
            dimensions.Property(d => d.Width)
                .HasColumnName("width");

            dimensions.Property(d => d.Height)
                .HasColumnName("height");
        });

        builder.OwnsOne(a => a.StorageInfo, storage =>
        {
            storage.Property(s => s.StorageProvider)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("storage_provider");

            storage.Property(s => s.StoragePath)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("storage_path");

            storage.Property(s => s.CdnUrl)
                .HasMaxLength(500)
                .HasColumnName("cdn_url");

            storage.Property(s => s.ThumbnailUrl)
                .HasMaxLength(500)
                .HasColumnName("thumbnail_url");

            // Store quality URLs as JSON
            storage.Property(s => s.QualityUrls)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<AssetQuality, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<AssetQuality, string>())
                .HasColumnName("quality_urls");
        });

        builder.OwnsOne(a => a.Metadata, metadata =>
        {
            metadata.Property(m => m.AltText)
                .HasMaxLength(500)
                .HasColumnName("alt_text");

            metadata.Property(m => m.Caption)
                .HasMaxLength(1000)
                .HasColumnName("caption");

            metadata.Property(m => m.Copyright)
                .HasMaxLength(200)
                .HasColumnName("copyright");

            metadata.Property(m => m.Attribution)
                .HasMaxLength(200)
                .HasColumnName("attribution");

            // Store properties and keywords as JSON
            metadata.Property(m => m.Properties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                .HasColumnName("metadata_properties");

            metadata.Property(m => m.Keywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnName("keywords");
        });

        builder.OwnsOne(a => a.ComplianceResult, compliance =>
        {
            compliance.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("compliance_status");

            compliance.Property(c => c.ComplianceScore)
                .HasColumnName("compliance_score");

            compliance.Property(c => c.ValidatedAt)
                .HasColumnName("compliance_validated_at");

            compliance.Property(c => c.ValidatedBy)
                .HasMaxLength(100)
                .HasColumnName("compliance_validated_by");

            compliance.Property(c => c.Violations)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnName("compliance_violations");

            compliance.Property(c => c.Warnings)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnName("compliance_warnings");
        });

        // Collection properties stored as JSON
        builder.Property<List<string>>("_tags")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnName("tags");

        builder.Property<List<AssetUsageContext>>("_usageContexts")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<AssetUsageContext>>(v, (JsonSerializerOptions?)null) ?? new List<AssetUsageContext>())
            .HasColumnName("usage_contexts");

        // Relationships
        builder.HasOne<AssetCategory>()
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Versions)
            .WithOne(v => v.Asset)
            .HasForeignKey(v => v.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(a => a.OrganizationId)
            .HasDatabaseName("ix_marketing_assets_organization_id");

        builder.HasIndex(a => a.Type)
            .HasDatabaseName("ix_marketing_assets_type");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("ix_marketing_assets_status");

        builder.HasIndex(a => a.CategoryId)
            .HasDatabaseName("ix_marketing_assets_category_id");

        builder.HasIndex(a => new { a.OrganizationId, a.Type })
            .HasDatabaseName("ix_marketing_assets_org_type");

        builder.HasIndex(a => new { a.OrganizationId, a.Status })
            .HasDatabaseName("ix_marketing_assets_org_status");

        builder.HasIndex(a => a.LastUsedAt)
            .HasDatabaseName("ix_marketing_assets_last_used");

        builder.HasIndex(a => a.UsageCount)
            .HasDatabaseName("ix_marketing_assets_usage_count");

        // Note: Checksum index will be created at database level if needed

        // Full-text search index on name and description
        builder.HasIndex(a => new { a.Name, a.Description })
            .HasDatabaseName("ix_marketing_assets_search");

        // Row-level security for multi-tenancy
        builder.HasIndex(a => a.OrganizationId)
            .HasDatabaseName("ix_marketing_assets_rls");
    }
}
