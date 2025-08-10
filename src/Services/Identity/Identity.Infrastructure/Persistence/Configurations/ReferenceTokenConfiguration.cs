using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class ReferenceTokenConfiguration : IEntityTypeConfiguration<ReferenceToken>
{
    public void Configure(EntityTypeBuilder<ReferenceToken> builder)
    {
        builder.ToTable("ReferenceTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.TokenType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rt => rt.SessionId)
            .HasMaxLength(128);

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevokedAt);

        builder.Property(rt => rt.RevokedBy)
            .HasMaxLength(256);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500);

        builder.Property(rt => rt.Claims)
            .HasColumnType("jsonb"); // PostgreSQL JSON column

        builder.Property(rt => rt.Scopes)
            .HasMaxLength(1000);

        builder.Property(rt => rt.ClientId)
            .HasMaxLength(128);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500);

        // Indexes for performance
        builder.HasIndex(rt => rt.TokenId)
            .IsUnique()
            .HasDatabaseName("IX_ReferenceTokens_TokenId");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_ReferenceTokens_UserId");

        builder.HasIndex(rt => rt.SessionId)
            .HasDatabaseName("IX_ReferenceTokens_SessionId");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_ReferenceTokens_ExpiresAt");

        builder.HasIndex(rt => new { rt.TokenType, rt.IsRevoked })
            .HasDatabaseName("IX_ReferenceTokens_TokenType_IsRevoked");

        builder.HasIndex(rt => new { rt.UserId, rt.TokenType, rt.IsRevoked })
            .HasDatabaseName("IX_ReferenceTokens_UserId_TokenType_IsRevoked");

        // Foreign key relationship with User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure base entity properties
        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(rt => rt.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(rt => rt.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.DeletedAt);

        // Global query filter for soft delete
        builder.HasQueryFilter(rt => !rt.IsDeleted);
    }
}
