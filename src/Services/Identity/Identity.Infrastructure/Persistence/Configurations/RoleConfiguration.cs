using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.Property(r => r.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.IsSystemRole)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.Priority)
            .IsRequired();

        // Configure relationships
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(r => r.Type)
            .HasDatabaseName("IX_Roles_Type");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Roles_IsActive");

        builder.HasIndex(r => r.Priority)
            .HasDatabaseName("IX_Roles_Priority");

        builder.HasIndex(r => new { r.IsActive, r.Priority })
            .HasDatabaseName("IX_Roles_IsActive_Priority");

        // Configure audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Roles_CreatedAt");

        // Ignore domain events
        builder.Ignore(r => r.DomainEvents);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RoleId)
            .IsRequired();

        builder.Property(p => p.Resource)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Scope)
            .HasMaxLength(100);

        builder.Property(p => p.IsActive)
            .IsRequired();

        // Configure indexes for performance
        builder.HasIndex(p => p.RoleId)
            .HasDatabaseName("IX_Permissions_RoleId");

        builder.HasIndex(p => p.Resource)
            .HasDatabaseName("IX_Permissions_Resource");

        builder.HasIndex(p => p.Action)
            .HasDatabaseName("IX_Permissions_Action");

        builder.HasIndex(p => new { p.Resource, p.Action })
            .HasDatabaseName("IX_Permissions_Resource_Action");

        builder.HasIndex(p => new { p.RoleId, p.Resource, p.Action })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_RoleId_Resource_Action");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Permissions_IsActive");

        // Configure audit fields
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Permissions_CreatedAt");

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}

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
