using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class ScopeConfiguration : IEntityTypeConfiguration<Scope>
{
    public void Configure(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("Scopes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_Scopes_Name");

        builder.Property(s => s.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IsRequired)
            .IsRequired();

        builder.Property(s => s.IsDefault)
            .IsRequired();

        builder.Property(s => s.ShowInDiscoveryDocument)
            .IsRequired();

        // Configure indexes for performance
        builder.HasIndex(s => s.Type)
            .HasDatabaseName("IX_Scopes_Type");

        builder.HasIndex(s => s.IsRequired)
            .HasDatabaseName("IX_Scopes_IsRequired");

        builder.HasIndex(s => s.IsDefault)
            .HasDatabaseName("IX_Scopes_IsDefault");

        builder.HasIndex(s => s.ShowInDiscoveryDocument)
            .HasDatabaseName("IX_Scopes_ShowInDiscoveryDocument");

        // Configure audit fields
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_Scopes_CreatedAt");

        // Ignore domain events
        builder.Ignore(s => s.DomainEvents);
    }
}
