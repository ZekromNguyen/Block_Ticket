using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IReferenceTokenRepository
{
    Task<ReferenceToken?> GetByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default);
    Task<ReferenceToken?> GetValidTokenAsync(string tokenId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReferenceToken>> GetUserTokensAsync(Guid userId, string? tokenType = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReferenceToken>> GetSessionTokensAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReferenceToken>> GetExpiredTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    
    Task AddAsync(ReferenceToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReferenceToken token, CancellationToken cancellationToken = default);
    Task DeleteAsync(ReferenceToken token, CancellationToken cancellationToken = default);
    Task DeleteExpiredTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    
    Task RevokeTokenAsync(string tokenId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default);
    Task RevokeUserTokensAsync(Guid userId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default);
    Task RevokeSessionTokensAsync(string sessionId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default);
}
