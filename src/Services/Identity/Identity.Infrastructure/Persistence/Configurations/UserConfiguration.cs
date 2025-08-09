using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Configure Email value object
        builder.Property(u => u.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Configure WalletAddress value object
        builder.Property(u => u.WalletAddress)
            .HasConversion(
                wallet => wallet != null ? wallet.Value : null,
                value => value != null ? new WalletAddress(value) : null)
            .HasMaxLength(42);

        builder.HasIndex(u => u.WalletAddress)
            .IsUnique()
            .HasDatabaseName("IX_Users_WalletAddress")
            .HasFilter("[WalletAddress] IS NOT NULL");

        // Configure other properties
        builder.Property(u => u.FirstName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.UserType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.EmailConfirmed)
            .IsRequired();

        builder.Property(u => u.MfaEnabled)
            .IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired();

        // Configure relationships
        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MfaDevices)
            .WithOne()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(u => u.UserType)
            .HasDatabaseName("IX_Users_UserType");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        builder.HasIndex(u => u.EmailConfirmed)
            .HasDatabaseName("IX_Users_EmailConfirmed");

        builder.HasIndex(u => u.MfaEnabled)
            .HasDatabaseName("IX_Users_MfaEnabled");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("IX_Users_LastLoginAt");

        builder.HasIndex(u => u.LockedOutUntil)
            .HasDatabaseName("IX_Users_LockedOutUntil")
            .HasFilter("[LockedOutUntil] IS NOT NULL");

        // Configure audit fields
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
