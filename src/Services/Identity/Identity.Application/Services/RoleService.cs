using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Roles.Commands;
using Identity.Application.Features.Roles.Queries;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class RoleService : IRoleService
{
    private readonly IMediator _mediator;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IMediator mediator, ILogger<RoleService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // Role Management
    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new CreateRoleCommand(
            createRoleDto.Name,
            createRoleDto.DisplayName,
            createRoleDto.Description,
            createRoleDto.Type,
            createRoleDto.Priority,
            createRoleDto.Permissions,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result<RoleDto>> UpdateRoleAsync(string roleName, UpdateRoleDto updateRoleDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new UpdateRoleCommand(
            roleName,
            updateRoleDto.DisplayName,
            updateRoleDto.Description,
            updateRoleDto.Priority,
            updateRoleDto.Permissions,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> DeleteRoleAsync(string roleName, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement DeleteRoleCommand
        _logger.LogInformation("DeleteRoleAsync called for role {RoleName}", roleName);
        return Result.Failure("Role deletion not implemented yet");
    }

    public async Task<Result<RoleDto>> GetRoleAsync(string roleName)
    {
        var query = new GetRoleByNameQuery(roleName);
        return await _mediator.Send(query);
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(bool activeOnly = false, RoleType? type = null)
    {
        var query = new GetRolesQuery(activeOnly, type);
        return await _mediator.Send(query);
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetSystemRolesAsync()
    {
        var query = new GetRolesQuery(true, RoleType.System);
        return await _mediator.Send(query);
    }

    // User Role Assignment
    public async Task<Result> AssignRoleAsync(AssignRoleDto assignRoleDto, string? assignedBy = null, string? ipAddress = null, string? userAgent = null)
    {
        var command = new AssignRoleCommand(
            assignRoleDto.UserId,
            assignRoleDto.RoleId,
            assignRoleDto.ExpiresAt,
            assignedBy,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> RemoveRoleAsync(RemoveRoleDto removeRoleDto, string? removedBy = null, string? ipAddress = null, string? userAgent = null)
    {
        var command = new RemoveRoleCommand(
            removeRoleDto.UserId,
            removeRoleDto.RoleId,
            removedBy,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId)
    {
        var query = new GetUserRolesQuery(userId);
        return await _mediator.Send(query);
    }

    public async Task<Result<IEnumerable<UserDto>>> GetRoleUsersAsync(Guid roleId)
    {
        // TODO: Implement GetRoleUsersQuery
        _logger.LogInformation("GetRoleUsersAsync called for role {RoleId}", roleId);
        return Result<IEnumerable<UserDto>>.Failure("Get role users not implemented yet");
    }

    // Permission Management
    public async Task<Result<UserPermissionsDto>> GetUserPermissionsAsync(Guid userId)
    {
        // TODO: Implement GetUserPermissionsQuery
        _logger.LogInformation("GetUserPermissionsAsync called for user {UserId}", userId);
        return Result<UserPermissionsDto>.Failure("Get user permissions not implemented yet");
    }

    public async Task<Result<PermissionCheckResultDto>> CheckPermissionAsync(CheckPermissionDto checkPermissionDto)
    {
        var query = new CheckUserPermissionQuery(
            checkPermissionDto.UserId,
            checkPermissionDto.Resource,
            checkPermissionDto.Action,
            checkPermissionDto.Scope);

        return await _mediator.Send(query);
    }

    public async Task<Result<bool>> HasPermissionAsync(Guid userId, string resource, string action, string? scope = null)
    {
        var query = new CheckUserPermissionQuery(userId, resource, action, scope);
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            return Result<bool>.Success(result.Value!.HasPermission);
        }

        return Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> HasAnyPermissionAsync(Guid userId, string resource)
    {
        // TODO: Implement HasAnyPermissionQuery
        _logger.LogInformation("HasAnyPermissionAsync called for user {UserId} and resource {Resource}", userId, resource);
        return Result<bool>.Failure("Has any permission check not implemented yet");
    }

    // Role Hierarchy and Priority
    public async Task<Result> SetRolePriorityAsync(string roleName, int priority, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement SetRolePriorityCommand
        _logger.LogInformation("SetRolePriorityAsync called for role {RoleName}", roleName);
        return Result.Failure("Set role priority not implemented yet");
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetRoleHierarchyAsync()
    {
        var query = new GetRolesQuery(true);
        var result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            var sortedRoles = result.Value!.OrderByDescending(r => r.Priority).ThenBy(r => r.Name);
            return Result<IEnumerable<RoleDto>>.Success(sortedRoles);
        }

        return result;
    }

    // Bulk Operations
    public async Task<Result> AssignMultipleRolesAsync(Guid userId, Guid[] roleIds, string? assignedBy = null, string? ipAddress = null, string? userAgent = null)
    {
        var results = new List<Result>();

        foreach (var roleId in roleIds)
        {
            var assignRoleDto = new AssignRoleDto { UserId = userId, RoleId = roleId };
            var result = await AssignRoleAsync(assignRoleDto, assignedBy, ipAddress, userAgent);
            results.Add(result);
        }

        var failedResults = results.Where(r => r.IsFailure).ToList();
        if (failedResults.Any())
        {
            var errors = failedResults.Select(r => r.Error).ToList();
            return Result.Failure($"Some role assignments failed: {string.Join(", ", errors)}");
        }

        return Result.Success();
    }

    public async Task<Result> RemoveAllUserRolesAsync(Guid userId, string? removedBy = null, string? ipAddress = null, string? userAgent = null)
    {
        var userRolesResult = await GetUserRolesAsync(userId);
        if (userRolesResult.IsFailure)
        {
            return Result.Failure(userRolesResult.Error);
        }

        var activeRoles = userRolesResult.Value!.Where(ur => ur.IsActive && !ur.IsExpired);
        var results = new List<Result>();

        foreach (var userRole in activeRoles)
        {
            var removeRoleDto = new RemoveRoleDto { UserId = userId, RoleId = userRole.RoleId };
            var result = await RemoveRoleAsync(removeRoleDto, removedBy, ipAddress, userAgent);
            results.Add(result);
        }

        var failedResults = results.Where(r => r.IsFailure).ToList();
        if (failedResults.Any())
        {
            var errors = failedResults.Select(r => r.Error).ToList();
            return Result.Failure($"Some role removals failed: {string.Join(", ", errors)}");
        }

        return Result.Success();
    }

    // Role Templates and Presets
    public async Task<Result<IEnumerable<RoleDto>>> GetRoleTemplatesAsync()
    {
        // Return system roles as templates
        return await GetSystemRolesAsync();
    }

    public async Task<Result<RoleDto>> CreateRoleFromTemplateAsync(string templateName, string newRoleName, string? ipAddress = null, string? userAgent = null)
    {
        var templateResult = await GetRoleAsync(templateName);
        if (templateResult.IsFailure)
        {
            return Result<RoleDto>.Failure($"Template role '{templateName}' not found");
        }

        var template = templateResult.Value!;
        var createRoleDto = new CreateRoleDto
        {
            Name = newRoleName,
            DisplayName = $"{template.DisplayName} (Copy)",
            Description = $"Copy of {template.Description}",
            Type = "Custom",
            Priority = template.Priority,
            Permissions = template.Permissions
        };

        return await CreateRoleAsync(createRoleDto, ipAddress, userAgent);
    }
}
