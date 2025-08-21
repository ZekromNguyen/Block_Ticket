using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Models;
using Event.Infrastructure.Middleware;
using Event.Infrastructure.Persistence.Entities;
using Event.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Common.Models;
using System.Data;
using System.Reflection;

namespace Event.Infrastructure.Persistence;

/// <summary>
/// Event Service database context
/// </summary>
public class EventDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IPostgreSqlSessionManager? _sessionManager;
    private readonly ILogger<EventDbContext>? _logger;

    public EventDbContext(
        DbContextOptions<EventDbContext> options,
        AuditInterceptor auditInterceptor,
        IHttpContextAccessor? httpContextAccessor = null,
        IPostgreSqlSessionManager? sessionManager = null,
        ILogger<EventDbContext>? logger = null)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _httpContextAccessor = httpContextAccessor;
        _sessionManager = sessionManager;
        _logger = logger;
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

    // Approval Workflow Entities
    public DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; } = null!;
    public DbSet<ApprovalWorkflowTemplate> ApprovalWorkflowTemplates { get; set; } = null!;
    public DbSet<ApprovalAuditLog> ApprovalAuditLogs { get; set; } = null!;
    public DbSet<ApprovalStep> ApprovalSteps { get; set; } = null!;

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

        // Set up organization-based query filters for RLS
        SetOrganizationQueryFilters(modelBuilder);

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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await SetPostgreSqlSessionContextAsync();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        SetPostgreSqlSessionContextAsync().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetPostgreSqlSessionContextAsync().GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Sets PostgreSQL session context for RLS enforcement
    /// </summary>
    private async Task SetPostgreSqlSessionContextAsync()
    {
        if (_httpContextAccessor?.HttpContext == null || _sessionManager == null)
        {
            return;
        }

        try
        {
            var organizationId = _httpContextAccessor.HttpContext.Items["CurrentOrganizationId"] as Guid?;
            var userId = _httpContextAccessor.HttpContext.Items["CurrentUserId"] as Guid?;
            var correlationId = _httpContextAccessor.HttpContext.Items["CorrelationId"] as string;

            if (organizationId.HasValue && Database.GetDbConnection() is NpgsqlConnection connection)
            {
                await _sessionManager.SetSessionVariablesAsync(connection, organizationId, userId, correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set PostgreSQL session context for RLS");
            // Don't throw - allow operation to continue
        }
    }

    /// <summary>
    /// Sets up organization-based query filters for entities that support multi-tenancy
    /// </summary>
    private void SetOrganizationQueryFilters(ModelBuilder modelBuilder)
    {
        // Note: These global query filters work in conjunction with RLS policies
        // They provide application-level filtering while RLS provides database-level security

        // Events are directly organization-scoped
        modelBuilder.Entity<EventAggregate>().HasQueryFilter(e =>
            GetCurrentOrganizationId() == Guid.Empty || e.OrganizationId == GetCurrentOrganizationId());

        // Venues are directly organization-scoped
        modelBuilder.Entity<Venue>().HasQueryFilter(v =>
            GetCurrentOrganizationId() == Guid.Empty || v.OrganizationId == GetCurrentOrganizationId());

        // Event Series are directly organization-scoped
        modelBuilder.Entity<EventSeries>().HasQueryFilter(es =>
            GetCurrentOrganizationId() == Guid.Empty || es.OrganizationId == GetCurrentOrganizationId());

        // Other entities are filtered through their relationship to events
        // These filters are automatically applied by EF Core when querying
    }

    /// <summary>
    /// Gets the current organization ID from HTTP context
    /// </summary>
    private Guid GetCurrentOrganizationId()
    {
        if (_httpContextAccessor?.HttpContext?.Items["CurrentOrganizationId"] is Guid organizationId)
        {
            return organizationId;
        }
        return Guid.Empty; // Return empty GUID to disable filtering when no context
    }
}
