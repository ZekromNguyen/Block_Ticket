namespace Event.Application.Interfaces.Application;

/// <summary>
/// Interface for entities that support ETag-based optimistic concurrency control
/// </summary>
public interface IETaggable
{
    /// <summary>
    /// Gets the current ETag value for the entity
    /// </summary>
    string ETag { get; }

    /// <summary>
    /// Gets the timestamp when the ETag was generated
    /// </summary>
    DateTime ETagGeneratedAt { get; }

    /// <summary>
    /// Updates the ETag to a new value
    /// </summary>
    /// <param name="newETag">New ETag value</param>
    void UpdateETag(string newETag);

    /// <summary>
    /// Validates if the provided ETag matches the current ETag
    /// </summary>
    /// <param name="providedETag">ETag to validate</param>
    /// <returns>True if ETags match, false otherwise</returns>
    bool ValidateETag(string? providedETag);

    /// <summary>
    /// Generates a new ETag based on current entity state
    /// </summary>
    /// <returns>New ETag string</returns>
    string GenerateETag();
}

/// <summary>
/// Interface for services that provide ETag functionality
/// </summary>
public interface IETagService
{
    /// <summary>
    /// Generates an ETag for the given object
    /// </summary>
    /// <param name="obj">Object to generate ETag for</param>
    /// <returns>Generated ETag string</returns>
    string GenerateETag(object obj);

    /// <summary>
    /// Generates an ETag from a version number
    /// </summary>
    /// <param name="version">Version number</param>
    /// <returns>Generated ETag string</returns>
    string GenerateETag(long version);

    /// <summary>
    /// Generates an ETag from a hash
    /// </summary>
    /// <param name="hash">Hash bytes</param>
    /// <returns>Generated ETag string</returns>
    string GenerateETag(byte[] hash);

    /// <summary>
    /// Validates if two ETags match
    /// </summary>
    /// <param name="etag1">First ETag</param>
    /// <param name="etag2">Second ETag</param>
    /// <returns>True if ETags match</returns>
    bool ValidateETag(string? etag1, string? etag2);

    /// <summary>
    /// Parses an ETag from an HTTP header value
    /// </summary>
    /// <param name="headerValue">HTTP header value</param>
    /// <returns>Parsed ETag or null if invalid</returns>
    string? ParseETagFromHeader(string? headerValue);

    /// <summary>
    /// Formats an ETag for HTTP header use
    /// </summary>
    /// <param name="etag">ETag to format</param>
    /// <returns>Formatted ETag header value</returns>
    string FormatETagForHeader(string etag);
}

/// <summary>
/// Exception thrown when ETag validation fails
/// </summary>
public class ETagMismatchException : Exception
{
    /// <summary>
    /// Expected ETag value
    /// </summary>
    public string? ExpectedETag { get; }

    /// <summary>
    /// Actual ETag value
    /// </summary>
    public string? ActualETag { get; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public string? ResourceId { get; }

    public ETagMismatchException(string? expectedETag, string? actualETag, string? resourceId = null)
        : base($"ETag mismatch for resource {resourceId ?? "unknown"}. Expected: {expectedETag}, Actual: {actualETag}")
    {
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
        ResourceId = resourceId;
    }

    public ETagMismatchException(string message, string? expectedETag, string? actualETag, string? resourceId = null)
        : base(message)
    {
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
        ResourceId = resourceId;
    }

    public ETagMismatchException(string message, Exception innerException, string? expectedETag, string? actualETag, string? resourceId = null)
        : base(message, innerException)
    {
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
        ResourceId = resourceId;
    }
}

/// <summary>
/// ETag configuration options
/// </summary>
public class ETagOptions
{
    /// <summary>
    /// Whether ETags are enabled globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// ETag generation algorithm (MD5, SHA1, SHA256)
    /// </summary>
    public string Algorithm { get; set; } = "SHA256";

    /// <summary>
    /// Whether to use weak ETags
    /// </summary>
    public bool UseWeakETags { get; set; } = false;

    /// <summary>
    /// Whether to include timestamp in ETag generation
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Cache duration for ETags in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Whether to validate ETags on reads
    /// </summary>
    public bool ValidateOnRead { get; set; } = true;

    /// <summary>
    /// Whether to require ETags for updates
    /// </summary>
    public bool RequireForUpdates { get; set; } = true;

    /// <summary>
    /// Whether to require ETags for deletes
    /// </summary>
    public bool RequireForDeletes { get; set; } = true;
}
