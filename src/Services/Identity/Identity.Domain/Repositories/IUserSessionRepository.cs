using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserSession session, CancellationToken cancellationToken = default);
    Task DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task EndAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
