namespace Identity.Application.DTOs;

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsSystemRole { get; init; }
    public bool IsActive { get; init; }
    public int Priority { get; init; }
    public PermissionDto[] Permissions { get; init; } = Array.Empty<PermissionDto>();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateRoleDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "Custom";
    public int Priority { get; init; } = 0;
    public PermissionDto[] Permissions { get; init; } = Array.Empty<PermissionDto>();
}

public record UpdateRoleDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Priority { get; init; }
    public PermissionDto[] Permissions { get; init; } = Array.Empty<PermissionDto>();
}

public record PermissionDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string? Scope { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UserRoleDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string RoleDisplayName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public string? AssignedBy { get; init; }
}

public record AssignRoleDto
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record RemoveRoleDto
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}

public record RolePermissionsDto
{
    public Guid RoleId { get; init; }
    public PermissionDto[] Permissions { get; init; } = Array.Empty<PermissionDto>();
}

public record UserPermissionsDto
{
    public Guid UserId { get; init; }
    public string[] Resources { get; init; } = Array.Empty<string>();
    public PermissionDto[] Permissions { get; init; } = Array.Empty<PermissionDto>();
    public RoleDto[] Roles { get; init; } = Array.Empty<RoleDto>();
}

public record CheckPermissionDto
{
    public Guid UserId { get; init; }
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Scope { get; init; }
}

public record PermissionCheckResultDto
{
    public bool HasPermission { get; init; }
    public string[] GrantingRoles { get; init; } = Array.Empty<string>();
    public string? Reason { get; init; }
}
