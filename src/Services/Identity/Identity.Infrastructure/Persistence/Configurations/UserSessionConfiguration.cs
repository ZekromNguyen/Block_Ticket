using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.DeviceInfo)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        builder.Property(s => s.RefreshToken)
            .HasMaxLength(256);

        // Configure indexes for performance
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        builder.HasIndex(s => s.RefreshToken)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_RefreshToken")
            .HasFilter("\"RefreshToken\" IS NOT NULL");

        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_UserSessions_ExpiresAt");

        builder.HasIndex(s => s.EndedAt)
            .HasDatabaseName("IX_UserSessions_EndedAt");

        builder.HasIndex(s => new { s.UserId, s.EndedAt })
            .HasDatabaseName("IX_UserSessions_UserId_EndedAt");

        // Configure computed property
        builder.Ignore(s => s.IsActive);

        // Configure audit fields
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_UserSessions_CreatedAt");

        // Ignore domain events
        builder.Ignore(s => s.DomainEvents);
    }
}
