using Shared.Common.Models;

namespace Event.Infrastructure.Persistence.Entities;

/// <summary>
/// Represents an immutable audit log entry for tracking changes
/// </summary>
public class AuditLog : BaseEntity
{
    // Basic Properties
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty; // CREATE, UPDATE, DELETE, ARCHIVE
    public DateTime Timestamp { get; private set; }
    
    // Actor Information
    public Guid? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string? ActorType { get; private set; } // USER, SYSTEM, SERVICE
    public string? ActorIdentifier { get; private set; }
    
    // Request Context
    public string? RequestId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    
    // Change Details
    public string? OldValues { get; private set; } // JSON
    public string? NewValues { get; private set; } // JSON
    public string? ChangedProperties { get; private set; } // JSON array of property names
    public string? Reason { get; private set; }
    
    // Metadata
    public string? AdditionalData { get; private set; } // JSON for extra context
    public string? Source { get; private set; } // API, BACKGROUND_JOB, MIGRATION, etc.
    public int? Version { get; private set; } // Entity version at time of change
    
    // For EF Core
    private AuditLog() { }
    
    public AuditLog(
        string entityName,
        Guid entityId,
        string action,
        Guid? userId = null,
        string? userEmail = null,
        string? requestId = null,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name cannot be empty", nameof(entityName));
        
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty", nameof(action));

        EntityName = entityName.Trim();
        EntityId = entityId;
        Action = action.Trim().ToUpperInvariant();
        Timestamp = DateTime.UtcNow;
        UserId = userId;
        UserEmail = userEmail?.Trim();
        RequestId = requestId?.Trim();
        CorrelationId = correlationId?.Trim();
    }
    
    public void SetActor(string actorType, string? actorIdentifier)
    {
        ActorType = actorType?.Trim().ToUpperInvariant();
        ActorIdentifier = actorIdentifier?.Trim();
    }
    
    public void SetRequestContext(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
    }
    
    public void SetChangeDetails(string? oldValues, string? newValues, string[]? changedProperties)
    {
        OldValues = oldValues?.Trim();
        NewValues = newValues?.Trim();
        ChangedProperties = changedProperties?.Any() == true ? 
            System.Text.Json.JsonSerializer.Serialize(changedProperties) : null;
    }
    
    public void SetMetadata(string? reason, string? additionalData, string? source, int? version)
    {
        Reason = reason?.Trim();
        AdditionalData = additionalData?.Trim();
        Source = source?.Trim().ToUpperInvariant();
        Version = version;
    }
    
    public string[] GetChangedProperties()
    {
        if (string.IsNullOrWhiteSpace(ChangedProperties))
            return Array.Empty<string>();
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string[]>(ChangedProperties) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
    
    public bool IsCreate() => Action == "CREATE";
    public bool IsUpdate() => Action == "UPDATE";
    public bool IsDelete() => Action == "DELETE";
    public bool IsArchive() => Action == "ARCHIVE";
    
    public bool HasChanges() => !string.IsNullOrWhiteSpace(ChangedProperties);
    
    public bool IsSystemAction() => ActorType == "SYSTEM";
    public bool IsUserAction() => ActorType == "USER";
    public bool IsServiceAction() => ActorType == "SERVICE";
}
