using Event.Domain.Common;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation with ETag support for optimistic concurrency
/// </summary>
public abstract class ETagRepository<T> : BaseRepository<T>, IETagRepository<T> 
    where T : class, IETaggable
{
    private readonly ILogger<ETagRepository<T>> _logger;

    protected ETagRepository(EventDbContext context, ILogger<ETagRepository<T>> logger) : base(context)
    {
        _logger = logger;
    }

    public virtual async Task<(T? Entity, ETag? ETag)> GetWithETagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (null, null);
        }

        return (entity, entity.CurrentETag);
    }

    public virtual async Task UpdateWithETagAsync(T entity, ETag expectedETag, CancellationToken cancellationToken = default)
    {
        // Validate ETag before updating
        entity.ValidateETag(expectedETag);

        // Update the entity's ETag
        entity.UpdateETag();

        // Perform the update
        await UpdateAsync(entity, cancellationToken);

        _logger.LogDebug("Updated {EntityType} {EntityId} with ETag validation. New ETag: {NewETag}", 
            typeof(T).Name, entity.GetEntityId(), entity.CurrentETag.Value);
    }

    public virtual async Task UpdateWithETagAsync(T entity, string expectedETagValue, CancellationToken cancellationToken = default)
    {
        // Validate ETag before updating
        entity.ValidateETag(expectedETagValue);

        // Update the entity's ETag
        entity.UpdateETag();

        // Perform the update
        await UpdateAsync(entity, cancellationToken);

        _logger.LogDebug("Updated {EntityType} {EntityId} with ETag validation. New ETag: {NewETag}", 
            typeof(T).Name, entity.GetEntityId(), entity.CurrentETag.Value);
    }

    public virtual async Task DeleteWithETagAsync(Guid id, ETag expectedETag, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} with ID {id} not found");
        }

        // Validate ETag before deleting
        entity.ValidateETag(expectedETag);

        // Perform the deletion
        await DeleteAsync(entity, cancellationToken);

        _logger.LogDebug("Deleted {EntityType} {EntityId} with ETag validation", typeof(T).Name, id);
    }

    public virtual async Task DeleteWithETagAsync(Guid id, string expectedETagValue, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} with ID {id} not found");
        }

        // Validate ETag before deleting
        entity.ValidateETag(expectedETagValue);

        // Perform the deletion
        await DeleteAsync(entity, cancellationToken);

        _logger.LogDebug("Deleted {EntityType} {EntityId} with ETag validation", typeof(T).Name, id);
    }

    public virtual async Task<bool> IsETagValidAsync(Guid id, ETag etag, CancellationToken cancellationToken = default)
    {
        var currentETag = await GetETagAsync(id, cancellationToken);
        return currentETag?.Matches(etag) == true;
    }

    public virtual async Task<bool> IsETagValidAsync(Guid id, string etagValue, CancellationToken cancellationToken = default)
    {
        var currentETag = await GetETagAsync(id, cancellationToken);
        return currentETag?.Matches(etagValue) == true;
    }

    public virtual async Task<ETag?> GetETagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var etagValue = await _context.Set<T>()
            .Where(e => e.Id == id)
            .Select(e => e.ETagValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(etagValue))
        {
            return null;
        }

        return ETag.FromHash(typeof(T).Name, id.ToString(), etagValue);
    }

    public virtual async Task<Dictionary<Guid, (T Entity, ETag ETag)>> GetManyWithETagsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Set<T>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        return entities.ToDictionary(
            e => e.Id,
            e => (e, e.CurrentETag)
        );
    }

    public virtual async Task UpdateManyWithETagsAsync(
        Dictionary<T, ETag> entitiesWithETags, 
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (entity, expectedETag) in entitiesWithETags)
            {
                // Validate each ETag
                entity.ValidateETag(expectedETag);
                
                // Update ETag
                entity.UpdateETag();
            }

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Bulk updated {Count} {EntityType} entities with ETag validation", 
                entitiesWithETags.Count, typeof(T).Name);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>
