using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IPasswordHistoryRepository
{
    Task<IEnumerable<PasswordHistory>> GetUserPasswordHistoryAsync(
        Guid userId, 
        int? count = null, 
        CancellationToken cancellationToken = default);

    Task<bool> IsPasswordInHistoryAsync(
        Guid userId, 
        string passwordHash, 
        int historyCount, 
        CancellationToken cancellationToken = default);

    Task AddAsync(PasswordHistory passwordHistory, CancellationToken cancellationToken = default);

    Task RemoveOldEntriesAsync(
        Guid userId, 
        int retentionDays, 
        int maxHistoryCount, 
        CancellationToken cancellationToken = default);

    Task<int> GetPasswordHistoryCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
