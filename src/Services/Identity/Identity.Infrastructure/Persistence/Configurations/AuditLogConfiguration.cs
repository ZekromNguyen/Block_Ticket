using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Resource)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Level)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(a => a.UserAgent)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.AdditionalData)
            .HasMaxLength(4000); // JSON data

        builder.Property(a => a.Success)
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(a => a.SessionId)
            .HasMaxLength(100);

        builder.Property(a => a.ClientId)
            .HasMaxLength(100);

        // Configure indexes for performance and querying
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(a => a.Resource)
            .HasDatabaseName("IX_AuditLogs_Resource");

        builder.HasIndex(a => a.Level)
            .HasDatabaseName("IX_AuditLogs_Level");

        builder.HasIndex(a => a.Success)
            .HasDatabaseName("IX_AuditLogs_Success");

        builder.HasIndex(a => a.IpAddress)
            .HasDatabaseName("IX_AuditLogs_IpAddress");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");

        builder.HasIndex(a => a.SessionId)
            .HasDatabaseName("IX_AuditLogs_SessionId");

        builder.HasIndex(a => a.ClientId)
            .HasDatabaseName("IX_AuditLogs_ClientId");

        // Composite indexes for common queries
        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");

        builder.HasIndex(a => new { a.Action, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Action_CreatedAt");

        builder.HasIndex(a => new { a.Success, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Success_CreatedAt");

        builder.HasIndex(a => new { a.Level, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Level_CreatedAt");

        // Configure audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(a => a.DomainEvents);
    }
}
