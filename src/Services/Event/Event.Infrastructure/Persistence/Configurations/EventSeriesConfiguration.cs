using Event.Domain.Entities;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for EventSeries
/// </summary>
public class EventSeriesConfiguration : IEntityTypeConfiguration<EventSeries>
{
    public void Configure(EntityTypeBuilder<EventSeries> builder)
    {
        // Table configuration
        builder.ToTable("event_series");
        builder.HasKey(es => es.Id);

        // Basic properties
        builder.Property(es => es.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(es => es.Description)
            .HasMaxLength(2000);

        builder.Property(es => es.OrganizationId)
            .IsRequired();

        builder.Property(es => es.PromoterId)
            .IsRequired();

        // Series metadata
        builder.Property(es => es.SeriesStartDate);

        builder.Property(es => es.SeriesEndDate);

        builder.Property(es => es.MaxEvents);

        builder.Property(es => es.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Marketing properties
        builder.Property(es => es.ImageUrl)
            .HasMaxLength(500);

        builder.Property(es => es.BannerUrl)
            .HasMaxLength(500);

        builder.Property(es => es.SeoTitle)
            .HasMaxLength(100);

        builder.Property(es => es.SeoDescription)
            .HasMaxLength(300);

        // Versioning
        builder.Property(es => es.Version)
            .IsRequired()
            .HasDefaultValue(1);

        // Value object configurations
        ConfigureSlug(builder);
        ConfigureCollections(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureSlug(EntityTypeBuilder<EventSeries> builder)
    {
        builder.Property(es => es.Slug)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value))
            .HasColumnName("slug");
    }

    private static void ConfigureCollections(EntityTypeBuilder<EventSeries> builder)
    {
        // Event IDs as JSON
        builder.Property(es => es.EventIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb")
            .HasColumnName("event_ids");

        // Categories as JSON
        builder.Property(es => es.Categories)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("categories");

        // Tags as JSON
        builder.Property(es => es.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("tags");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EventSeries> builder)
    {
        // Unique slug per organization
        builder.HasIndex(es => new { es.OrganizationId, es.Slug })
            .IsUnique()
            .HasDatabaseName("IX_EventSeries_Organization_Slug");

        // Performance indexes for common queries
        builder.HasIndex(es => new { es.OrganizationId, es.IsActive })
            .HasDatabaseName("IX_EventSeries_Organization_Active");

        builder.HasIndex(es => es.PromoterId)
            .HasDatabaseName("IX_EventSeries_PromoterId");

        builder.HasIndex(es => new { es.IsActive, es.SeriesStartDate, es.SeriesEndDate })
            .HasDatabaseName("IX_EventSeries_Active_Dates");

        // Index for series with available slots
        builder.HasIndex(es => new { es.IsActive, es.MaxEvents })
            .HasDatabaseName("IX_EventSeries_Active_MaxEvents")
            .HasFilter("\"MaxEvents\" IS NOT NULL");

        // Index for name-based searches
        builder.HasIndex(es => es.Name)
            .HasDatabaseName("IX_EventSeries_Name");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<EventSeries> builder)
    {
        // Check constraints for business rules
        builder.HasCheckConstraint("CK_EventSeries_SeriesDates_Valid",
            "\"SeriesStartDate\" IS NULL OR \"SeriesEndDate\" IS NULL OR \"SeriesStartDate\" < \"SeriesEndDate\"");

        builder.HasCheckConstraint("CK_EventSeries_MaxEvents_Positive",
            "\"MaxEvents\" IS NULL OR \"MaxEvents\" > 0");

        builder.HasCheckConstraint("CK_EventSeries_Version_Positive",
            "\"Version\" > 0");

        // Ensure series dates are reasonable
        builder.HasCheckConstraint("CK_EventSeries_SeriesStartDate_Valid",
            "\"SeriesStartDate\" IS NULL OR \"SeriesStartDate\" >= \"CreatedAt\"");
    }
}
