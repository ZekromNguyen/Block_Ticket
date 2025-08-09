using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Roles.Commands;

public record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    DateTime? ExpiresAt = null,
    string? AssignedBy = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                return Result.Failure("Role not found");
            }

            if (!role.IsActive)
            {
                return Result.Failure("Cannot assign inactive role");
            }

            // Check if user already has this role
            if (user.HasRole(request.RoleId))
            {
                return Result.Failure("User already has this role");
            }

            // Assign role
            user.AssignRole(request.RoleId, request.AssignedBy, request.ExpiresAt);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = Domain.Entities.AuditLog.CreateAdminAction(
                request.UserId,
                "ROLE_ASSIGNED",
                "USER_MANAGEMENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"userId\":\"{request.UserId}\",\"roleId\":\"{request.RoleId}\",\"roleName\":\"{role.Name}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", role.Name, request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
            return Result.Failure("An error occurred while assigning the role");
        }
    }
}

public record RemoveRoleCommand(
    Guid UserId,
    Guid RoleId,
    string? RemovedBy = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<RemoveRoleCommandHandler> _logger;

    public RemoveRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<RemoveRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                return Result.Failure("Role not found");
            }

            // Check if user has this role
            if (!user.HasRole(request.RoleId))
            {
                return Result.Failure("User does not have this role");
            }

            // Remove role
            user.RemoveRole(request.RoleId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create audit log
            var auditLog = Domain.Entities.AuditLog.CreateAdminAction(
                request.UserId,
                "ROLE_REMOVED",
                "USER_MANAGEMENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"userId\":\"{request.UserId}\",\"roleId\":\"{request.RoleId}\",\"roleName\":\"{role.Name}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", role.Name, request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);
            return Result.Failure("An error occurred while removing the role");
        }
    }
}
