using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;

namespace Event.Domain.Models;

/// <summary>
/// Cursor-based pagination result
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public record CursorPagedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// Cursor pointing to the position after the last item in this page
    /// Use this for the 'after' parameter in the next request
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// Cursor pointing to the position before the first item in this page
    /// Use this for the 'before' parameter in the previous request
    /// </summary>
    public string? PreviousCursor { get; init; }

    /// <summary>
    /// Indicates if there are more items after this page
    /// </summary>
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Indicates if there are more items before this page
    /// </summary>
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Number of items requested
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Actual number of items returned in this page
    /// </summary>
    public int Count => Items.Count();

    /// <summary>
    /// Optional total count (may be expensive to compute for large datasets)
    /// Only included when explicitly requested
    /// </summary>
    public int? TotalCount { get; init; }
}

/// <summary>
/// Cursor pagination parameters
/// </summary>
public record CursorPaginationParams
{
    /// <summary>
    /// Maximum number of items to return (1-100)
    /// </summary>
    [Range(1, 100)]
    public int First { get; init; } = 20;

    /// <summary>
    /// Cursor to start fetching from (exclusive)
    /// Returns items after this cursor
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// Maximum number of items to return going backwards (1-100)
    /// </summary>
    [Range(1, 100)]
    public int Last { get; init; } = 20;

    /// <summary>
    /// Cursor to start fetching from (exclusive) going backwards
    /// Returns items before this cursor
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Whether to include total count in response (expensive for large datasets)
    /// </summary>
    public bool IncludeTotalCount { get; init; } = false;

    /// <summary>
    /// Direction of pagination
    /// </summary>
    public PaginationDirection Direction => Before != null ? PaginationDirection.Backward : PaginationDirection.Forward;

    /// <summary>
    /// Effective page size based on direction
    /// </summary>
    public int EffectivePageSize => Direction == PaginationDirection.Backward ? Last : First;

    /// <summary>
    /// Effective cursor based on direction
    /// </summary>
    public string? EffectiveCursor => Direction == PaginationDirection.Backward ? Before : After;

    /// <summary>
    /// Validate pagination parameters
    /// </summary>
    public void Validate()
    {
        if (After != null && Before != null)
        {
            throw new ArgumentException("Cannot specify both 'after' and 'before' parameters");
        }

        if (First < 1 || First > 100)
        {
            throw new ArgumentException("'first' parameter must be between 1 and 100");
        }

        if (Last < 1 || Last > 100)
        {
            throw new ArgumentException("'last' parameter must be between 1 and 100");
        }
    }
}

/// <summary>
/// Pagination direction
/// </summary>
public enum PaginationDirection
{
    Forward,
    Backward
}

/// <summary>
/// Sort direction for cursor pagination
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Cursor implementation for pagination
/// </summary>
public class Cursor
{
    /// <summary>
    /// Primary sort value (typically ID or timestamp)
    /// </summary>
    public object PrimaryValue { get; }

    /// <summary>
    /// Secondary sort value (for tie-breaking)
    /// </summary>
    public object? SecondaryValue { get; }

    /// <summary>
    /// Type of the primary value
    /// </summary>
    public Type PrimaryType { get; }

    /// <summary>
    /// Type of the secondary value
    /// </summary>
    public Type? SecondaryType { get; }

    public Cursor(object primaryValue, object? secondaryValue = null)
    {
        PrimaryValue = primaryValue ?? throw new ArgumentNullException(nameof(primaryValue));
        SecondaryValue = secondaryValue;
        PrimaryType = primaryValue.GetType();
        SecondaryType = secondaryValue?.GetType();
    }

    /// <summary>
    /// Encode cursor to base64 string
    /// </summary>
    public string Encode()
    {
        var cursorData = new
        {
            Primary = PrimaryValue,
            PrimaryType = PrimaryType.FullName,
            Secondary = SecondaryValue,
            SecondaryType = SecondaryType?.FullName
        };

        var json = JsonSerializer.Serialize(cursorData);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decode cursor from base64 string
    /// </summary>
    public static Cursor Decode(string encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            throw new ArgumentException("Cursor cannot be null or empty", nameof(encodedCursor));
        }

        try
        {
            var bytes = Convert.FromBase64String(encodedCursor);
            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var primaryType = Type.GetType(data.GetProperty("PrimaryType").GetString()!) 
                ?? throw new InvalidOperationException("Invalid primary type in cursor");

            var primaryValue = ConvertJsonElement(data.GetProperty("Primary"), primaryType);

            object? secondaryValue = null;
            if (data.TryGetProperty("Secondary", out var secondaryElement) && secondaryElement.ValueKind != JsonValueKind.Null)
            {
                if (data.TryGetProperty("SecondaryType", out var secondaryTypeElement) && 
                    secondaryTypeElement.ValueKind != JsonValueKind.Null)
                {
                    var secondaryType = Type.GetType(secondaryTypeElement.GetString()!);
                    if (secondaryType != null)
                    {
                        secondaryValue = ConvertJsonElement(secondaryElement, secondaryType);
                    }
                }
            }

            return new Cursor(primaryValue, secondaryValue);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid cursor format: {ex.Message}", nameof(encodedCursor), ex);
        }
    }

    /// <summary>
    /// Try to decode cursor, returns null if invalid
    /// </summary>
    public static Cursor? TryDecode(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        try
        {
            return Decode(encodedCursor);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Create cursor from entity with primary key and timestamp
    /// </summary>
    public static Cursor FromEntity<TKey>(TKey id, DateTime timestamp) where TKey : struct
    {
        return new Cursor(timestamp, id);
    }

    /// <summary>
    /// Create cursor from entity with composite key
    /// </summary>
    public static Cursor FromComposite(object primary, object secondary)
    {
        return new Cursor(primary, secondary);
    }

    private static object ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (targetType == typeof(Guid))
        {
            return Guid.Parse(element.GetString()!);
        }
        if (targetType == typeof(DateTime))
        {
            return element.GetDateTime();
        }
        if (targetType == typeof(DateTimeOffset))
        {
            return element.GetDateTimeOffset();
        }
        if (targetType == typeof(int))
        {
            return element.GetInt32();
        }
        if (targetType == typeof(long))
        {
            return element.GetInt64();
        }
        if (targetType == typeof(string))
        {
            return element.GetString()!;
        }
        if (targetType == typeof(decimal))
        {
            return element.GetDecimal();
        }
        if (targetType == typeof(double))
        {
            return element.GetDouble();
        }

        // Fallback to JSON deserialization
        return JsonSerializer.Deserialize(element.GetRawText(), targetType)!;
    }

    public override string ToString() => Encode();

    public override bool Equals(object? obj)
    {
        if (obj is not Cursor other) return false;
        return PrimaryValue.Equals(other.PrimaryValue) && 
               Equals(SecondaryValue, other.SecondaryValue);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimaryValue, SecondaryValue);
    }
}
