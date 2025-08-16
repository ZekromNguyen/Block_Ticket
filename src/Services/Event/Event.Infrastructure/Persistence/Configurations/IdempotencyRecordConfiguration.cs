using Event.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for IdempotencyRecord
/// </summary>
public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Idempotency Key (unique)
        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("idx_idempotency_records_key");

        // Request Information
        builder.Property(x => x.RequestPath)
            .HasColumnName("request_path")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.HttpMethod)
            .HasColumnName("http_method")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.RequestBody)
            .HasColumnName("request_body")
            .HasColumnType("text");

        builder.Property(x => x.RequestHeaders)
            .HasColumnName("request_headers")
            .HasColumnType("jsonb");

        // Response Information
        builder.Property(x => x.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("text");

        builder.Property(x => x.ResponseStatusCode)
            .HasColumnName("response_status_code")
            .HasDefaultValue(0);

        builder.Property(x => x.ResponseHeaders)
            .HasColumnName("response_headers")
            .HasColumnType("jsonb");

        // Timing
        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // User and Organization
        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100);

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(x => x.RequestId)
            .HasColumnName("request_id")
            .HasMaxLength(100)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_idempotency_records_expires_at");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_idempotency_records_user_id");

        builder.HasIndex(x => x.OrganizationId)
            .HasDatabaseName("idx_idempotency_records_organization_id");

        builder.HasIndex(x => new { x.RequestPath, x.HttpMethod })
            .HasDatabaseName("idx_idempotency_records_path_method");

        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("idx_idempotency_records_processed_at");

        // Check constraints
        builder.HasCheckConstraint(
            "ck_idempotency_records_expires_at_future",
            "expires_at > processed_at");

        builder.HasCheckConstraint(
            "ck_idempotency_records_http_method",
            "http_method IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE')");

        builder.HasCheckConstraint(
            "ck_idempotency_records_status_code",
            "response_status_code >= 0 AND response_status_code <= 999");
    }
}
