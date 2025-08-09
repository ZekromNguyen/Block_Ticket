using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UserSessionRepository> _logger;

    public UserSessionRepository(IdentityDbContext context, ILogger<UserSessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId && 
                       s.EndedAt == null && 
                       s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(s => s.EndedAt == null && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.UserSessions.AddAsync(session, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Session {SessionId} added successfully", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding session {SessionId}", session.Id);
            throw;
        }
    }

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Session {SessionId} updated successfully", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", session.Id);
            throw;
        }
    }

    public async Task DeleteAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Session {SessionId} deleted successfully", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", session.Id);
            throw;
        }
    }

    public async Task DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredSessions = await GetExpiredSessionsAsync(cancellationToken);
            _context.UserSessions.RemoveRange(expiredSessions);
            var deletedCount = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} expired sessions", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired sessions");
            throw;
        }
    }

    public async Task EndAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSessions = await GetActiveSessionsByUserIdAsync(userId, cancellationToken);
            foreach (var session in activeSessions)
            {
                session.End();
            }
            
            if (activeSessions.Any())
            {
                _context.UserSessions.UpdateRange(activeSessions);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Ended {Count} active sessions for user {UserId}", activeSessions.Count(), userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending all sessions for user {UserId}", userId);
            throw;
        }
    }
}
