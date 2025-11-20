using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class Role : BaseAuditableEntity
{
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RoleType Type { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; } // Higher number = higher priority



    private Role() { } // For EF Core

    public Role(string name, string displayName, string description, RoleType type, bool isSystemRole = false, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty");

        Name = name;
        DisplayName = displayName;
        Description = description;
        Type = type;
        IsSystemRole = isSystemRole;
        IsActive = true;
        Priority = priority;
    }

    public void UpdateDetails(string displayName, string description)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be modified");

        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be deactivated");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }


}



public class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? AssignedBy { get; private set; }

    // Navigation properties
    public Role Role { get; private set; } = null!;

    private UserRole() { } // For EF Core

    public UserRole(Guid userId, Guid roleId, string? assignedBy = null, DateTime? expiresAt = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IsActive = true;
        AssignedBy = assignedBy;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendExpiry(DateTime newExpiryDate)
    {
        ExpiresAt = newExpiryDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return IsActive && !IsExpired();
    }
}

public enum RoleType
{
    System = 0,     // System-defined roles (Admin, User, etc.)
    Custom = 1,     // User-defined roles
    Temporary = 2   // Temporary roles with expiration
}

// Predefined system roles
public static class SystemRoles
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string Promoter = "promoter";
    public const string Fan = "fan";
    public const string ApiClient = "api_client";
}

// Predefined resources and actions
public static class Resources
{
    public const string Users = "users";
    public const string Roles = "roles";
    public const string OAuthClients = "oauth_clients";
    public const string Scopes = "scopes";
    public const string Events = "events";
    public const string Tickets = "tickets";
    public const string Wallet = "wallet";
    public const string AuditLogs = "audit_logs";
    public const string System = "system";
}

public static class Actions
{
    public const string Create = "create";
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Manage = "manage";
    public const string Execute = "execute";
    public const string Approve = "approve";
}
