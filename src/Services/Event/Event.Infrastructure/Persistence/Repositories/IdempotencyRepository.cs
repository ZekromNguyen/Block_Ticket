using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for idempotency records
/// </summary>
public class IdempotencyRepository : BaseRepository<IdempotencyRecord>, IIdempotencyRepository
{
    private readonly ILogger<IdempotencyRepository> _logger;

    public IdempotencyRepository(EventDbContext context, ILogger<IdempotencyRepository> logger) 
        : base(context)
    {
        _logger = logger;
    }

    public async Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await Context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<bool> ExistsByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await Context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<(IdempotencyRecord Record, bool IsNew)> GetOrCreateAsync(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        string? requestHeaders,
        string? userId = null,
        Guid? organizationId = null,
        string? requestId = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Try to get existing record first
            var existing = await Context.Set<IdempotencyRecord>()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existing != null)
            {
                await transaction.CommitAsync(cancellationToken);
                return (existing, false);
            }

            // Create new record
            var newRecord = new IdempotencyRecord(
                idempotencyKey,
                requestPath,
                httpMethod,
                requestBody,
                requestHeaders,
                userId,
                organizationId,
                requestId,
                ttl);

            Context.Set<IdempotencyRecord>().Add(newRecord);
            await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Created new idempotency record for key: {IdempotencyKey}", idempotencyKey);
            return (newRecord, true);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Handle race condition - another thread created the record
            await transaction.RollbackAsync(cancellationToken);
            
            var existing = await GetByKeyAsync(idempotencyKey, cancellationToken);
            if (existing != null)
            {
                _logger.LogDebug("Found existing idempotency record for key: {IdempotencyKey}", idempotencyKey);
                return (existing, false);
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateResponseAsync(
        string idempotencyKey,
        string? responseBody,
        int statusCode,
        string? responseHeaders,
        CancellationToken cancellationToken = default)
    {
        var record = await Context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (record == null)
        {
            _logger.LogWarning("Attempted to update response for non-existent idempotency key: {IdempotencyKey}", idempotencyKey);
            return;
        }

        record.SetResponse(responseBody, statusCode, responseHeaders);
        await Context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated response for idempotency key: {IdempotencyKey} with status: {StatusCode}", 
            idempotencyKey, statusCode);
    }

    public async Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredRecords = await Context.Set<IdempotencyRecord>()
            .Where(x => x.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredRecords.Any())
        {
            Context.Set<IdempotencyRecord>().RemoveRange(expiredRecords);
            await Context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} expired idempotency records", expiredRecords.Count);
        }

        return expiredRecords.Count;
    }

    public async Task<IEnumerable<IdempotencyRecord>> GetByUserAsync(
        string userId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ProcessedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<IdempotencyRecord>> GetByOrganizationAsync(
        Guid organizationId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.ProcessedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key") == true ||
               ex.InnerException?.Message.Contains("UNIQUE constraint") == true;
    }
}
