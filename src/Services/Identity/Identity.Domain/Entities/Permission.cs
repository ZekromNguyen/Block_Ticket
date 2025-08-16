using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class Permission : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Service { get; private set; } = string.Empty;
    public string? Scope { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core navigation property for the join entity
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    private Permission() { } // For EF Core

    public Permission(string name, string description, string resource, string action, string service, string? scope = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be null or empty");
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be null or empty");
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty");
        if (string.IsNullOrWhiteSpace(service))
            throw new ArgumentException("Service cannot be null or empty");

        Name = name;
        Description = description;
        Resource = resource;
        Action = action;
        Service = service;
        Scope = scope;
        IsActive = true;
    }

    public void UpdateDetails(string description, string? scope = null)
    {
        Description = description;
        Scope = scope;
        UpdatedAt = DateTime.UtcNow;
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

    public bool MatchesResource(string resource, string action, string? scope = null)
    {
        if (!IsActive)
            return false;

        var resourceMatches = Resource == "*" || Resource.Equals(resource, StringComparison.OrdinalIgnoreCase);
        var actionMatches = Action == "*" || Action.Equals(action, StringComparison.OrdinalIgnoreCase);
        var scopeMatches = string.IsNullOrEmpty(Scope) || 
                          string.IsNullOrEmpty(scope) || 
                          Scope.Equals(scope, StringComparison.OrdinalIgnoreCase);

        return resourceMatches && actionMatches && scopeMatches;
    }

    public override string ToString()
    {
        return $"{Service}:{Resource}:{Action}" + (string.IsNullOrEmpty(Scope) ? "" : $":{Scope}");
    }

    public IReadOnlyList<Role> GetAssignedRoles()
    {
        return RolePermissions
            .Where(rp => rp.IsActive && !rp.IsExpired)
            .Select(rp => rp.Role)
            .ToList();
    }
}
