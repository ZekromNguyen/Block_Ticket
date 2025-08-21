using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Event.Domain.ValueObjects;

/// <summary>
/// Represents an entity tag (ETag) for optimistic concurrency control
/// </summary>
public class ETag : IEquatable<ETag>
{
    public string Value { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }

    private ETag() 
    { 
        Value = null!; 
        EntityType = null!;
        EntityId = null!;
    } // For EF Core

    private ETag(string value, string entityType, string entityId, DateTime? generatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ETag value cannot be null or whitespace.", nameof(value));
        }
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or whitespace.", nameof(entityType));
        }
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or whitespace.", nameof(entityId));
        }

        Value = value;
        EntityType = entityType;
        EntityId = entityId;
        GeneratedAt = generatedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new ETag from entity data
    /// </summary>
    public static ETag FromEntity<T>(T entity, string entityId) where T : class
    {
        var entityType = typeof(T).Name;
        var value = GenerateETagValue(entity);
        return new ETag(value, entityType, entityId);
    }

    /// <summary>
    /// Creates a new ETag from a timestamp and additional data
    /// </summary>
    public static ETag FromTimestamp(string entityType, string entityId, DateTime timestamp, object? additionalData = null)
    {
        var dataToHash = new
        {
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
            AdditionalData = additionalData
        };

        var value = GenerateETagValue(dataToHash);
        return new ETag(value, entityType, entityId, timestamp);
    }

    /// <summary>
    /// Creates a new ETag from version number
    /// </summary>
    public static ETag FromVersion(string entityType, string entityId, long version)
    {
        var value = $"{entityType}-{entityId}-v{version}";
        var hash = ComputeHash(value);
        return new ETag(hash, entityType, entityId);
    }

    /// <summary>
    /// Creates a new ETag from a hash value
    /// </summary>
    public static ETag FromHash(string entityType, string entityId, string hash)
    {
        return new ETag(hash, entityType, entityId);
    }

    /// <summary>
    /// Creates a weak ETag (W/ prefix)
    /// </summary>
    public static ETag CreateWeak(string entityType, string entityId, object data)
    {
        var value = "W/" + GenerateETagValue(data);
        return new ETag(value, entityType, entityId);
    }

    /// <summary>
    /// Generates a new random ETag (used when entity type/id is not available)
    /// </summary>
    public static ETag Generate()
    {
        var value = Guid.NewGuid().ToString("N");
        return new ETag(value, "Unknown", "Unknown");
    }

    /// <summary>
    /// Checks if this is a weak ETag
    /// </summary>
    public bool IsWeak => Value.StartsWith("W/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the ETag value without weak prefix
    /// </summary>
    public string GetStrongValue()
    {
        return IsWeak ? Value.Substring(2) : Value;
    }

    /// <summary>
    /// Checks if this ETag matches another, considering weak ETags
    /// </summary>
    public bool Matches(ETag other)
    {
        if (other == null) return false;
        
        // For weak ETags, compare the strong parts
        if (IsWeak || other.IsWeak)
        {
            return GetStrongValue().Equals(other.GetStrongValue(), StringComparison.OrdinalIgnoreCase);
        }

        // For strong ETags, exact match
        return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this ETag matches a string value
    /// </summary>
    public bool Matches(string? etagValue)
    {
        if (string.IsNullOrEmpty(etagValue)) return false;
        
        var otherETag = Parse(etagValue, EntityType, EntityId);
        return Matches(otherETag);
    }

    /// <summary>
    /// Parses an ETag string into an ETag object
    /// </summary>
    public static ETag Parse(string etagValue, string entityType, string entityId)
    {
        if (string.IsNullOrWhiteSpace(etagValue))
        {
            throw new ArgumentException("ETag value cannot be null or whitespace.", nameof(etagValue));
        }

        // Remove quotes if present
        var cleanValue = etagValue.Trim('"');
        
        return new ETag(cleanValue, entityType, entityId);
    }

    /// <summary>
    /// Tries to parse an ETag string
    /// </summary>
    public static bool TryParse(string? etagValue, string entityType, string entityId, out ETag? etag)
    {
        etag = null;
        
        if (string.IsNullOrWhiteSpace(etagValue))
        {
            return false;
        }

        try
        {
            etag = Parse(etagValue, entityType, entityId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a new ETag for updated entity
    /// </summary>
    public ETag NextVersion()
    {
        var newTimestamp = DateTime.UtcNow;
        return FromTimestamp(EntityType, EntityId, newTimestamp);
    }

    /// <summary>
    /// Converts to HTTP ETag header format
    /// </summary>
    public string ToHttpHeaderValue()
    {
        return $"\"{Value}\"";
    }

    /// <summary>
    /// Converts to conditional request format
    /// </summary>
    public string ToIfMatchHeaderValue()
    {
        return ToHttpHeaderValue();
    }

    /// <summary>
    /// Converts to conditional request format for If-None-Match
    /// </summary>
    public string ToIfNoneMatchHeaderValue()
    {
        return ToHttpHeaderValue();
    }

    /// <summary>
    /// Checks if ETag is expired based on age
    /// </summary>
    public bool IsExpired(TimeSpan maxAge)
    {
        return DateTime.UtcNow - GeneratedAt > maxAge;
    }

    private static string GenerateETagValue<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return ComputeHash(json);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16); // Shorter hash for readability
    }

    public bool Equals(ETag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value && EntityType == other.EntityType && EntityId == other.EntityId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ETag);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, EntityType, EntityId);
    }

    public override string ToString() => ToHttpHeaderValue();

    public static implicit operator string(ETag etag) => etag.Value;
    
    public static bool operator ==(ETag? left, ETag? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Matches(right);
    }

    public static bool operator !=(ETag? left, ETag? right) => !(left == right);
}

/// <summary>
/// Exception thrown when an ETag mismatch occurs
/// </summary>
public class ETagMismatchException : Exception
{
    public string ExpectedETag { get; }
    public string ActualETag { get; }
    public string EntityType { get; }
    public string EntityId { get; }

    public ETagMismatchException(string expectedETag, string actualETag, string entityType, string entityId)
        : base($"ETag mismatch for {entityType} '{entityId}'. Expected: {expectedETag}, Actual: {actualETag}")
    {
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Exception thrown when an ETag is required but not provided
/// </summary>
public class ETagRequiredException : Exception
{
    public string EntityType { get; }
    public string EntityId { get; }

    public ETagRequiredException(string entityType, string entityId)
        : base($"ETag is required for {entityType} '{entityId}' but was not provided")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
