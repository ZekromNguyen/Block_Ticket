using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public Guid? GrantedBy { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { } // For EF Core

    public RolePermission(Guid roleId, Guid permissionId, Guid? grantedBy = null, DateTime? expiresAt = null)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAt = DateTime.UtcNow;
        GrantedBy = grantedBy;
        ExpiresAt = expiresAt;
        IsActive = true;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void Extend(DateTime newExpiryDate)
    {
        if (newExpiryDate <= DateTime.UtcNow)
            throw new ArgumentException("New expiry date must be in the future");

        ExpiresAt = newExpiryDate;
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    public bool IsValid => IsActive && !IsExpired;
}
