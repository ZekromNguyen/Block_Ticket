using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class OAuthClientRepository : IOAuthClientRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<OAuthClientRepository> _logger;

    public OAuthClientRepository(IdentityDbContext context, ILogger<OAuthClientRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OAuthClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<OAuthClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<IEnumerable<OAuthClient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OAuthClient>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .AnyAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task AddAsync(OAuthClient client, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OAuthClients.AddAsync(client, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("OAuth client {ClientId} added successfully", client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding OAuth client {ClientId}", client.ClientId);
            throw;
        }
    }

    public async Task UpdateAsync(OAuthClient client, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.OAuthClients.Update(client);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("OAuth client {ClientId} updated successfully", client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating OAuth client {ClientId}", client.ClientId);
            throw;
        }
    }

    public async Task DeleteAsync(OAuthClient client, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.OAuthClients.Remove(client);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("OAuth client {ClientId} deleted successfully", client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting OAuth client {ClientId}", client.ClientId);
            throw;
        }
    }
}
