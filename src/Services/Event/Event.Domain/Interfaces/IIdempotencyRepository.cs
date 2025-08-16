using Event.Domain.Entities;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for idempotency records
/// </summary>
public interface IIdempotencyRepository : IRepository<IdempotencyRecord>
{
    /// <summary>
    /// Gets an idempotency record by key
    /// </summary>
    Task<IdempotencyRecord?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an idempotency key exists
    /// </summary>
    Task<bool> ExistsByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or gets an existing idempotency record atomically
    /// </summary>
    Task<(IdempotencyRecord Record, bool IsNew)> GetOrCreateAsync(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        string? requestHeaders,
        string? userId = null,
        Guid? organizationId = null,
        string? requestId = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the response for an idempotency record
    /// </summary>
    Task UpdateResponseAsync(
        string idempotencyKey,
        string? responseBody,
        int statusCode,
        string? responseHeaders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired idempotency records
    /// </summary>
    Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets idempotency records by user ID
    /// </summary>
    Task<IEnumerable<IdempotencyRecord>> GetByUserAsync(
        string userId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets idempotency records by organization ID
    /// </summary>
    Task<IEnumerable<IdempotencyRecord>> GetByOrganizationAsync(
        Guid organizationId,
        int limit = 100,
        CancellationToken cancellationToken = default);
}
