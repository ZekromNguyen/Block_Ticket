using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly IdentityDbContext _context;

    public PasswordHistoryRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PasswordHistory>> GetUserPasswordHistoryAsync(
        Guid userId, 
        int? count = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.PasswordHistory
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt);

        if (count.HasValue && count.Value > 0)
        {
            return await query.Take(count.Value).ToListAsync(cancellationToken);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> IsPasswordInHistoryAsync(
        Guid userId, 
        string passwordHash, 
        int historyCount, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.PasswordHistory
            .Where(ph => ph.UserId == userId && ph.PasswordHash == passwordHash)
            .OrderByDescending(ph => ph.CreatedAt);

        if (historyCount > 0)
        {
            return await query.Take(historyCount).AnyAsync(cancellationToken);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordHistory passwordHistory, CancellationToken cancellationToken = default)
    {
        await _context.PasswordHistory.AddAsync(passwordHistory, cancellationToken);
    }

    public async Task RemoveOldEntriesAsync(
        Guid userId, 
        int retentionDays, 
        int maxHistoryCount, 
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        
        // Get all password history for user ordered by creation date (newest first)
        var allHistory = await _context.PasswordHistory
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync(cancellationToken);

        if (!allHistory.Any()) return;

        // Keep the most recent entries up to maxHistoryCount
        var entriesToKeep = allHistory.Take(maxHistoryCount).ToList();

        // Also keep any entries within retention period that aren't already kept
        var entriesWithinRetention = allHistory
            .Skip(maxHistoryCount)
            .Where(h => h.CreatedAt > cutoffDate)
            .ToList();

        var finalEntriesToKeep = entriesToKeep.Union(entriesWithinRetention).ToHashSet();
        var entriesToRemove = allHistory.Where(e => !finalEntriesToKeep.Contains(e)).ToList();

        if (entriesToRemove.Any())
        {
            _context.PasswordHistory.RemoveRange(entriesToRemove);
        }
    }

    public async Task<int> GetPasswordHistoryCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordHistory
            .Where(ph => ph.UserId == userId)
            .CountAsync(cancellationToken);
    }
}
