using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Roles.Queries;

public record GetRolesQuery(bool ActiveOnly = false, RoleType? Type = null) : IQuery<Result<IEnumerable<RoleDto>>>;

public class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, Result<IEnumerable<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetRolesQueryHandler> _logger;

    public GetRolesQueryHandler(IRoleRepository roleRepository, ILogger<GetRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Role> roles;

            if (request.Type.HasValue)
            {
                roles = await _roleRepository.GetByTypeAsync(request.Type.Value, cancellationToken);
            }
            else if (request.ActiveOnly)
            {
                roles = await _roleRepository.GetActiveRolesAsync(cancellationToken);
            }
            else
            {
                roles = await _roleRepository.GetAllAsync(cancellationToken);
            }

            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                Description = r.Description,
                Type = r.Type.ToString(),
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                Priority = r.Priority,
                Permissions = r.GetActivePermissions().Select(p => new PermissionDto
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    IsActive = p.IsActive
                }).ToArray(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });

            return Result<IEnumerable<RoleDto>>.Success(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return Result<IEnumerable<RoleDto>>.Failure("An error occurred while retrieving roles");
        }
    }
}

public record GetRoleByNameQuery(string Name) : IQuery<Result<RoleDto>>;

public class GetRoleByNameQueryHandler : IQueryHandler<GetRoleByNameQuery, Result<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<GetRoleByNameQueryHandler> _logger;

    public GetRoleByNameQueryHandler(IRoleRepository roleRepository, ILogger<GetRoleByNameQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _roleRepository.GetByNameAsync(request.Name, cancellationToken);
            if (role == null)
            {
                return Result<RoleDto>.Failure("Role not found");
            }

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
            _logger.LogError(ex, "Error retrieving role {RoleName}", request.Name);
            return Result<RoleDto>.Failure("An error occurred while retrieving the role");
        }
    }
}

public record GetUserRolesQuery(Guid UserId) : IQuery<Result<IEnumerable<UserRoleDto>>>;

public class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, Result<IEnumerable<UserRoleDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserRolesQueryHandler> _logger;

    public GetUserRolesQueryHandler(IUserRepository userRepository, ILogger<GetUserRolesQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UserRoleDto>>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<IEnumerable<UserRoleDto>>.Failure("User not found");
            }

            var userRoleDtos = user.UserRoles.Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                RoleDisplayName = ur.Role.DisplayName,
                AssignedAt = ur.AssignedAt,
                ExpiresAt = ur.ExpiresAt,
                IsActive = ur.IsActive,
                IsExpired = ur.IsExpired(),
                AssignedBy = ur.AssignedBy
            });

            return Result<IEnumerable<UserRoleDto>>.Success(userRoleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles for user {UserId}", request.UserId);
            return Result<IEnumerable<UserRoleDto>>.Failure("An error occurred while retrieving user roles");
        }
    }
}

public record CheckUserPermissionQuery(
    Guid UserId,
    string Resource,
    string Action,
    string? Scope = null) : IQuery<Result<PermissionCheckResultDto>>;

public class CheckUserPermissionQueryHandler : IQueryHandler<CheckUserPermissionQuery, Result<PermissionCheckResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CheckUserPermissionQueryHandler> _logger;

    public CheckUserPermissionQueryHandler(IUserRepository userRepository, ILogger<CheckUserPermissionQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<PermissionCheckResultDto>> Handle(CheckUserPermissionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<PermissionCheckResultDto>.Success(new PermissionCheckResultDto
                {
                    HasPermission = false,
                    Reason = "User not found"
                });
            }

            var activeRoles = user.GetActiveRoles().ToList();
            var grantingRoles = new List<string>();

            foreach (var role in activeRoles)
            {
                if (role.HasPermission(request.Resource, request.Action))
                {
                    grantingRoles.Add(role.Name);
                }
            }

            var hasPermission = grantingRoles.Any();

            var result = new PermissionCheckResultDto
            {
                HasPermission = hasPermission,
                GrantingRoles = grantingRoles.ToArray(),
                Reason = hasPermission ? null : "No matching permissions found"
            };

            return Result<PermissionCheckResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", request.UserId);
            return Result<PermissionCheckResultDto>.Failure("An error occurred while checking permissions");
        }
    }
}
