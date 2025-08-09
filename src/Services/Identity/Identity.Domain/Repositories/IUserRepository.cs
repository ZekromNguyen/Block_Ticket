using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByWalletAddressAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByUserTypeAsync(UserType userType, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(WalletAddress walletAddress, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersWithExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
