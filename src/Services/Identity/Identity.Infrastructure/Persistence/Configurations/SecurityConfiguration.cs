using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired(false);

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EventCategory)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Location)
            .HasMaxLength(200);

        builder.Property(e => e.DeviceFingerprint)
            .HasMaxLength(100);

        builder.Property(e => e.SessionId)
            .HasMaxLength(100);

        builder.Property(e => e.AdditionalData)
            .HasColumnType("jsonb"); // PostgreSQL JSON column

        builder.Property(e => e.IsResolved)
            .IsRequired();

        builder.Property(e => e.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(e => e.Resolution)
            .HasMaxLength(1000);

        // Configure indexes for performance
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_SecurityEvents_UserId");

        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_SecurityEvents_EventType");

        builder.HasIndex(e => e.EventCategory)
            .HasDatabaseName("IX_SecurityEvents_EventCategory");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_SecurityEvents_Severity");

        builder.HasIndex(e => e.IpAddress)
            .HasDatabaseName("IX_SecurityEvents_IpAddress");

        builder.HasIndex(e => e.IsResolved)
            .HasDatabaseName("IX_SecurityEvents_IsResolved");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_SecurityEvents_CreatedAt");

        builder.HasIndex(e => new { e.UserId, e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_SecurityEvents_UserId_EventType_CreatedAt");

        builder.HasIndex(e => new { e.IsResolved, e.Severity })
            .HasDatabaseName("IX_SecurityEvents_IsResolved_Severity");

        // Configure audit fields
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}

public class AccountLockoutConfiguration : IEntityTypeConfiguration<AccountLockout>
{
    public void Configure(EntityTypeBuilder<AccountLockout> builder)
    {
        builder.ToTable("AccountLockouts");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId)
            .IsRequired();

        builder.Property(l => l.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.LockedAt)
            .IsRequired();

        builder.Property(l => l.FailedAttempts)
            .IsRequired();

        builder.Property(l => l.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(l => l.IsActive)
            .IsRequired();

        builder.Property(l => l.UnlockedBy)
            .HasMaxLength(100);

        // Configure indexes for performance
        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("IX_AccountLockouts_UserId");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("IX_AccountLockouts_IsActive");

        builder.HasIndex(l => l.LockedAt)
            .HasDatabaseName("IX_AccountLockouts_LockedAt");

        builder.HasIndex(l => new { l.UserId, l.IsActive })
            .HasDatabaseName("IX_AccountLockouts_UserId_IsActive");

        builder.HasIndex(l => new { l.IsActive, l.LockedAt })
            .HasDatabaseName("IX_AccountLockouts_IsActive_LockedAt");

        // Configure audit fields
        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("IX_AccountLockouts_CreatedAt");

        // Ignore domain events
        builder.Ignore(l => l.DomainEvents);
    }
}

public class SuspiciousActivityConfiguration : IEntityTypeConfiguration<SuspiciousActivity>
{
    public void Configure(EntityTypeBuilder<SuspiciousActivity> builder)
    {
        builder.ToTable("SuspiciousActivities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.ActivityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.Location)
            .HasMaxLength(200);

        builder.Property(a => a.RiskScore)
            .HasPrecision(5, 2) // 999.99 max
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Resolution)
            .HasMaxLength(1000);

        builder.Property(a => a.ResolvedBy)
            .HasMaxLength(100);

        // Configure indexes for performance
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_SuspiciousActivities_UserId");

        builder.HasIndex(a => a.ActivityType)
            .HasDatabaseName("IX_SuspiciousActivities_ActivityType");

        builder.HasIndex(a => a.IpAddress)
            .HasDatabaseName("IX_SuspiciousActivities_IpAddress");

        builder.HasIndex(a => a.RiskScore)
            .HasDatabaseName("IX_SuspiciousActivities_RiskScore");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_SuspiciousActivities_Status");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_SuspiciousActivities_CreatedAt");

        builder.HasIndex(a => new { a.Status, a.RiskScore })
            .HasDatabaseName("IX_SuspiciousActivities_Status_RiskScore");

        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
            .HasDatabaseName("IX_SuspiciousActivities_UserId_CreatedAt");

        // Configure audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_SuspiciousActivities_CreatedAt");

        // Ignore domain events
        builder.Ignore(a => a.DomainEvents);
    }
}
