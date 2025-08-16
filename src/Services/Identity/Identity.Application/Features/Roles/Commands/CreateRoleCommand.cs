using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Roles.Commands;

public record CreateRoleCommand(
    string Name,
    string DisplayName,
    string Description,
    string Type = "Custom",
    int Priority = 0,
    PermissionDto[] Permissions = null!,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<RoleDto>>;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if role already exists
            if (await _roleRepository.ExistsAsync(request.Name, cancellationToken))
            {
                return Result<RoleDto>.Failure("Role already exists");
            }

            // Parse role type
            if (!Enum.TryParse<RoleType>(request.Type, true, out var roleType))
            {
                return Result<RoleDto>.Failure("Invalid role type");
            }

            // Create role
            var role = new Role(request.Name, request.DisplayName, request.Description, roleType, false, request.Priority);

            // Add permissions
            if (request.Permissions != null)
            {
                foreach (var permissionDto in request.Permissions)
                {
                    // Since permissions should be pre-seeded, we'll create a basic permission
                    // The proper implementation should use a permission repository to find existing permissions
                    var permission = new Permission(
                        permissionDto.Name ?? $"{permissionDto.Resource}:{permissionDto.Action}",
                        permissionDto.Description ?? $"Permission to {permissionDto.Action} {permissionDto.Resource}",
                        permissionDto.Resource, 
                        permissionDto.Action, 
                        permissionDto.Service ?? "Identity",
                        permissionDto.Scope);
                    
                    role.AddPermission(permission);
                }
            }

            await _roleRepository.AddAsync(role, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty, // System action
                "ROLE_CREATED",
                "ROLE_MANAGEMENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"name\":\"{request.Name}\",\"type\":\"{request.Type}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Role {RoleName} created successfully", request.Name);

            // Map to DTO
            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                Type = role.Type.ToString(),
                IsSystemRole = role.IsSystemRole,
                IsActive = role.IsActive,
                Priority = role.Priority,
                Permissions = role.GetActivePermissions().Select(p => new PermissionDto
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    IsActive = p.IsActive
                }).ToArray(),
                CreatedAt = role.CreatedAt
            };

            return Result<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", request.Name);
            return Result<RoleDto>.Failure("An error occurred while creating the role");
        }
    }
}

public record UpdateRoleCommand(
    string Name,
    string DisplayName,
    string Description,
    int Priority,
    PermissionDto[] Permissions,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<RoleDto>>;

public class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _roleRepository.GetByNameAsync(request.Name, cancellationToken);
            if (role == null)
            {
                return Result<RoleDto>.Failure("Role not found");
            }

            // Update role details
            role.UpdateDetails(request.DisplayName, request.Description);
            role.SetPriority(request.Priority);

            // Update permissions
            role.ClearPermissions();
            foreach (var permissionDto in request.Permissions)
            {
                // Since permissions should be pre-seeded, we'll create a basic permission
                // The proper implementation should use a permission repository to find existing permissions
                var permission = new Permission(
                    permissionDto.Name ?? $"{permissionDto.Resource}:{permissionDto.Action}",
                    permissionDto.Description ?? $"Permission to {permissionDto.Action} {permissionDto.Resource}",
                    permissionDto.Resource, 
                    permissionDto.Action, 
                    permissionDto.Service ?? "Identity",
                    permissionDto.Scope);
                
                role.AddPermission(permission);
            }

            await _roleRepository.UpdateAsync(role, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty,
                "ROLE_UPDATED",
                "ROLE_MANAGEMENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"name\":\"{request.Name}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Role {RoleName} updated successfully", request.Name);

            // Map to DTO
            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                Type = role.Type.ToString(),
                IsSystemRole = role.IsSystemRole,
                IsActive = role.IsActive,
                Priority = role.Priority,
                Permissions = role.GetActivePermissions().Select(p => new PermissionDto
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    IsActive = p.IsActive
                }).ToArray(),
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };

            return Result<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleName}", request.Name);
            return Result<RoleDto>.Failure("An error occurred while updating the role");
        }
    }
}
