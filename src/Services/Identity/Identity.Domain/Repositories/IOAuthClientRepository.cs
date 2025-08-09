using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IOAuthClientRepository
{
    Task<OAuthClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OAuthClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OAuthClient>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OAuthClient>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default);
    Task AddAsync(OAuthClient client, CancellationToken cancellationToken = default);
    Task UpdateAsync(OAuthClient client, CancellationToken cancellationToken = default);
    Task DeleteAsync(OAuthClient client, CancellationToken cancellationToken = default);
}
