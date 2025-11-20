using Event.Domain.Entities;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Venue
/// </summary>
public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        // Table configuration
        builder.ToTable("venues");
        builder.HasKey(v => v.Id);

        // Basic properties
        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.TotalCapacity)
            .IsRequired();

        // Contact information
        builder.Property(v => v.ContactEmail)
            .HasMaxLength(100);

        builder.Property(v => v.ContactPhone)
            .HasMaxLength(20);

        builder.Property(v => v.Website)
            .HasMaxLength(200);

        // Seat map properties
        builder.Property(v => v.HasSeatMap)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.SeatMapMetadata)
;

        builder.Property(v => v.SeatMapChecksum)
            .HasMaxLength(64);

        builder.Property(v => v.SeatMapLastUpdated);

        // Value object configurations
        ConfigureAddress(builder);
        ConfigureTimeZone(builder);

        // Relationships
        ConfigureRelationships(builder);


    }

    private static void ConfigureAddress(EntityTypeBuilder<Venue> builder)
    {
        builder.OwnsOne(v => v.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("street")
                .HasMaxLength(200)
                .IsRequired();

            address.Property(a => a.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.State)
                .HasColumnName("state")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(20)
                .IsRequired();

            address.Property(a => a.Country)
                .HasColumnName("country")
                .HasMaxLength(100)
                .IsRequired();

            // Nested value object for coordinates
            address.OwnsOne(a => a.Coordinates, coords =>
            {
                coords.Property(c => c.Latitude)
                    .HasColumnName("latitude")
                    .HasColumnType("decimal(10,8)");

                coords.Property(c => c.Longitude)
                    .HasColumnName("longitude")
                    .HasColumnType("decimal(11,8)");
            });
        });
    }

    private static void ConfigureTimeZone(EntityTypeBuilder<Venue> builder)
    {
        builder.Property(v => v.TimeZone)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                tz => tz.Value,
                value => new TimeZoneId(value))
            .HasColumnName("time_zone");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Venue> builder)
    {
        // One-to-many with Seats
        builder.HasMany(v => v.Seats)
            .WithOne(s => s.Venue)
            .HasForeignKey(s => s.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }


}
