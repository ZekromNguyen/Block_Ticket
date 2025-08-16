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
            .HasColumnType("jsonb");

        builder.Property(v => v.SeatMapChecksum)
            .HasMaxLength(64);

        builder.Property(v => v.SeatMapLastUpdated);

        // Value object configurations
        ConfigureAddress(builder);
        ConfigureTimeZone(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
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

    private static void ConfigureIndexes(EntityTypeBuilder<Venue> builder)
    {
        // Performance indexes
        builder.HasIndex(v => v.Name)
            .HasDatabaseName("IX_Venues_Name");

        // Note: Address-based indexes will be handled at database level
        // EF Core has limitations with value object properties in indexes

        // Note: Geospatial indexes will be handled at database level

        // Seat map indexes
        builder.HasIndex(v => v.HasSeatMap)
            .HasDatabaseName("IX_Venues_HasSeatMap");

        builder.HasIndex(v => v.SeatMapLastUpdated)
            .HasFilter("\"SeatMapLastUpdated\" IS NOT NULL")
            .HasDatabaseName("IX_Venues_SeatMapLastUpdated");

        // Capacity index for filtering
        builder.HasIndex(v => v.TotalCapacity)
            .HasDatabaseName("IX_Venues_TotalCapacity");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<Venue> builder)
    {
        // Check constraints for business rules
        builder.HasCheckConstraint("CK_Venues_Capacity_Positive",
            "\"TotalCapacity\" > 0");

        builder.HasCheckConstraint("CK_Venues_Coordinates_Valid",
            "(latitude IS NULL AND longitude IS NULL) OR " +
            "(latitude IS NOT NULL AND longitude IS NOT NULL AND " +
            "latitude >= -90 AND latitude <= 90 AND " +
            "longitude >= -180 AND longitude <= 180)");

        builder.HasCheckConstraint("CK_Venues_Email_Format",
            "\"ContactEmail\" IS NULL OR \"ContactEmail\" ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");

        builder.HasCheckConstraint("CK_Venues_Website_Format",
            "\"Website\" IS NULL OR \"Website\" ~ '^https?://'");

        builder.HasCheckConstraint("CK_Venues_SeatMap_Consistency",
            "(\"HasSeatMap\" = false AND \"SeatMapMetadata\" IS NULL AND \"SeatMapChecksum\" IS NULL AND \"SeatMapLastUpdated\" IS NULL) OR " +
            "(\"HasSeatMap\" = true AND \"SeatMapMetadata\" IS NOT NULL AND \"SeatMapChecksum\" IS NOT NULL AND \"SeatMapLastUpdated\" IS NOT NULL)");
    }
}
