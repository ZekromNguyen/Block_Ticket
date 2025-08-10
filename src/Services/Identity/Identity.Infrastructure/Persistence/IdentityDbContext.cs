using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Models;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;
    public DbSet<MfaDevice> MfaDevices { get; set; } = null!;
    public DbSet<OAuthClient> OAuthClients { get; set; } = null!;
    public DbSet<Scope> Scopes { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<SecurityEvent> SecurityEvents { get; set; } = null!;
    public DbSet<AccountLockout> AccountLockouts { get; set; } = null!;
    public DbSet<SuspiciousActivity> SuspiciousActivities { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<ReferenceToken> ReferenceTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserSessionConfiguration());
        modelBuilder.ApplyConfiguration(new MfaDeviceConfiguration());
        modelBuilder.ApplyConfiguration(new OAuthClientConfiguration());
        modelBuilder.ApplyConfiguration(new ScopeConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityEventConfiguration());
        modelBuilder.ApplyConfiguration(new AccountLockoutConfiguration());
        modelBuilder.ApplyConfiguration(new SuspiciousActivityConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ReferenceTokenConfiguration());

        // Configure OpenIddict entities
        modelBuilder.UseOpenIddict();

        // Global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(IdentityDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : BaseAuditableEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle domain events before saving
        var domainEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Handle soft delete
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after saving
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }
}
