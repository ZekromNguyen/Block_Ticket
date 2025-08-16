using Event.Domain.Entities;
using Event.Infrastructure.Persistence.Entities;
using Event.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Models;
using System.Reflection;

namespace Event.Infrastructure.Persistence;

/// <summary>
/// Event Service database context
/// </summary>
public class EventDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public EventDbContext(DbContextOptions<EventDbContext> options, AuditInterceptor auditInterceptor) 
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    // Domain Entities
    public DbSet<EventAggregate> Events { get; set; } = null!;
    public DbSet<EventSeries> EventSeries { get; set; } = null!;
    public DbSet<Venue> Venues { get; set; } = null!;
    public DbSet<Seat> Seats { get; set; } = null!;
    public DbSet<TicketType> TicketTypes { get; set; } = null!;
    public DbSet<PricingRule> PricingRules { get; set; } = null!;
    public DbSet<Allocation> Allocations { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<ReservationItem> ReservationItems { get; set; } = null!;
    
    // Infrastructure Entities
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add audit interceptor
        optionsBuilder.AddInterceptors(_auditInterceptor);
        
        // Enable sensitive data logging in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure schema
        modelBuilder.HasDefaultSchema("event");

        // Set up soft delete filters
        SetSoftDeleteFilter<EventAggregate>(modelBuilder);
        SetSoftDeleteFilter<EventSeries>(modelBuilder);
        SetSoftDeleteFilter<Venue>(modelBuilder);
        SetSoftDeleteFilter<TicketType>(modelBuilder);
        SetSoftDeleteFilter<PricingRule>(modelBuilder);
        SetSoftDeleteFilter<Allocation>(modelBuilder);
        SetSoftDeleteFilter<Reservation>(modelBuilder);

        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);
        
        // Configure PostgreSQL specific features
        ConfigurePostgreSqlFeatures(modelBuilder);
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : BaseAuditableEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Note: Indexes are now configured in individual entity configuration files
        // This method is kept for any global index configurations if needed in the future

        // All indexes are now configured in individual entity configuration files

        // Audit log indexes
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityName, a.EntityId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Entity_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId")
            .HasFilter("\"UserId\" IS NOT NULL");
    }

    private static void ConfigurePostgreSqlFeatures(ModelBuilder modelBuilder)
    {
        // Configure full-text search for events
        modelBuilder.Entity<EventAggregate>()
            .HasGeneratedTsVectorColumn(
                e => e.SearchVector,
                "english",
                e => new { e.Title, e.Description })
            .HasIndex(e => e.SearchVector)
            .HasMethod("GIN");

        // Configure JSON columns for PostgreSQL
        modelBuilder.Entity<EventAggregate>()
            .Property(e => e.ChangeHistory)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Venue>()
            .Property(v => v.SeatMapMetadata)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.OldValues)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.NewValues)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.AdditionalData)
            .HasColumnType("jsonb");
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

        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after successful save
        foreach (var entity in ChangeTracker.Entries<BaseEntity>().Select(x => x.Entity))
        {
            entity.ClearDomainEvents();
        }

        return result;
    }
}
