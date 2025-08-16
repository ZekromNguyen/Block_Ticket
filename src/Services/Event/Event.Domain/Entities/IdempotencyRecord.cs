using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents an idempotency record for request deduplication
/// </summary>
public class IdempotencyRecord : BaseEntity
{
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestPath { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = string.Empty;
    public string? RequestBody { get; private set; }
    public string? RequestHeaders { get; private set; }
    public string? ResponseBody { get; private set; }
    public int ResponseStatusCode { get; private set; }
    public string? ResponseHeaders { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? UserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string RequestId { get; private set; } = string.Empty;

    // For EF Core
    private IdempotencyRecord() { }

    public IdempotencyRecord(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        string? requestHeaders,
        string? userId = null,
        Guid? organizationId = null,
        string? requestId = null,
        TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be empty", nameof(idempotencyKey));
        
        if (string.IsNullOrWhiteSpace(requestPath))
            throw new ArgumentException("Request path cannot be empty", nameof(requestPath));
        
        if (string.IsNullOrWhiteSpace(httpMethod))
            throw new ArgumentException("HTTP method cannot be empty", nameof(httpMethod));

        IdempotencyKey = idempotencyKey;
        RequestPath = requestPath;
        HttpMethod = httpMethod.ToUpperInvariant();
        RequestBody = requestBody;
        RequestHeaders = requestHeaders;
        UserId = userId;
        OrganizationId = organizationId;
        RequestId = requestId ?? Guid.NewGuid().ToString();
        ProcessedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(24));
    }

    public void SetResponse(
        string? responseBody,
        int statusCode,
        string? responseHeaders)
    {
        ResponseBody = responseBody;
        ResponseStatusCode = statusCode;
        ResponseHeaders = responseHeaders;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public bool IsProcessing() => ResponseStatusCode == 0;

    public bool IsSuccessful() => ResponseStatusCode >= 200 && ResponseStatusCode < 300;
}
