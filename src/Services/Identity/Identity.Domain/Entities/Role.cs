using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class Role : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RoleType Type { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; } // Higher number = higher priority

    // EF Core navigation properties - these need to be settable for EF change tracking
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    private Role() { } // For EF Core

    public Role(string name, string displayName, string description, RoleType type, bool isSystemRole = false, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be null or empty");

        Name = name;
        NormalizedName = name.ToUpper();
        DisplayName = displayName;
        Description = description;
        Type = type;
        IsSystemRole = isSystemRole;
        IsActive = true;
        Priority = priority;
        
        // Initialize navigation properties
        RolePermissions = new List<RolePermission>();
        UserRoles = new List<UserRole>();
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

    public void AddPermission(Permission permission, DateTime? expiresAt = null, Guid? grantedBy = null)
    {
        var existingRolePermission = RolePermissions
            .FirstOrDefault(rp => rp.PermissionId == permission.Id);

        if (existingRolePermission != null)
        {
            if (!existingRolePermission.IsActive)
            {
                // Need to create a new RolePermission since the old one is revoked
                var newRolePermission = new RolePermission(Id, permission.Id, grantedBy, expiresAt);
                RolePermissions.Add(newRolePermission);
                UpdatedAt = DateTime.UtcNow;
            }
            return;
        }

        var rolePermission = new RolePermission(Id, permission.Id, grantedBy, expiresAt);
        RolePermissions.Add(rolePermission);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemovePermission(Guid permissionId)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System role permissions cannot be modified");

        var rolePermission = RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission != null)
        {
            rolePermission.Revoke();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearPermissions()
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System role permissions cannot be modified");

        foreach (var rolePermission in RolePermissions)
        {
            rolePermission.Revoke();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasPermission(string resource, string action, string? service = null)
    {
        return RolePermissions.Any(rp => 
            rp.IsActive && 
            !rp.IsExpired && 
            rp.Permission.Resource == resource && 
            rp.Permission.Action == action &&
            (service == null || rp.Permission.Service == service) &&
            rp.Permission.IsActive);
    }

    public bool HasAnyPermission(string resource, string? service = null)
    {
        return RolePermissions.Any(rp => 
            rp.IsActive && 
            !rp.IsExpired && 
            rp.Permission.Resource == resource &&
            (service == null || rp.Permission.Service == service) &&
            rp.Permission.IsActive);
    }

    public IReadOnlyList<Permission> GetActivePermissions()
    {
        return RolePermissions
            .Where(rp => rp.IsActive && !rp.IsExpired)
            .Select(rp => rp.Permission)
            .ToList();
    }

    public IReadOnlyList<Permission> GetPermissions(string? service = null)
    {
        return RolePermissions
            .Where(rp => rp.IsActive && !rp.IsExpired && 
                         (service == null || rp.Permission.Service == service))
            .Select(rp => rp.Permission)
            .ToList();
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
