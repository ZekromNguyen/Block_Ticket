using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetByTypeAsync(RoleType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
}

public interface IUserRoleRepository
{
    Task<UserRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRole>> GetExpiredRolesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task CleanupExpiredRolesAsync(CancellationToken cancellationToken = default);
}
