using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class ScopeRepository : IScopeRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<ScopeRepository> _logger;

    public ScopeRepository(IdentityDbContext context, ILogger<ScopeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Scope?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Scope?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Scope>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Scope>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .Where(s => names.Contains(s.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Scope>> GetDefaultScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .Where(s => s.IsDefault)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Scope>> GetDiscoveryScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .Where(s => s.ShowInDiscoveryDocument)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .AnyAsync(s => s.Name == name, cancellationToken);
    }

    public async Task AddAsync(Scope scope, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Scopes.AddAsync(scope, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Scope {ScopeName} added successfully", scope.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding scope {ScopeName}", scope.Name);
            throw;
        }
    }

    public async Task UpdateAsync(Scope scope, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Scopes.Update(scope);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Scope {ScopeName} updated successfully", scope.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scope {ScopeName}", scope.Name);
            throw;
        }
    }

    public async Task DeleteAsync(Scope scope, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Scopes.Remove(scope);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Scope {ScopeName} deleted successfully", scope.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scope {ScopeName}", scope.Name);
            throw;
        }
    }
}