/// Inventory-specific ETag repository implementation
/// </summary>
public abstract class InventoryETagRepository<T> : ETagRepository<T>, IInventoryETagRepository<T> 
    where T : class, IETaggable
{
    private readonly ILogger<InventoryETagRepository<T>> _logger;

    protected InventoryETagRepository(EventDbContext context, ILogger<InventoryETagRepository<T>> logger) 
        : base(context, logger)
    {
        _logger = logger;
    }

    public virtual async Task<bool> TryUpdateInventoryWithETagAsync(
        Guid id,
        ETag expectedETag,
        Func<T, bool> inventoryUpdate,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Get entity with row lock for atomic update
            var entity = await _context.Set<T>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                return false;
            }

            // Validate ETag
            if (!entity.CurrentETag.Matches(expectedETag))
            {
                _logger.LogWarning("ETag mismatch during inventory update for {EntityType} {EntityId}. Expected: {Expected}, Actual: {Actual}",
                    typeof(T).Name, id, expectedETag.Value, entity.CurrentETag.Value);
                return false;
            }

            // Apply inventory update
            var updateSuccessful = inventoryUpdate(entity);
            if (!updateSuccessful)
            {
                return false;
            }

            // Update ETag and save
            entity.UpdateETag();
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Successfully updated inventory for {EntityType} {EntityId} with ETag validation", 
                typeof(T).Name, id);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update inventory for {EntityType} {EntityId}", typeof(T).Name, id);
            return false;
        }
    }

    public abstract Task<(int Available, int Sold, ETag ETag)?> GetInventorySummaryWithETagAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    public virtual async Task<Dictionary<Guid, bool>> TryUpdateInventoryBulkWithETagsAsync(
        Dictionary<Guid, (ETag ExpectedETag, Func<T, bool> Update)> updates,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<Guid, bool>();
        
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ids = updates.Keys.ToList();
            var entities = await _context.Set<T>()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync(cancellationToken);

            var entityDict = entities.ToDictionary(e => e.Id);

            foreach (var (id, (expectedETag, update)) in updates)
            {
                if (!entityDict.TryGetValue(id, out var entity))
                {
                    results[id] = false;
                    continue;
                }

                // Validate ETag
                if (!entity.CurrentETag.Matches(expectedETag))
                {
                    results[id] = false;
                    continue;
                }

                // Apply update
                var updateSuccessful = update(entity);
                if (!updateSuccessful)
                {
                    results[id] = false;
                    continue;
                }

                // Update ETag
                entity.UpdateETag();
                results[id] = true;
            }

            // Only commit if all updates succeeded
            if (results.Values.All(success => success))
            {
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogDebug("Bulk inventory update succeeded for {Count} {EntityType} entities", 
                    results.Count, typeof(T).Name);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                
                _logger.LogWarning("Bulk inventory update failed for some {EntityType} entities. Success rate: {SuccessCount}/{TotalCount}",
                    typeof(T).Name, results.Values.Count(s => s), results.Count);
            }

            return results;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Bulk inventory update failed for {EntityType}", typeof(T).Name);
            
            // Return all failures
            return updates.Keys.ToDictionary(id => id, _ => false);
        }
    }

    public virtual async Task<IEnumerable<(T Entity, ETag ETag)>> GetModifiedSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Set<T>()
            .Where(e => e.ETagUpdatedAt > since)
            .OrderBy(e => e.ETagUpdatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => (e, e.CurrentETag));
    }

    public virtual async Task<Dictionary<Guid, bool>> ValidateETagsBulkAsync(
        Dictionary<Guid, ETag> entityETags,
        CancellationToken cancellationToken = default)
    {
        var ids = entityETags.Keys.ToList();
        var currentETags = await _context.Set<T>()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.ETagValue })
            .ToListAsync(cancellationToken);

        var results = new Dictionary<Guid, bool>();

        foreach (var (id, expectedETag) in entityETags)
        {
            var currentETagData = currentETags.FirstOrDefault(e => e.Id == id);
            if (currentETagData == null)
            {
                results[id] = false;
                continue;
            }

            var currentETag = ETag.FromHash(typeof(T).Name, id.ToString(), currentETagData.ETagValue);
            results[id] = currentETag.Matches(expectedETag);
        }

        return results;
    }
}
