using Event.Domain.Entities;

namespace Event.Domain.Services;

/// <summary>
/// Service for handling idempotency logic
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Processes a request with idempotency checking
    /// </summary>
    Task<IdempotencyResult<TResponse>> ProcessRequestAsync<TResponse>(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        string? requestHeaders,
        Func<CancellationToken, Task<TResponse>> requestHandler,
        string? userId = null,
        Guid? organizationId = null,
        string? requestId = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a request is a duplicate
    /// </summary>
    Task<IdempotencyCheckResult> CheckDuplicateAsync(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a request as completed
    /// </summary>
    Task CompleteRequestAsync(
        string idempotencyKey,
        object response,
        int statusCode,
        string? responseHeaders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored response for an idempotency key
    /// </summary>
    Task<TResponse?> GetStoredResponseAsync<TResponse>(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an idempotency key format
    /// </summary>
    bool IsValidIdempotencyKey(string? idempotencyKey);

    /// <summary>
    /// Cleans up expired idempotency records
    /// </summary>
    Task<int> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of idempotency processing
/// </summary>
public class IdempotencyResult<TResponse>
{
    public TResponse Response { get; set; } = default!;
    public bool IsNewRequest { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseHeaders { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Result of idempotency duplicate check
/// </summary>
public class IdempotencyCheckResult
{
    public bool IsDuplicate { get; set; }
    public bool IsProcessing { get; set; }
    public IdempotencyRecord? ExistingRecord { get; set; }
    public string? ConflictReason { get; set; }
}
