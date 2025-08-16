using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IdentityDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .Include(u => u.MfaDevices)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .Include(u => u.MfaDevices)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByWalletAddressAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .Include(u => u.MfaDevices)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.WalletAddress == walletAddress, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByUserTypeAsync(UserType userType, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.UserType == userType)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.WalletAddress == walletAddress, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Users.AddAsync(user, cancellationToken);
            _logger.LogDebug("User {UserId} added to context", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId}", user.Id);
            throw;
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Users.Update(user);
            _logger.LogDebug("User {UserId} updated in context", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Users.Remove(user);
            _logger.LogDebug("User {UserId} marked for deletion in context", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Status == UserStatus.LockedOut && 
                       u.LockedOutUntil.HasValue && 
                       u.LockedOutUntil.Value <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersWithExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .Where(u => u.Sessions.Any(s => s.ExpiresAt <= DateTime.UtcNow && s.EndedAt == null))
            .ToListAsync(cancellationToken);
    }
}
