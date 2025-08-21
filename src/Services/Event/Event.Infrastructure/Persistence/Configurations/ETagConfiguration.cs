using Event.Domain.Common;
using Event.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ETag support in entities
/// </summary>
public static class ETagConfigurationExtensions
{
    /// <summary>
    /// Configures ETag properties for an entity that extends ETaggableEntity
    /// </summary>
    public static void ConfigureETag<T>(this EntityTypeBuilder<T> builder) 
        where T : ETaggableEntity
    {
        // Configure ETag value storage
        builder.Property(e => e.ETagValue)
            .HasColumnName("etag_value")
            .HasMaxLength(64)
            .IsRequired();

        // Configure ETag timestamp
        builder.Property(e => e.ETagUpdatedAt)
            .HasColumnName("etag_updated_at")
            .IsRequired();

        // Add index on ETag value for efficient lookups
        builder.HasIndex(e => e.ETagValue)
            .HasDatabaseName($"IX_{typeof(T).Name}_ETag");

        // Add index on ETag timestamp for cleanup operations
        builder.HasIndex(e => e.ETagUpdatedAt)
            .HasDatabaseName($"IX_{typeof(T).Name}_ETagTimestamp");

        // Configure value conversion for ETag (if stored as computed property)
        // This handles the conversion between the ETag object and its string representation
        builder.Ignore(e => e.CurrentETag); // Don't map the object directly, use ETagValue instead
    }

    /// <summary>
    /// Configures optimistic concurrency using ETag as a concurrency token
    /// </summary>
    public static void ConfigureOptimisticConcurrency<T>(this EntityTypeBuilder<T> builder) 
        where T : ETaggableEntity
    {
        // Use ETag as concurrency token for optimistic locking
        builder.Property(e => e.ETagValue)
            .IsConcurrencyToken();

        // Configure row version alternative (if needed for additional protection)
        builder.Property<byte[]>("RowVersion")
            .HasColumnName("row_version")
            .IsRowVersion()
            .HasColumnType("bytea"); // PostgreSQL bytea type
    }

    /// <summary>
    /// Adds check constraints for ETag validation
    /// </summary>
    public static void AddETagConstraints<T>(this EntityTypeBuilder<T> builder, string tableName) 
        where T : ETaggableEntity
    {
        // Ensure ETag value is not empty
        builder.ToTable(t => t.HasCheckConstraint(
            $"CK_{tableName}_ETag_NotEmpty",
            "LENGTH(etag_value) > 0"));

        // Ensure ETag timestamp is reasonable (not in future, not too old)
        builder.ToTable(t => t.HasCheckConstraint(
            $"CK_{tableName}_ETag_ValidTimestamp",
            "etag_updated_at <= NOW() AND etag_updated_at > '2024-01-01'::timestamp"));
    }
}
