using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IScopeRepository
{
    Task<Scope?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Scope?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetDefaultScopesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetDiscoveryScopesAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Scope scope, CancellationToken cancellationToken = default);
    Task UpdateAsync(Scope scope, CancellationToken cancellationToken = default);
    Task DeleteAsync(Scope scope, CancellationToken cancellationToken = default);
}
