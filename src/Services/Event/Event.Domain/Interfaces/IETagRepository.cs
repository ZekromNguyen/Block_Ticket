using Event.Domain.Common;
using Event.Domain.ValueObjects;

namespace Event.Domain.Interfaces;

/// <summary>
/// Interface for repositories that support ETag-based optimistic concurrency
/// </summary>
public interface IETagRepository<T> : IRepository<T> where T : class, IETaggable
{
    /// <summary>
    /// Gets an entity by ID and returns it with its current ETag
    /// </summary>
    Task<(T? Entity, ETag? ETag)> GetWithETagAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity after validating the provided ETag
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="expectedETag">The expected ETag for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ETagMismatchException">Thrown when the ETag doesn't match</exception>
    Task UpdateWithETagAsync(T entity, ETag expectedETag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity after validating the provided ETag string
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="expectedETagValue">The expected ETag value for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ETagMismatchException">Thrown when the ETag doesn't match</exception>
    Task UpdateWithETagAsync(T entity, string expectedETagValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity after validating the provided ETag
    /// </summary>
    /// <param name="id">The ID of the entity to delete</param>
    /// <param name="expectedETag">The expected ETag for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ETagMismatchException">Thrown when the ETag doesn't match</exception>
    Task DeleteWithETagAsync(Guid id, ETag expectedETag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity after validating the provided ETag string
    /// </summary>
    /// <param name="id">The ID of the entity to delete</param>
    /// <param name="expectedETagValue">The expected ETag value for optimistic concurrency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ETagMismatchException">Thrown when the ETag doesn't match</exception>
    Task DeleteWithETagAsync(Guid id, string expectedETagValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provided ETag matches the current entity ETag
    /// </summary>
    Task<bool> IsETagValidAsync(Guid id, ETag etag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provided ETag string matches the current entity ETag
    /// </summary>
    Task<bool> IsETagValidAsync(Guid id, string etagValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current ETag for an entity without loading the full entity
    /// </summary>
    Task<ETag?> GetETagAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities with their ETags
    /// </summary>
    Task<Dictionary<Guid, (T Entity, ETag ETag)>> GetManyWithETagsAsync(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities with ETag validation
    /// </summary>
    Task UpdateManyWithETagsAsync(
        Dictionary<T, ETag> entitiesWithETags, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for repositories that support inventory-specific ETag operations
/// </summary>
public interface IInventoryETagRepository<T> : IETagRepository<T> where T : class, IETaggable
{
    /// <summary>
    /// Atomically updates inventory and ETag in a single database operation
    /// </summary>
    Task<bool> TryUpdateInventoryWithETagAsync(
        Guid id,
        ETag expectedETag,
        Func<T, bool> inventoryUpdate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory summary with ETag for quick availability checks
    /// </summary>
    Task<(int Available, int Sold, ETag ETag)?> GetInventorySummaryWithETagAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inventory update with ETag validation for multiple entities
    /// </summary>
    Task<Dictionary<Guid, bool>> TryUpdateInventoryBulkWithETagsAsync(
        Dictionary<Guid, (ETag ExpectedETag, Func<T, bool> Update)> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities that have been modified since a specific ETag generation time
    /// </summary>
    Task<IEnumerable<(T Entity, ETag ETag)>> GetModifiedSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple ETags in a single database round trip
    /// </summary>
    Task<Dictionary<Guid, bool>> ValidateETagsBulkAsync(
        Dictionary<Guid, ETag> entityETags,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for ETag repositories
/// </summary>
public static class ETagRepositoryExtensions
{
    /// <summary>
    /// Safely updates an entity with ETag validation, returning success status
    /// </summary>
    public static async Task<bool> TryUpdateWithETagAsync<T>(
        this IETagRepository<T> repository,
        T entity,
        ETag expectedETag,
        CancellationToken cancellationToken = default) where T : class, IETaggable
    {
        try
        {
            await repository.UpdateWithETagAsync(entity, expectedETag, cancellationToken);
            return true;
        }
        catch (ETagMismatchException)
        {
            return false;
        }
    }

    /// <summary>
    /// Safely deletes an entity with ETag validation, returning success status
    /// </summary>
    public static async Task<bool> TryDeleteWithETagAsync<T>(
        this IETagRepository<T> repository,
        Guid id,
        ETag expectedETag,
        CancellationToken cancellationToken = default) where T : class, IETaggable
    {
        try
        {
            await repository.DeleteWithETagAsync(id, expectedETag, cancellationToken);
            return true;
        }
        catch (ETagMismatchException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets an entity and validates the ETag in one operation
    /// </summary>
    public static async Task<T?> GetAndValidateETagAsync<T>(
        this IETagRepository<T> repository,
        Guid id,
        ETag expectedETag,
        CancellationToken cancellationToken = default) where T : class, IETaggable
    {
        var (entity, currentETag) = await repository.GetWithETagAsync(id, cancellationToken);
        
        if (entity == null) return null;
        
        if (currentETag == null || !currentETag.Matches(expectedETag))
        {
            throw new ETagMismatchException(
                currentETag?.Value ?? "null",
                expectedETag.Value,
                typeof(T).Name,
                id.ToString()
            );
        }

        return entity;
    }
}
