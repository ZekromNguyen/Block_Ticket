using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistory");

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ph => ph.UserId)
            .IsRequired();

        builder.Property(ph => ph.PasswordHash)
            .IsRequired()
            .HasMaxLength(500); // Sufficient for base64 encoded hash

        // Foreign key relationship
        builder.HasOne(ph => ph.User)
            .WithMany(u => u.PasswordHistory)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(ph => ph.UserId)
            .HasDatabaseName("IX_PasswordHistory_UserId");

        builder.HasIndex(ph => new { ph.UserId, ph.CreatedAt })
            .HasDatabaseName("IX_PasswordHistory_UserId_CreatedAt")
            .IsDescending(false, true); // UserId ascending, CreatedAt descending

        // Composite index for password checking
        builder.HasIndex(ph => new { ph.UserId, ph.PasswordHash })
            .HasDatabaseName("IX_PasswordHistory_UserId_PasswordHash");
    }
}
