using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.UserId)
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .IsRequired();

        builder.Property(ur => ur.IsActive)
            .IsRequired();

        builder.Property(ur => ur.AssignedBy)
            .HasMaxLength(100);

        // Configure relationships
        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .HasDatabaseName("IX_UserRoles_UserId_RoleId");

        builder.HasIndex(ur => ur.IsActive)
            .HasDatabaseName("IX_UserRoles_IsActive");

        builder.HasIndex(ur => ur.ExpiresAt)
            .HasDatabaseName("IX_UserRoles_ExpiresAt");

        builder.HasIndex(ur => ur.AssignedAt)
            .HasDatabaseName("IX_UserRoles_AssignedAt");

        builder.HasIndex(ur => new { ur.UserId, ur.IsActive })
            .HasDatabaseName("IX_UserRoles_UserId_IsActive");

        builder.HasIndex(ur => new { ur.IsActive, ur.ExpiresAt })
            .HasDatabaseName("IX_UserRoles_IsActive_ExpiresAt");

        // Configure audit fields
        builder.Property(ur => ur.CreatedAt)
            .IsRequired();

        builder.HasIndex(ur => ur.CreatedAt)
            .HasDatabaseName("IX_UserRoles_CreatedAt");

        // Ignore domain events
        builder.Ignore(ur => ur.DomainEvents);
    }
}
