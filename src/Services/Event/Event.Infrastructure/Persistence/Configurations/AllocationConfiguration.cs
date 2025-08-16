using Event.Domain.Entities;
using Event.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Allocation
/// </summary>
public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        // Table configuration
        builder.ToTable("allocations");
        builder.HasKey(a => a.Id);

        // Basic properties
        builder.Property(a => a.EventId)
            .IsRequired();

        builder.Property(a => a.TicketTypeId);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.TotalQuantity)
            .IsRequired();

        // Ignore computed property
        builder.Ignore(a => a.Quantity);

        builder.Property(a => a.AllocatedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        // Access control
        builder.Property(a => a.AccessCode)
            .HasMaxLength(50);

        // Timing properties
        builder.Property(a => a.AvailableFrom);

        builder.Property(a => a.AvailableUntil);

        builder.Property(a => a.ExpiresAt);

        // Status
        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Enum configuration
        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Collections configuration
        ConfigureCollections(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureCollections(EntityTypeBuilder<Allocation> builder)
    {
        // Allowed user IDs as JSON
        builder.Property(a => a.AllowedUserIds)
            .HasConversion(
                v => v != null && v.Any() ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb")
            .HasColumnName("allowed_user_ids");

        // Allowed email domains as JSON
        builder.Property(a => a.AllowedEmailDomains)
            .HasConversion(
                v => v != null && v.Any() ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb")
            .HasColumnName("allowed_email_domains");

        // Allocated seat IDs as JSON
        builder.Property(a => a.AllocatedSeatIds)
            .HasConversion(
                v => v != null && v.Any() ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>() : new List<Guid>())
            .HasColumnType("jsonb")
            .HasColumnName("allocated_seat_ids");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Allocation> builder)
    {
        // Many-to-one with Event
        builder.HasOne(a => a.Event)
            .WithMany(e => e.Allocations)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional many-to-one with TicketType
        builder.HasOne(a => a.TicketType)
            .WithMany()
            .HasForeignKey(a => a.TicketTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Allocation> builder)
    {
        // Performance indexes for common queries
        builder.HasIndex(a => new { a.EventId, a.Type, a.IsActive })
            .HasDatabaseName("IX_Allocations_Event_Type_Active");

        builder.HasIndex(a => new { a.EventId, a.IsActive, a.AvailableFrom, a.AvailableUntil })
            .HasDatabaseName("IX_Allocations_Event_Active_Available");

        builder.HasIndex(a => a.TicketTypeId)
            .HasDatabaseName("IX_Allocations_TicketTypeId")
            .HasFilter("\"TicketTypeId\" IS NOT NULL");

        // Index for access code lookups
        builder.HasIndex(a => new { a.AccessCode, a.IsActive })
            .HasDatabaseName("IX_Allocations_AccessCode_Active")
            .HasFilter("\"AccessCode\" IS NOT NULL");

        // Index for expired allocations cleanup
        builder.HasIndex(a => new { a.IsActive, a.ExpiresAt })
            .HasDatabaseName("IX_Allocations_Active_Expires")
            .HasFilter("\"ExpiresAt\" IS NOT NULL");

        // Index for availability window queries
        builder.HasIndex(a => new { a.AvailableFrom, a.AvailableUntil })
            .HasDatabaseName("IX_Allocations_AvailabilityWindow")
            .HasFilter("\"AvailableFrom\" IS NOT NULL OR \"AvailableUntil\" IS NOT NULL");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<Allocation> builder)
    {
        // Check constraints for business rules
        builder.HasCheckConstraint("CK_Allocations_Type_Valid",
            "\"Type\" IN ('Public', 'PromoterHold', 'ArtistHold', 'Presale', 'VIP', 'Press')");

        builder.HasCheckConstraint("CK_Allocations_Quantity_Valid",
            "\"TotalQuantity\" > 0 AND \"AllocatedQuantity\" >= 0 AND \"AllocatedQuantity\" <= \"TotalQuantity\"");

        builder.HasCheckConstraint("CK_Allocations_AvailabilityWindow_Valid",
            "\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" < \"AvailableUntil\"");

        builder.HasCheckConstraint("CK_Allocations_ExpiresAt_Valid",
            "\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > \"CreatedAt\"");

        builder.HasCheckConstraint("CK_Allocations_AccessCode_Consistency",
            "(\"Type\" IN ('Presale', 'VIP') AND \"AccessCode\" IS NOT NULL) OR " +
            "(\"Type\" NOT IN ('Presale', 'VIP'))");

        // Computed property constraint for IsExpired
        builder.HasCheckConstraint("CK_Allocations_NotExpiredWhenActive",
            "NOT \"IsActive\" OR \"ExpiresAt\" IS NULL OR \"ExpiresAt\" > NOW()");
    }
}
