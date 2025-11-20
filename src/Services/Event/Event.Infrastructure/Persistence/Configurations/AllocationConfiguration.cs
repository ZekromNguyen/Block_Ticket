using Event.Domain.Entities;
using Event.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

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

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AllocationScope.ByQuantity);

        // Quantity properties
        builder.Property(a => a.Quantity)
            .IsRequired();

        builder.Property(a => a.UsedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        // Access control
        builder.Property(a => a.AccessCode)
            .HasMaxLength(50);

        builder.Property(a => a.AllowedCustomerSegments)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        // Time windows
        builder.Property(a => a.StartTime);
        builder.Property(a => a.EndTime);

        // Capacity limits
        builder.Property(a => a.MaxPerCustomer);
        builder.Property(a => a.MinPerCustomer);

        // Status and metadata
        builder.Property(a => a.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.Priority)
            .IsRequired()
            .HasDefaultValue(0);


        // Ignored properties
        builder.Ignore(a => a.AllocatedSeats);
        builder.Property(a => a.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Allocation> builder)
    {
        // Many-to-one with Event
        builder.HasOne(a => a.Event)
            .WithMany() // EventAggregate doesn't have Allocations navigation property yet
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one with TicketType (optional)
        builder.HasOne(a => a.TicketType)
            .WithMany() // TicketType doesn't have Allocations navigation property yet
            .HasForeignKey(a => a.TicketTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Allocation> builder)
    {
        // Primary lookup indexes
        builder.HasIndex(a => a.EventId)
            .HasDatabaseName("IX_Allocations_EventId");

        builder.HasIndex(a => a.TicketTypeId)
            .HasDatabaseName("IX_Allocations_TicketTypeId")
            .HasFilter("\"TicketTypeId\" IS NOT NULL");

        builder.HasIndex(a => a.Type)
            .HasDatabaseName("IX_Allocations_Type");

        // Access code index (unique)
        builder.HasIndex(a => a.AccessCode)
            .HasDatabaseName("IX_Allocations_AccessCode")
            .IsUnique()
            .HasFilter("\"AccessCode\" IS NOT NULL");

        // Composite indexes for common queries
        builder.HasIndex(a => new { a.EventId, a.Type })
            .HasDatabaseName("IX_Allocations_Event_Type");

        builder.HasIndex(a => new { a.EventId, a.IsEnabled })
            .HasDatabaseName("IX_Allocations_Event_Enabled");

        builder.HasIndex(a => new { a.EventId, a.StartTime, a.EndTime })
            .HasDatabaseName("IX_Allocations_Event_TimeWindow");

        // Performance index for active allocations
        builder.HasIndex(a => new { a.EventId, a.IsEnabled, a.StartTime, a.EndTime })
            .HasDatabaseName("IX_Allocations_Event_Active")
            .HasFilter("\"IsEnabled\" = true");

        // Priority ordering index
        builder.HasIndex(a => new { a.EventId, a.Priority, a.Name })
            .HasDatabaseName("IX_Allocations_Event_Priority_Name");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<Allocation> builder)
    {
        // Check constraints for business rules
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_Quantity_Positive",
            "\"Quantity\" > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_UsedQuantity_NonNegative",
            "\"UsedQuantity\" >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_UsedQuantity_LessOrEqual_Quantity",
            "\"UsedQuantity\" <= \"Quantity\""));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_MaxPerCustomer_Positive",
            "\"MaxPerCustomer\" IS NULL OR \"MaxPerCustomer\" > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_MinPerCustomer_Positive",
            "\"MinPerCustomer\" IS NULL OR \"MinPerCustomer\" > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_MinMax_PerCustomer",
            "\"MinPerCustomer\" IS NULL OR \"MaxPerCustomer\" IS NULL OR \"MinPerCustomer\" <= \"MaxPerCustomer\""));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_TimeWindow",
            "\"StartTime\" IS NULL OR \"EndTime\" IS NULL OR \"StartTime\" < \"EndTime\""));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Allocations_Name_NotEmpty",
            "LENGTH(TRIM(\"Name\")) > 0"));
    }
}
