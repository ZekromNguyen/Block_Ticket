using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for PricingRule
/// </summary>
public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        // Table configuration
        builder.ToTable("pricing_rules");
        builder.HasKey(p => p.Id);

        // Basic properties
        builder.Property(p => p.EventId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Effective period
        builder.Property(p => p.EffectiveFrom)
            .IsRequired();

        builder.Property(p => p.EffectiveTo);

        // Discount configuration
        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(10,4)");

        // Quantity-based rules
        builder.Property(p => p.MinQuantity);

        builder.Property(p => p.MaxQuantity);

        // Discount code rules
        builder.Property(p => p.DiscountCode)
            .HasMaxLength(50);

        builder.Property(p => p.IsSingleUse);

        builder.Property(p => p.MaxUses);

        builder.Property(p => p.CurrentUses)
            .IsRequired()
            .HasDefaultValue(0);

        // Enum configurations
        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Value object configurations
        ConfigureMoney(builder);
        ConfigureCollections(builder);

        // Relationships
        ConfigureRelationships(builder);

        // Indexes
        ConfigureIndexes(builder);

        // Constraints
        ConfigureConstraints(builder);
    }

    private static void ConfigureMoney(EntityTypeBuilder<PricingRule> builder)
    {
        // Max discount amount (optional)
        builder.OwnsOne(p => p.MaxDiscountAmount, maxDiscount =>
        {
            maxDiscount.Property(m => m.Amount)
                .HasColumnName("max_discount_amount")
                .HasColumnType("decimal(10,2)");

            maxDiscount.Property(m => m.Currency)
                .HasColumnName("max_discount_currency")
                .HasMaxLength(3);
        });

        // Min order amount (optional)
        builder.OwnsOne(p => p.MinOrderAmount, minOrder =>
        {
            minOrder.Property(m => m.Amount)
                .HasColumnName("min_order_amount")
                .HasColumnType("decimal(10,2)");

            minOrder.Property(m => m.Currency)
                .HasColumnName("min_order_currency")
                .HasMaxLength(3);
        });
    }

    private static void ConfigureCollections(EntityTypeBuilder<PricingRule> builder)
    {
        // Target ticket type IDs as JSON
        builder.Property(p => p.TargetTicketTypeIds)
            .HasConversion(
                v => v != null && v.Any() ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb")
            .HasColumnName("target_ticket_type_ids");

        // Target customer segments as JSON
        builder.Property(p => p.TargetCustomerSegments)
            .HasConversion(
                v => v != null && v.Any() ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => !string.IsNullOrEmpty(v) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb")
            .HasColumnName("target_customer_segments");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<PricingRule> builder)
    {
        // Foreign key to Event (no navigation property configured here)
        builder.HasIndex(p => p.EventId)
            .HasDatabaseName("IX_PricingRules_EventId");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<PricingRule> builder)
    {
        // Performance indexes for rule evaluation
        builder.HasIndex(p => new { p.EventId, p.IsActive, p.EffectiveFrom, p.EffectiveTo })
            .HasDatabaseName("IX_PricingRules_Event_Active_Effective");

        builder.HasIndex(p => new { p.EventId, p.Type, p.IsActive })
            .HasDatabaseName("IX_PricingRules_Event_Type_Active");

        builder.HasIndex(p => new { p.EventId, p.Priority, p.IsActive })
            .HasDatabaseName("IX_PricingRules_Event_Priority_Active");

        // Index for discount code lookups
        builder.HasIndex(p => new { p.DiscountCode, p.IsActive })
            .HasDatabaseName("IX_PricingRules_DiscountCode_Active")
            .HasFilter("\"DiscountCode\" IS NOT NULL");

        // Index for time-based rules
        builder.HasIndex(p => new { p.Type, p.EffectiveFrom, p.EffectiveTo })
            .HasDatabaseName("IX_PricingRules_Type_Effective")
            .HasFilter("\"Type\" = 'TimeBased'");

        // Index for usage tracking
        builder.HasIndex(p => new { p.MaxUses, p.CurrentUses })
            .HasDatabaseName("IX_PricingRules_Usage")
            .HasFilter("\"MaxUses\" IS NOT NULL");
    }

    private static void ConfigureConstraints(EntityTypeBuilder<PricingRule> builder)
    {
        // Check constraints for business rules
        builder.HasCheckConstraint("CK_PricingRules_Type_Valid",
            "\"Type\" IN ('BasePrice', 'TimeBased', 'QuantityBased', 'DiscountCode', 'Dynamic')");

        builder.HasCheckConstraint("CK_PricingRules_DiscountType_Valid",
            "\"DiscountType\" IS NULL OR \"DiscountType\" IN ('FixedAmount', 'Percentage')");

        builder.HasCheckConstraint("CK_PricingRules_EffectivePeriod_Valid",
            "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");

        builder.HasCheckConstraint("CK_PricingRules_DiscountValue_Valid",
            "\"DiscountValue\" IS NULL OR \"DiscountValue\" >= 0");

        builder.HasCheckConstraint("CK_PricingRules_Percentage_Valid",
            "\"DiscountType\" != 'Percentage' OR (\"DiscountValue\" IS NOT NULL AND \"DiscountValue\" <= 100)");

        builder.HasCheckConstraint("CK_PricingRules_Quantity_Valid",
            "(\"MinQuantity\" IS NULL AND \"MaxQuantity\" IS NULL) OR " +
            "(\"MinQuantity\" IS NOT NULL AND \"MaxQuantity\" IS NOT NULL AND \"MinQuantity\" <= \"MaxQuantity\" AND \"MinQuantity\" > 0)");

        builder.HasCheckConstraint("CK_PricingRules_Usage_Valid",
            "\"CurrentUses\" >= 0 AND (\"MaxUses\" IS NULL OR \"CurrentUses\" <= \"MaxUses\")");

        builder.HasCheckConstraint("CK_PricingRules_DiscountCode_Consistency",
            "(\"Type\" = 'DiscountCode' AND \"DiscountCode\" IS NOT NULL) OR " +
            "(\"Type\" != 'DiscountCode' AND \"DiscountCode\" IS NULL)");

        builder.HasCheckConstraint("CK_PricingRules_MoneyAmounts_Positive",
            "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND " +
            "(min_order_amount IS NULL OR min_order_amount >= 0)");

        builder.HasCheckConstraint("CK_PricingRules_Priority_Valid",
            "\"Priority\" >= 0");
    }
}
