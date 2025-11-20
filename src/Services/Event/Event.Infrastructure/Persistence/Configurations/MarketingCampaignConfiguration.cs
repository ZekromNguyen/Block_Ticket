using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for MarketingCampaign
/// </summary>
public class MarketingCampaignConfiguration : IEntityTypeConfiguration<MarketingCampaign>
{
    public void Configure(EntityTypeBuilder<MarketingCampaign> builder)
    {
        // Table configuration
        builder.ToTable("marketing_campaigns");
        builder.HasKey(c => c.Id);

        // Basic properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.OrganizationId)
            .IsRequired();

        builder.Property(c => c.StartDate)
            .IsRequired();

        builder.Property(c => c.EndDate);

        builder.Property(c => c.IsABTest)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.ConfidenceThreshold)
            .HasDefaultValue(95.0);

        // Performance tracking properties
        builder.Property(c => c.TotalImpressions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalClicks)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalConversions)
            .IsRequired()
            .HasDefaultValue(0);

        // A/B Testing properties
        builder.Property(c => c.WinningVariantId);

        builder.Property(c => c.TestCompletedAt);

        builder.Property(c => c.StatisticalSignificance);

        // Budget properties
        builder.Property(c => c.Budget)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.TotalSpent)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        // Enum configurations
        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.PrimaryContext)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Value object configuration for TimeZone
        builder.OwnsOne(c => c.TimeZone, timeZone =>
        {
            timeZone.Property(t => t.Value)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("time_zone");
        });

        // Collection properties stored as JSON
        builder.Property<List<Guid>>("_targetEventIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnName("target_event_ids")
            .HasColumnType("jsonb");

        builder.Property<List<Guid>>("_targetVenueIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnName("target_venue_ids")
            .HasColumnType("jsonb");

        builder.Property<List<string>>("_targetAudiences")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnName("target_audiences")
            .HasColumnType("jsonb");

        builder.Property<Dictionary<string, double>>("_metrics")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, double>())
            .HasColumnName("metrics")
            .HasColumnType("jsonb");

        // Relationships
        builder.HasMany(c => c.Variants)
            .WithOne(v => v.Campaign)
            .HasForeignKey(v => v.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("ix_marketing_campaigns_organization_id");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_marketing_campaigns_status");

        builder.HasIndex(c => c.PrimaryContext)
            .HasDatabaseName("ix_marketing_campaigns_context");

        builder.HasIndex(c => new { c.OrganizationId, c.Status })
            .HasDatabaseName("ix_marketing_campaigns_org_status");

        builder.HasIndex(c => c.StartDate)
            .HasDatabaseName("ix_marketing_campaigns_start_date");

        builder.HasIndex(c => c.EndDate)
            .HasDatabaseName("ix_marketing_campaigns_end_date");

        builder.HasIndex(c => new { c.StartDate, c.EndDate })
            .HasDatabaseName("ix_marketing_campaigns_date_range");

        builder.HasIndex(c => c.IsABTest)
            .HasDatabaseName("ix_marketing_campaigns_ab_test");

        builder.HasIndex(c => c.WinningVariantId)
            .HasDatabaseName("ix_marketing_campaigns_winning_variant");

        builder.HasIndex(c => c.TestCompletedAt)
            .HasDatabaseName("ix_marketing_campaigns_test_completed");

        builder.HasIndex(c => c.TotalSpent)
            .HasDatabaseName("ix_marketing_campaigns_total_spent");

        // Performance metrics indexes
        builder.HasIndex(c => c.TotalImpressions)
            .HasDatabaseName("ix_marketing_campaigns_impressions");

        builder.HasIndex(c => c.TotalClicks)
            .HasDatabaseName("ix_marketing_campaigns_clicks");

        builder.HasIndex(c => c.TotalConversions)
            .HasDatabaseName("ix_marketing_campaigns_conversions");

        // Full-text search index on name and description
        builder.HasIndex(c => new { c.Name, c.Description })
            .HasDatabaseName("ix_marketing_campaigns_search");

        // Row-level security for multi-tenancy
        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("ix_marketing_campaigns_rls");

        // Unique constraint for campaign name within organization
        builder.HasIndex(c => new { c.OrganizationId, c.Name })
            .IsUnique()
            .HasDatabaseName("ix_marketing_campaigns_unique_name");
    }
}
