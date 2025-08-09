using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class OAuthClientConfiguration : IEntityTypeConfiguration<OAuthClient>
{
    public void Configure(EntityTypeBuilder<OAuthClient> builder)
    {
        builder.ToTable("OAuthClients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClientId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.ClientId)
            .IsUnique()
            .HasDatabaseName("IX_OAuthClients_ClientId");

        builder.Property(c => c.ClientSecret)
            .HasMaxLength(256); // Hashed secret

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.RequirePkce)
            .IsRequired();

        builder.Property(c => c.RequireClientSecret)
            .IsRequired();

        builder.Property(c => c.LogoUri)
            .HasMaxLength(500);

        builder.Property(c => c.ClientUri)
            .HasMaxLength(500);

        builder.Property(c => c.TosUri)
            .HasMaxLength(500);

        builder.Property(c => c.PolicyUri)
            .HasMaxLength(500);

        // Configure collections as JSON
        builder.Property(c => c.RedirectUris)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(2000);

        builder.Property(c => c.Scopes)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(1000);

        builder.Property(c => c.GrantTypes)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(500);

        // Configure indexes for performance
        builder.HasIndex(c => c.Type)
            .HasDatabaseName("IX_OAuthClients_Type");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_OAuthClients_IsActive");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_OAuthClients_Name");

        // Configure audit fields
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_OAuthClients_CreatedAt");

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
