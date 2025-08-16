using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Seat
/// </summary>
public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        // Table configuration
        builder.ToTable("seats");
        builder.HasKey(s => s.Id);

        // Basic properties
        builder.Property(s => s.VenueId)
            .IsRequired();

        // Seat attributes
        builder.Property(s => s.IsAccessible)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.HasRestrictedView)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.PriceCategory)
            .HasMaxLength(50);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        // Current allocation properties
        builder.Property(s => s.CurrentReservationId);

        builder.Property(s => s.ReservedUntil);

        builder.Property(s => s.AllocatedToTicketTypeId);

        // Enum configuration
        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SeatStatus.Available);

        // Value object configuration
        ConfigureSeatPosition(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureSeatPosition(EntityTypeBuilder<Seat> builder)
    {
        builder.OwnsOne(s => s.Position, position =>
        {
            position.Property(p => p.Section)
                .HasColumnName("section")
                .HasMaxLength(20)
                .IsRequired();

            position.Property(p => p.Row)
                .HasColumnName("row")
                .HasMaxLength(10)
                .IsRequired();

            position.Property(p => p.Number)
                .HasColumnName("seat_number")
                .HasMaxLength(10)
                .IsRequired();
        });
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Seat> builder)
    {
        // Many-to-one with Venue
        builder.HasOne(s => s.Venue)
            .WithMany(v => v.Seats)
            .HasForeignKey(s => s.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional relationship with current reservation
        builder.HasIndex(s => s.CurrentReservationId)
            .HasDatabaseName("IX_Seats_CurrentReservationId")
            .HasFilter("\"CurrentReservationId\" IS NOT NULL");

        // Optional relationship with allocated ticket type
        builder.HasIndex(s => s.AllocatedToTicketTypeId)
            .HasDatabaseName("IX_Seats_AllocatedToTicketTypeId")
            .HasFilter("\"AllocatedToTicketTypeId\" IS NOT NULL");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Seat> builder)
    {
        // Note: Unique constraint for seat position will be handled at database level
        // EF Core has limitations with value object properties in composite indexes

        // Performance indexes for availability queries
        builder.HasIndex(s => new { s.VenueId, s.Status })
            .HasDatabaseName("IX_Seats_Venue_Status");

        builder.HasIndex(s => new { s.VenueId, s.Status, s.IsAccessible })
            .HasDatabaseName("IX_Seats_Venue_Status_Accessible");

        builder.HasIndex(s => new { s.VenueId, s.PriceCategory })
            .HasDatabaseName("IX_Seats_Venue_PriceCategory")
            .HasFilter("\"PriceCategory\" IS NOT NULL");

        // Index for expired reservations cleanup
        builder.HasIndex(s => new { s.Status, s.ReservedUntil })
            .HasDatabaseName("IX_Seats_Status_ReservedUntil")
            .HasFilter("\"ReservedUntil\" IS NOT NULL");

        // Note: Section-based queries will use other available indexes

        // Index for ticket type allocation queries
        builder.HasIndex(s => new { s.AllocatedToTicketTypeId, s.Status })
            .HasDatabaseName("IX_Seats_TicketType_Status")
            .HasFilter("\"AllocatedToTicketTypeId\" IS NOT NULL");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<Seat> builder)
    {
        // Check constraints for business rules - temporarily removed to fix column name issues
        // TODO: Re-add check constraints with correct column names
    }
}
