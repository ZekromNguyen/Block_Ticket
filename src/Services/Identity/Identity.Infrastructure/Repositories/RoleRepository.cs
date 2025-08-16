using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(IdentityDbContext context, ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.IsSystemRole)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetByTypeAsync(RoleType type, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.Type == type)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == userId && ur.IsValid())
            .Select(ur => ur.Role)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AnyAsync(r => r.Name == name, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Roles.AddAsync(role, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Role {RoleName} added successfully", role.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role {RoleName}", role.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Role {RoleName} updated successfully", role.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleName}", role.Name);
            throw;
        }
    }

    public async Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Role {RoleName} deleted successfully", role.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleName}", role.Name);
            throw;
        }
    }
}

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserRoleRepository> _logger;

    public UserRoleRepository(IdentityDbContext context, ILogger<UserRoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);
    }

    public async Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .OrderByDescending(ur => ur.Role.Priority)
            .ThenBy(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.RoleId == roleId && ur.IsValid())
            .OrderBy(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserRole>> GetExpiredRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.IsActive && ur.ExpiresAt.HasValue && ur.ExpiresAt.Value <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.UserRoles.AddAsync(userRole, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("UserRole {UserRoleId} added successfully", userRole.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user role {UserRoleId}", userRole.Id);
            throw;
        }
    }

    public async Task UpdateAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserRoles.Update(userRole);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("UserRole {UserRoleId} updated successfully", userRole.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role {UserRoleId}", userRole.Id);
            throw;
        }
    }

    public async Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("UserRole {UserRoleId} deleted successfully", userRole.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user role {UserRoleId}", userRole.Id);
            throw;
        }
    }

    public async Task CleanupExpiredRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredRoles = await GetExpiredRolesAsync(cancellationToken);
            foreach (var expiredRole in expiredRoles)
            {
                expiredRole.Deactivate();
            }

            if (expiredRoles.Any())
            {
                _context.UserRoles.UpdateRange(expiredRoles);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} expired user roles", expiredRoles.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired user roles");
            throw;
        }
    }
}
