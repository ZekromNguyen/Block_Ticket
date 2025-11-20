using Event.Domain.Entities;
using Event.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for CampaignVariant
/// </summary>
public class CampaignVariantConfiguration : IEntityTypeConfiguration<CampaignVariant>
{
    public void Configure(EntityTypeBuilder<CampaignVariant> builder)
    {
        // Table configuration
        builder.ToTable("campaign_variants");
        builder.HasKey(v => v.Id);

        // Basic properties
        builder.Property(v => v.CampaignId)
            .IsRequired();

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.TrafficPercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)"); // e.g., 50.25%

        builder.Property(v => v.IsControl)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.IsWinner)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.PrimaryAssetId)
            .IsRequired();

        // Performance tracking properties
        builder.Property(v => v.TotalImpressions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.TotalClicks)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.TotalConversions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.TotalConversionValue)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("decimal(18,2)");

        // Statistical properties
        builder.Property(v => v.ConfidenceLevel)
            .HasColumnType("decimal(5,2)");

        builder.Property(v => v.StatisticalSignificance)
            .HasColumnType("decimal(5,2)");

        // Enum configuration
        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Collection properties stored as JSON
        builder.Property<List<Guid>>("_assetIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnName("asset_ids")
            .HasColumnType("jsonb");

        builder.Property<Dictionary<string, double>>("_metrics")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, double>())
            .HasColumnName("metrics")
            .HasColumnType("jsonb");

        // Relationships
        builder.HasOne(v => v.Campaign)
            .WithMany(c => c.Variants)
            .HasForeignKey(v => v.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to MarketingAsset for primary asset
        builder.HasOne<MarketingAsset>()
            .WithMany()
            .HasForeignKey(v => v.PrimaryAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(v => v.CampaignId)
            .HasDatabaseName("ix_campaign_variants_campaign_id");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("ix_campaign_variants_status");

        builder.HasIndex(v => v.PrimaryAssetId)
            .HasDatabaseName("ix_campaign_variants_primary_asset");

        builder.HasIndex(v => v.IsControl)
            .HasDatabaseName("ix_campaign_variants_control");

        builder.HasIndex(v => v.IsWinner)
            .HasDatabaseName("ix_campaign_variants_winner");

        builder.HasIndex(v => v.TrafficPercentage)
            .HasDatabaseName("ix_campaign_variants_traffic");

        // Performance metrics indexes
        builder.HasIndex(v => v.TotalImpressions)
            .HasDatabaseName("ix_campaign_variants_impressions");

        builder.HasIndex(v => v.TotalClicks)
            .HasDatabaseName("ix_campaign_variants_clicks");

        builder.HasIndex(v => v.TotalConversions)
            .HasDatabaseName("ix_campaign_variants_conversions");

        builder.HasIndex(v => v.TotalConversionValue)
            .HasDatabaseName("ix_campaign_variants_conversion_value");

        // Statistical analysis indexes
        builder.HasIndex(v => v.ConfidenceLevel)
            .HasDatabaseName("ix_campaign_variants_confidence");

        builder.HasIndex(v => v.StatisticalSignificance)
            .HasDatabaseName("ix_campaign_variants_significance");

        // Composite indexes for common queries
        builder.HasIndex(v => new { v.CampaignId, v.Status })
            .HasDatabaseName("ix_campaign_variants_campaign_status");

        builder.HasIndex(v => new { v.CampaignId, v.IsWinner })
            .HasDatabaseName("ix_campaign_variants_campaign_winner");

        builder.HasIndex(v => new { v.CampaignId, v.TrafficPercentage })
            .HasDatabaseName("ix_campaign_variants_campaign_traffic");

        // Full-text search index on name and description
        builder.HasIndex(v => new { v.Name, v.Description })
            .HasDatabaseName("ix_campaign_variants_search");

        // Unique constraint for variant name within campaign
        builder.HasIndex(v => new { v.CampaignId, v.Name })
            .IsUnique()
            .HasDatabaseName("ix_campaign_variants_unique_name");

        // Constraint to ensure only one control variant per campaign
        builder.HasIndex(v => new { v.CampaignId, v.IsControl })
            .IsUnique()
            .HasFilter("\"IsControl\" = true")
            .HasDatabaseName("ix_campaign_variants_unique_control");

        // Constraint to ensure only one winner per campaign
        builder.HasIndex(v => new { v.CampaignId, v.IsWinner })
            .IsUnique()
            .HasFilter("\"IsWinner\" = true")
            .HasDatabaseName("ix_campaign_variants_unique_winner");
    }
}
