using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for EventAggregate
/// </summary>
public class EventAggregateConfiguration : IEntityTypeConfiguration<EventAggregate>
{
    public void Configure(EntityTypeBuilder<EventAggregate> builder)
    {
        // Table configuration
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        // Basic properties
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.OrganizationId)
            .IsRequired();

        builder.Property(e => e.PromoterId)
            .IsRequired();

        builder.Property(e => e.VenueId)
            .IsRequired();

        builder.Property(e => e.EventDate)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired()
            .HasDefaultValue(1);

        // Enum configuration
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Value object configurations
        ConfigureSlug(builder);
        ConfigureTimeZone(builder);
        ConfigureDateTimeRange(builder);

        // Marketing properties
        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.BannerUrl)
            .HasMaxLength(500);

        builder.Property(e => e.SeoTitle)
            .HasMaxLength(100);

        builder.Property(e => e.SeoDescription)
            .HasMaxLength(300);

        // JSON properties
        builder.Property(e => e.ChangeHistory)
            .HasColumnType("jsonb");

        // Collections as JSON
        builder.Property(e => e.Categories)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("categories");

        builder.Property(e => e.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasColumnName("tags");

        // PostgreSQL search vector
        builder.Property(e => e.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(title, '') || ' ' || coalesce(description, ''))", stored: true);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureSlug(EntityTypeBuilder<EventAggregate> builder)
    {
        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                slug => slug.Value,
                value => new Slug(value))
            .HasColumnName("slug");
    }

    private static void ConfigureTimeZone(EntityTypeBuilder<EventAggregate> builder)
    {
        builder.Property(e => e.TimeZone)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                tz => tz.Value,
                value => new TimeZoneId(value))
            .HasColumnName("time_zone");
    }

    private static void ConfigureDateTimeRange(EntityTypeBuilder<EventAggregate> builder)
    {
        builder.OwnsOne(e => e.PublishWindow, pw =>
        {
            pw.Property(p => p.StartDate)
                .HasColumnName("publish_start_date");

            pw.Property(p => p.EndDate)
                .HasColumnName("publish_end_date");

            pw.Property(p => p.TimeZone)
                .HasMaxLength(50)
                .HasConversion(
                    tz => tz.Value,
                    value => new TimeZoneId(value))
                .HasColumnName("publish_time_zone");
        });
    }

    private static void ConfigureRelationships(EntityTypeBuilder<EventAggregate> builder)
    {
        // One-to-many with TicketTypes
        builder.HasMany(e => e.TicketTypes)
            .WithOne(t => t.Event)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many with PricingRules
        builder.HasMany(e => e.PricingRules)
            .WithOne()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many with Allocations
        builder.HasMany(e => e.Allocations)
            .WithOne(a => a.Event)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Venue (no navigation property configured here)
        builder.HasIndex(e => e.VenueId)
            .HasDatabaseName("IX_Events_VenueId");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EventAggregate> builder)
    {
        // Unique slug per organization
        builder.HasIndex(e => new { e.OrganizationId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_Events_Organization_Slug");

        // Performance indexes for common queries
        builder.HasIndex(e => new { e.Status, e.EventDate })
            .HasDatabaseName("IX_Events_Status_EventDate");

        builder.HasIndex(e => new { e.OrganizationId, e.Status, e.EventDate })
            .HasDatabaseName("IX_Events_Organization_Status_EventDate");

        builder.HasIndex(e => e.PromoterId)
            .HasDatabaseName("IX_Events_PromoterId");

        builder.HasIndex(e => e.VenueId)
            .HasDatabaseName("IX_Events_VenueId");

        // Search index
        builder.HasIndex(e => e.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("IX_Events_SearchVector");

        // Partial indexes for active events
        builder.HasIndex(e => new { e.EventDate, e.Status })
            .HasFilter("\"Status\" IN ('Published', 'OnSale')")
            .HasDatabaseName("IX_Events_ActiveEvents");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<EventAggregate> builder)
    {
        // Configure ETag support
        builder.ConfigureETag();
        builder.ConfigureOptimisticConcurrency();
        builder.AddETagConstraints("EventAggregate");

        // Check constraints
        builder.HasCheckConstraint("CK_Events_EventDate_Future",
            "\"EventDate\" > \"CreatedAt\"");

        builder.HasCheckConstraint("CK_Events_PublishWindow_Valid",
            "publish_start_date IS NULL OR publish_end_date IS NULL OR publish_start_date < publish_end_date");

        builder.HasCheckConstraint("CK_Events_Version_Positive",
            "\"Version\" > 0");

        // Status transitions constraint (business rule enforcement)
        builder.HasCheckConstraint("CK_Events_Status_Valid",
            "\"Status\" IN ('Draft', 'Review', 'Published', 'OnSale', 'SoldOut', 'Completed', 'Cancelled', 'Archived')");
    }
}
