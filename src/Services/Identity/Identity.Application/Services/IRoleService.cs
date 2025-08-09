using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Services;

public interface IRoleService
{
    // Role Management
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<RoleDto>> UpdateRoleAsync(string roleName, UpdateRoleDto updateRoleDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> DeleteRoleAsync(string roleName, string? ipAddress = null, string? userAgent = null);
    Task<Result<RoleDto>> GetRoleAsync(string roleName);
    Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(bool activeOnly = false, RoleType? type = null);
    Task<Result<IEnumerable<RoleDto>>> GetSystemRolesAsync();

    // User Role Assignment
    Task<Result> AssignRoleAsync(AssignRoleDto assignRoleDto, string? assignedBy = null, string? ipAddress = null, string? userAgent = null);
    Task<Result> RemoveRoleAsync(RemoveRoleDto removeRoleDto, string? removedBy = null, string? ipAddress = null, string? userAgent = null);
    Task<Result<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId);
    Task<Result<IEnumerable<UserDto>>> GetRoleUsersAsync(Guid roleId);

    // Permission Management
    Task<Result<UserPermissionsDto>> GetUserPermissionsAsync(Guid userId);
    Task<Result<PermissionCheckResultDto>> CheckPermissionAsync(CheckPermissionDto checkPermissionDto);
    Task<Result<bool>> HasPermissionAsync(Guid userId, string resource, string action, string? scope = null);
    Task<Result<bool>> HasAnyPermissionAsync(Guid userId, string resource);

    // Role Hierarchy and Priority
    Task<Result> SetRolePriorityAsync(string roleName, int priority, string? ipAddress = null, string? userAgent = null);
    Task<Result<IEnumerable<RoleDto>>> GetRoleHierarchyAsync();

    // Bulk Operations
    Task<Result> AssignMultipleRolesAsync(Guid userId, Guid[] roleIds, string? assignedBy = null, string? ipAddress = null, string? userAgent = null);
    Task<Result> RemoveAllUserRolesAsync(Guid userId, string? removedBy = null, string? ipAddress = null, string? userAgent = null);

    // Role Templates and Presets
    Task<Result<IEnumerable<RoleDto>>> GetRoleTemplatesAsync();
    Task<Result<RoleDto>> CreateRoleFromTemplateAsync(string templateName, string newRoleName, string? ipAddress = null, string? userAgent = null);
}
