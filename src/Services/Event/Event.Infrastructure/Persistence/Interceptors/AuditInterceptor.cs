using Event.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Common.Models;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor for automatic audit logging of entity changes
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = CreateAuditEntry(entry);
            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
            }
        }

        // Add audit logs to context
        foreach (var auditEntry in auditEntries)
        {
            var auditLog = CreateAuditLog(auditEntry);
            context.Set<AuditLog>().Add(auditLog);
        }
    }

    private AuditEntry? CreateAuditEntry(EntityEntry<BaseAuditableEntity> entry)
    {
        var entityName = entry.Entity.GetType().Name;
        var entityId = entry.Entity.Id;
        var action = GetAction(entry.State);

        if (string.IsNullOrEmpty(action))
            return null;

        var auditEntry = new AuditEntry
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Timestamp = DateTime.UtcNow
        };

        // Capture old values for updates and deletes
        if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
        {
            auditEntry.OldValues = JsonSerializer.Serialize(GetEntityValues(entry.OriginalValues));
        }

        // Capture new values for adds and updates
        if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        {
            auditEntry.NewValues = JsonSerializer.Serialize(GetEntityValues(entry.CurrentValues));
        }

        // Capture changed properties for updates
        if (entry.State == EntityState.Modified)
        {
            auditEntry.ChangedProperties = entry.Properties
                .Where(p => p.IsModified && !IsAuditProperty(p.Metadata.Name))
                .Select(p => p.Metadata.Name)
                .ToArray();
        }

        // Get version if entity supports versioning
        if (entry.CurrentValues.Properties.Any(p => p.Name == "Version"))
        {
            auditEntry.Version = entry.CurrentValues["Version"] as int?;
        }

        return auditEntry;
    }

    private AuditLog CreateAuditLog(AuditEntry auditEntry)
    {
        // Get current user context (this would typically come from ICurrentUserService)
        var currentUser = GetCurrentUserContext();

        var auditLog = new AuditLog(
            auditEntry.EntityName,
            auditEntry.EntityId,
            auditEntry.Action,
            currentUser.UserId,
            currentUser.UserEmail,
            currentUser.RequestId,
            currentUser.CorrelationId);

        auditLog.SetActor(currentUser.ActorType, currentUser.ActorIdentifier);
        auditLog.SetRequestContext(currentUser.IpAddress, currentUser.UserAgent);
        
        auditLog.SetChangeDetails(
            auditEntry.OldValues,
            auditEntry.NewValues,
            auditEntry.ChangedProperties);

        auditLog.SetMetadata(
            currentUser.Reason,
            currentUser.AdditionalData,
            currentUser.Source,
            auditEntry.Version);

        return auditLog;
    }

    private Dictionary<string, object?> GetEntityValues(PropertyValues values)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in values.Properties)
        {
            // Skip audit properties and navigation properties
            if (IsAuditProperty(property.Name) || property.IsForeignKey())
                continue;

            var value = values[property];
            
            // Handle complex types and value objects
            if (value != null && !IsSimpleType(value.GetType()))
            {
                try
                {
                    result[property.Name] = JsonSerializer.Serialize(value, _jsonOptions);
                }
                catch
                {
                    result[property.Name] = value.ToString();
                }
            }
            else
            {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static string? GetAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => null
        };
    }

    private static bool IsAuditProperty(string propertyName)
    {
        var auditProperties = new[]
        {
            nameof(BaseEntity.CreatedAt),
            nameof(BaseEntity.UpdatedAt),
            nameof(BaseAuditableEntity.CreatedBy),
            nameof(BaseAuditableEntity.UpdatedBy),
            nameof(BaseAuditableEntity.IsDeleted),
            nameof(BaseAuditableEntity.DeletedAt),
            nameof(BaseAuditableEntity.DeletedBy),
            "SearchVector" // PostgreSQL search vector
        };

        return auditProperties.Contains(propertyName);
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               Nullable.GetUnderlyingType(type) != null;
    }

    private CurrentUserContext GetCurrentUserContext()
    {
        // This would typically be injected from ICurrentUserService
        // For now, return a default context
        return new CurrentUserContext
        {
            ActorType = "SYSTEM",
            Source = "API"
        };
    }

    private class AuditEntry
    {
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string[]? ChangedProperties { get; set; }
        public int? Version { get; set; }
    }

    private class CurrentUserContext
    {
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string ActorType { get; set; } = "SYSTEM";
        public string? ActorIdentifier { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Reason { get; set; }
        public string? AdditionalData { get; set; }
        public string Source { get; set; } = "API";
    }
}
