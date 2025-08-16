using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Reservation
/// </summary>
public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        // Table configuration
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        // Basic properties
        builder.Property(r => r.EventId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.ReservationNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Timing properties
        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.ConfirmedAt);

        builder.Property(r => r.CancelledAt);

        // Discount properties
        builder.Property(r => r.DiscountCode)
            .HasMaxLength(50);

        // Enum configuration
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Value object configurations
        ConfigureMoney(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureMoney(EntityTypeBuilder<Reservation> builder)
    {
        // Total amount (required)
        builder.OwnsOne(r => r.TotalAmount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            amount.Property(a => a.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Discount amount (optional)
        builder.OwnsOne(r => r.DiscountAmount, discount =>
        {
            discount.Property(d => d.Amount)
                .HasColumnName("discount_amount")
                .HasColumnType("decimal(10,2)");

            discount.Property(d => d.Currency)
                .HasColumnName("discount_currency")
                .HasMaxLength(3);
        });
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Reservation> builder)
    {
        // One-to-many with ReservationItems
        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey("ReservationId")
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Event (no navigation property configured here)
        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("IX_Reservations_EventId");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Reservation> builder)
    {
        // Unique reservation number
        builder.HasIndex(r => r.ReservationNumber)
            .IsUnique()
            .HasDatabaseName("IX_Reservations_Number");

        // Performance indexes for common queries
        builder.HasIndex(r => new { r.EventId, r.Status, r.ExpiresAt })
            .HasDatabaseName("IX_Reservations_Event_Status_Expires");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("IX_Reservations_UserId");

        builder.HasIndex(r => new { r.UserId, r.Status })
            .HasDatabaseName("IX_Reservations_User_Status");

        // Index for expired reservations cleanup
        builder.HasIndex(r => new { r.Status, r.ExpiresAt })
            .HasDatabaseName("IX_Reservations_Status_Expires")
            .HasFilter("\"Status\" = 'Active'");

        // Index for confirmed reservations
        builder.HasIndex(r => new { r.EventId, r.ConfirmedAt })
            .HasDatabaseName("IX_Reservations_Event_Confirmed")
            .HasFilter("\"ConfirmedAt\" IS NOT NULL");

        // Index for discount code usage tracking
        builder.HasIndex(r => new { r.DiscountCode, r.Status })
            .HasDatabaseName("IX_Reservations_DiscountCode_Status")
            .HasFilter("\"DiscountCode\" IS NOT NULL");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<Reservation> builder)
    {
        // Check constraints for business rules
        builder.HasCheckConstraint("CK_Reservations_Status_Valid",
            "\"Status\" IN ('Active', 'Confirmed', 'Cancelled', 'Expired', 'Released')");

        builder.HasCheckConstraint("CK_Reservations_ExpiresAt_Future",
            "\"ExpiresAt\" > \"CreatedAt\"");

        builder.HasCheckConstraint("CK_Reservations_TotalAmount_Positive",
            "total_amount >= 0");

        builder.HasCheckConstraint("CK_Reservations_DiscountAmount_Positive",
            "discount_amount IS NULL OR discount_amount >= 0");

        builder.HasCheckConstraint("CK_Reservations_Currency_Match",
            "discount_currency IS NULL OR discount_currency = currency");

        builder.HasCheckConstraint("CK_Reservations_Timing_Valid",
            "(\"Status\" = 'Confirmed' AND \"ConfirmedAt\" IS NOT NULL) OR " +
            "(\"Status\" = 'Cancelled' AND \"CancelledAt\" IS NOT NULL) OR " +
            "(\"Status\" IN ('Active', 'Expired', 'Released'))");

        builder.HasCheckConstraint("CK_Reservations_ConfirmedAt_Valid",
            "\"ConfirmedAt\" IS NULL OR \"ConfirmedAt\" >= \"CreatedAt\"");

        builder.HasCheckConstraint("CK_Reservations_CancelledAt_Valid",
            "\"CancelledAt\" IS NULL OR \"CancelledAt\" >= \"CreatedAt\"");
    }
}

/// <summary>
/// Entity Framework configuration for ReservationItem
/// </summary>
public class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        // Table configuration
        builder.ToTable("reservation_items");
        builder.HasKey(ri => ri.Id);

        // Basic properties
        builder.Property(ri => ri.TicketTypeId)
            .IsRequired();

        builder.Property(ri => ri.SeatId);

        builder.Property(ri => ri.Quantity)
            .IsRequired();

        // Value object configuration for unit price
        builder.OwnsOne(ri => ri.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("unit_price_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("unit_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(ri => ri.TicketTypeId)
            .HasDatabaseName("IX_ReservationItems_TicketTypeId");

        builder.HasIndex(ri => ri.SeatId)
            .HasDatabaseName("IX_ReservationItems_SeatId")
            .HasFilter("\"SeatId\" IS NOT NULL");

        // Constraints
        builder.HasCheckConstraint("CK_ReservationItems_Quantity_Positive",
            "\"Quantity\" > 0");

        builder.HasCheckConstraint("CK_ReservationItems_UnitPrice_Positive",
            "unit_price_amount >= 0");
    }
}
