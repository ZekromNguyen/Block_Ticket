using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class ReferenceTokenRepository : IReferenceTokenRepository
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReferenceTokenRepository> _logger;

    public ReferenceTokenRepository(IServiceScopeFactory scopeFactory, ILogger<ReferenceTokenRepository> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ReferenceToken?> GetByTokenIdAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            return await context.ReferenceTokens
                .FirstOrDefaultAsync(t => t.TokenId == tokenId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reference token by ID {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<ReferenceToken?> GetValidTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            return await context.ReferenceTokens
                .FirstOrDefaultAsync(t => t.TokenId == tokenId &&
                                         !t.IsRevoked &&
                                         t.ExpiresAt > DateTime.UtcNow,
                                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid reference token {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<IEnumerable<ReferenceToken>> GetUserTokensAsync(Guid userId, string? tokenType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            var query = context.ReferenceTokens.Where(t => t.UserId == userId);

            if (!string.IsNullOrEmpty(tokenType))
            {
                query = query.Where(t => t.TokenType == tokenType);
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tokens for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ReferenceToken>> GetSessionTokensAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            return await context.ReferenceTokens
                .Where(t => t.SessionId == sessionId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session tokens for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<IEnumerable<ReferenceToken>> GetExpiredTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            return await context.ReferenceTokens
                .Where(t => t.ExpiresAt <= cutoffDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired tokens before {CutoffDate}", cutoffDate);
            throw;
        }
    }

    public async Task AddAsync(ReferenceToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            await context.ReferenceTokens.AddAsync(token, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Reference token {TokenId} added successfully", token.TokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reference token {TokenId}", token.TokenId);
            throw;
        }
    }

    public async Task UpdateAsync(ReferenceToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            context.ReferenceTokens.Update(token);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Reference token {TokenId} updated successfully", token.TokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reference token {TokenId}", token.TokenId);
            throw;
        }
    }

    public async Task DeleteAsync(ReferenceToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            context.ReferenceTokens.Remove(token);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Reference token {TokenId} deleted successfully", token.TokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reference token {TokenId}", token.TokenId);
            throw;
        }
    }

    public async Task DeleteExpiredTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            var expiredTokens = await context.ReferenceTokens
                .Where(t => t.ExpiresAt <= cutoffDate)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                context.ReferenceTokens.RemoveRange(expiredTokens);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted {Count} expired reference tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired tokens before {CutoffDate}", cutoffDate);
            throw;
        }
    }

    public async Task RevokeTokenAsync(string tokenId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetByTokenIdAsync(tokenId, cancellationToken);
            if (token != null && !token.IsRevoked)
            {
                token.Revoke(revokedBy, reason);
                await UpdateAsync(token, cancellationToken);
                _logger.LogInformation("Reference token {TokenId} revoked", tokenId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking reference token {TokenId}", tokenId);
            throw;
        }
    }

    public async Task RevokeUserTokensAsync(Guid userId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            var tokens = await context.ReferenceTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.Revoke(revokedBy, reason);
            }

            if (tokens.Any())
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Revoked {Count} tokens for user {UserId}", tokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking user tokens for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeSessionTokensAsync(string sessionId, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            var tokens = await context.ReferenceTokens
                .Where(t => t.SessionId == sessionId && !t.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.Revoke(revokedBy, reason);
            }

            if (tokens.Any())
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Revoked {Count} tokens for session {SessionId}", tokens.Count, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session tokens for session {SessionId}", sessionId);
            throw;
        }
    }
}
