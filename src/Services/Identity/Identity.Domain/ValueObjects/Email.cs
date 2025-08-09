using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        if (value.Length > 254) // RFC 5321 limit
            throw new ArgumentException("Email address is too long", nameof(value));

        Value = value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string email) => new(email);

    public override string ToString() => Value;
}
