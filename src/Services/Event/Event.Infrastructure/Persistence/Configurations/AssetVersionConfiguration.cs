using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AssetVersion
/// </summary>
public class AssetVersionConfiguration : IEntityTypeConfiguration<AssetVersion>
{
    public void Configure(EntityTypeBuilder<AssetVersion> builder)
    {
        // Table configuration
        builder.ToTable("asset_versions");
        builder.HasKey(v => v.Id);

        // Basic properties
        builder.Property(v => v.AssetId)
            .IsRequired();

        builder.Property(v => v.VersionNumber)
            .IsRequired();

        builder.Property(v => v.ChangeDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.ProcessedAt);

        builder.Property(v => v.ProcessingLog)
            .HasMaxLength(2000);

        builder.Property(v => v.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.LastUsedAt);

        // Value object configurations
        builder.OwnsOne(v => v.FileInfo, fileInfo =>
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

        builder.OwnsOne(v => v.Dimensions, dimensions =>
        {
            dimensions.Property(d => d.Width)
                .HasColumnName("width");

            dimensions.Property(d => d.Height)
                .HasColumnName("height");
        });

        builder.OwnsOne(v => v.StorageInfo, storage =>
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
                .HasColumnName("quality_urls")
                .HasColumnType("jsonb");
        });

        // Relationships
        builder.HasOne(v => v.Asset)
            .WithMany(a => a.Versions)
            .HasForeignKey(v => v.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(v => v.AssetId)
            .HasDatabaseName("ix_asset_versions_asset_id");

        builder.HasIndex(v => new { v.AssetId, v.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ix_asset_versions_asset_version");

        builder.HasIndex(v => v.IsProcessed)
            .HasDatabaseName("ix_asset_versions_processed");

        builder.HasIndex(v => v.ProcessedAt)
            .HasDatabaseName("ix_asset_versions_processed_at");

        builder.HasIndex(v => v.LastUsedAt)
            .HasDatabaseName("ix_asset_versions_last_used");

        builder.HasIndex(v => v.UsageCount)
            .HasDatabaseName("ix_asset_versions_usage_count");

        // Note: Checksum index will be created at database level if needed
    }
}
