using Event.Domain.Common;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common.Models;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation with ETag support for optimistic concurrency
/// </summary>
public abstract class ETagRepository<T> : BaseRepository<T>, IETagRepository<T> 
    where T : BaseEntity, IETaggable
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

        var etag = entity.CurrentETag ?? Event.Domain.ValueObjects.ETag.Generate();
        return (entity, etag);
    }

    public virtual async Task UpdateWithETagAsync(T entity, ETag expectedETag, CancellationToken cancellationToken = default)
    {
        var trackedEntity = await Context.Set<T>().FindAsync(new object[] { entity.Id }, cancellationToken);
        if (trackedEntity == null)
        {
            throw new InvalidOperationException($"Entity with ID {entity.Id} not found");
        }

        // Check if ETag matches
        var currentETag = trackedEntity.CurrentETag;
        if (currentETag?.Value != expectedETag.Value)
        {
            _logger.LogWarning("ETag mismatch for entity {EntityId}. Expected: {ExpectedETag}, Current: {CurrentETag}", 
                entity.Id, expectedETag.Value, currentETag?.Value);
            throw new InvalidOperationException($"ETag mismatch for entity {entity.Id}. Expected: {expectedETag.Value}, Current: {currentETag?.Value}");
        }

        // Update entity properties (excluding primary key and ETag)
        Context.Entry(trackedEntity).CurrentValues.SetValues(entity);
        
        // Update ETag
        trackedEntity.UpdateETag();
        trackedEntity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected for entity {EntityId}", entity.Id);
            throw new InvalidOperationException($"Concurrency conflict detected for entity {entity.Id}", ex);
        }
    }

    public virtual async Task UpdateWithETagAsync(T entity, string expectedETagValue, CancellationToken cancellationToken = default)
    {
        var expectedETag = ETag.Parse(expectedETagValue, typeof(T).Name, entity.Id.ToString());
        await UpdateWithETagAsync(entity, expectedETag, cancellationToken);
    }

    public virtual async Task DeleteWithETagAsync(Guid id, ETag expectedETag, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Entity with ID {id} not found");
        }

        // Check if ETag matches
        if (entity.CurrentETag?.Value != expectedETag.Value)
        {
            _logger.LogWarning("ETag mismatch for entity {EntityId}. Expected: {ExpectedETag}, Current: {CurrentETag}", 
                id, expectedETag.Value, entity.CurrentETag?.Value);
            throw new InvalidOperationException($"ETag mismatch for entity {id}. Expected: {expectedETag.Value}, Current: {entity.CurrentETag?.Value}");
        }

        Context.Set<T>().Remove(entity);

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected while deleting entity {EntityId}", id);
            throw new InvalidOperationException($"Concurrency conflict detected while deleting entity {id}", ex);
        }
    }

    public virtual async Task DeleteWithETagAsync(Guid id, string expectedETagValue, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return;
        
        var expectedETag = ETag.Parse(expectedETagValue, typeof(T).Name, id.ToString());
        await DeleteWithETagAsync(id, expectedETag, cancellationToken);
    }

    public virtual async Task<bool> IsETagValidAsync(Guid id, ETag etag, CancellationToken cancellationToken = default)
    {
        var currentETag = await GetETagAsync(id, cancellationToken);
        return currentETag?.Value == etag.Value;
    }

    public virtual async Task<bool> IsETagValidAsync(Guid id, string etagValue, CancellationToken cancellationToken = default)
    {
        var etag = ETag.Parse(etagValue, typeof(T).Name, id.ToString());
        return await IsETagValidAsync(id, etag, cancellationToken);
    }

    public virtual async Task<ETag?> GetETagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.Set<T>().AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.CurrentETag })
            .FirstOrDefaultAsync(cancellationToken);
        
        return entity?.CurrentETag;
    }

    public virtual async Task<Dictionary<Guid, (T Entity, ETag ETag)>> GetManyWithETagsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default)
    {
        var entities = await Context.Set<T>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        return entities.ToDictionary(
            e => e.Id,
            e => (e, e.CurrentETag ?? Event.Domain.ValueObjects.ETag.Generate()));
    }

    public virtual async Task UpdateManyWithETagsAsync(
        Dictionary<T, ETag> entitiesWithETags, 
        CancellationToken cancellationToken = default)
    {
        foreach (var kvp in entitiesWithETags)
        {
            await UpdateWithETagAsync(kvp.Key, kvp.Value, cancellationToken);
        }
    }

    protected virtual Event.Domain.ValueObjects.ETag GenerateEntityETag(T entity)
    {
        // Generate ETag based on entity state (can be overridden by derived classes)
        return Event.Domain.ValueObjects.ETag.Generate();
    }
}
