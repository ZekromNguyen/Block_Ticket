using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class MfaDeviceConfiguration : IEntityTypeConfiguration<MfaDevice>
{
    public void Configure(EntityTypeBuilder<MfaDevice> builder)
    {
        builder.ToTable("MfaDevices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Secret)
            .HasMaxLength(512) // Encrypted data can be longer
            .IsRequired();

        builder.Property(d => d.IsActive)
            .IsRequired();

        builder.Property(d => d.UsageCount)
            .IsRequired();

        builder.Property(d => d.BackupCodes)
            .HasMaxLength(2048); // JSON array of encrypted backup codes

        // Configure indexes for performance
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("IX_MfaDevices_UserId");

        builder.HasIndex(d => new { d.UserId, d.Type, d.IsActive })
            .HasDatabaseName("IX_MfaDevices_UserId_Type_IsActive");

        builder.HasIndex(d => d.Type)
            .HasDatabaseName("IX_MfaDevices_Type");

        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("IX_MfaDevices_IsActive");

        builder.HasIndex(d => d.LastUsedAt)
            .HasDatabaseName("IX_MfaDevices_LastUsedAt");

        // Configure audit fields
        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.HasIndex(d => d.CreatedAt)
            .HasDatabaseName("IX_MfaDevices_CreatedAt");

        // Ignore domain events
        builder.Ignore(d => d.DomainEvents);
    }
}
