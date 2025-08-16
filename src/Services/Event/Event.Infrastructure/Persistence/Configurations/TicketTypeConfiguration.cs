using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for TicketType
/// </summary>
public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        // Table configuration
        builder.ToTable("ticket_types");
        builder.HasKey(t => t.Id);

        // Basic properties
        builder.Property(t => t.EventId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        // Enum configuration
        builder.Property(t => t.InventoryType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Purchase constraints
        builder.Property(t => t.MinPurchaseQuantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(t => t.MaxPurchaseQuantity)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(t => t.MaxPerCustomer)
            .IsRequired()
            .HasDefaultValue(10);

        // Visibility and rules
        builder.Property(t => t.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.IsResaleAllowed)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(false);

        // Value object configurations
        ConfigureMoney(builder);
        ConfigureCapacity(builder);
        ConfigureOnSaleWindows(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureMoney(EntityTypeBuilder<TicketType> builder)
    {
        // Base price (required)
        builder.OwnsOne(t => t.BasePrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("base_price_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("base_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Service fee (optional)
        builder.OwnsOne(t => t.ServiceFee, fee =>
        {
            fee.Property(f => f.Amount)
                .HasColumnName("service_fee_amount")
                .HasColumnType("decimal(10,2)");

            fee.Property(f => f.Currency)
                .HasColumnName("service_fee_currency")
                .HasMaxLength(3);
        });

        // Tax amount (optional)
        builder.OwnsOne(t => t.TaxAmount, tax =>
        {
            tax.Property(t => t.Amount)
                .HasColumnName("tax_amount")
                .HasColumnType("decimal(10,2)");

            tax.Property(t => t.Currency)
                .HasColumnName("tax_currency")
                .HasMaxLength(3);
        });
    }

    private static void ConfigureCapacity(EntityTypeBuilder<TicketType> builder)
    {
        builder.OwnsOne(t => t.Capacity, capacity =>
        {
            capacity.Property(c => c.Total)
                .HasColumnName("total_capacity")
                .IsRequired();

            capacity.Property(c => c.Available)
                .HasColumnName("available_capacity")
                .IsRequired();

            // Computed property for reserved capacity
            capacity.Ignore(c => c.Reserved);
        });
    }

    private static void ConfigureOnSaleWindows(EntityTypeBuilder<TicketType> builder)
    {
        // Store on-sale windows as JSON
        builder.Property(t => t.OnSaleWindows)
            .HasConversion(
                windows => System.Text.Json.JsonSerializer.Serialize(windows, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<DateTimeRange>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<DateTimeRange>())
            .HasColumnType("jsonb")
            .HasColumnName("on_sale_windows");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<TicketType> builder)
    {
        // Many-to-one with Event
        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTypes)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<TicketType> builder)
    {
        // Unique code per event
        builder.HasIndex(t => new { t.EventId, t.Code })
            .IsUnique()
            .HasDatabaseName("IX_TicketTypes_Event_Code");

        // Performance indexes
        builder.HasIndex(t => t.EventId)
            .HasDatabaseName("IX_TicketTypes_EventId");

        builder.HasIndex(t => new { t.EventId, t.IsVisible })
            .HasDatabaseName("IX_TicketTypes_Event_Visible");

        builder.HasIndex(t => new { t.EventId, t.InventoryType })
            .HasDatabaseName("IX_TicketTypes_Event_InventoryType");

        // Partial index for available tickets
        builder.HasIndex(t => new { t.EventId, t.IsVisible })
            .HasFilter("available_capacity > 0 AND \"IsVisible\" = true")
            .HasDatabaseName("IX_TicketTypes_Available");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<TicketType> builder)
    {
        // Check constraints for business rules - temporarily removed to fix column name issues
        // TODO: Re-add check constraints with correct column names
    }
}
