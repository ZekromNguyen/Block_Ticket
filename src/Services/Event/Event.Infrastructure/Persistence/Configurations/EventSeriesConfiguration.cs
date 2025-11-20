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


}
