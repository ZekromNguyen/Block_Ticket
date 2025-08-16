using System.Text.RegularExpressions;

namespace Event.Domain.ValueObjects;

/// <summary>
/// Represents an idempotency key value object
/// </summary>
public record IdempotencyKey
{
    private static readonly Regex ValidKeyPattern = new(@"^[a-zA-Z0-9\-_]{1,255}$", RegexOptions.Compiled);

    public string Value { get; }

    // For EF Core
    private IdempotencyKey()
    {
        Value = string.Empty;
    }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(value));

        if (value.Length > 255)
            throw new ArgumentException("Idempotency key cannot exceed 255 characters", nameof(value));

        if (!ValidKeyPattern.IsMatch(value))
            throw new ArgumentException("Idempotency key must contain only alphanumeric characters, hyphens, and underscores", nameof(value));

        Value = value;
    }

    public static IdempotencyKey Generate()
    {
        return new IdempotencyKey($"idem_{Guid.NewGuid():N}");
    }

    public static implicit operator string(IdempotencyKey key) => key.Value;
    public static implicit operator IdempotencyKey(string value) => new(value);

    public override string ToString() => Value;
}
