using System.Globalization;

namespace Event.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency
/// </summary>
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty", nameof(currency));
        
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);
    
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        
        return new Money(left.Amount + right.Amount, left.Currency);
    }
    
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        
        return new Money(left.Amount - right.Amount, left.Currency);
    }
    
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
    
    public static Money operator *(decimal multiplier, Money money)
    {
        return money * multiplier;
    }
    
    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");
        
        return left.Amount > right.Amount;
    }
    
    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");
        
        return left.Amount < right.Amount;
    }
    
    public static bool operator >=(Money left, Money right)
    {
        return left > right || left == right;
    }
    
    public static bool operator <=(Money left, Money right)
    {
        return left < right || left == right;
    }

    public override string ToString()
    {
        return $"{Amount:C} {Currency}";
    }
    
    public string ToString(string format)
    {
        return Amount.ToString(format, CultureInfo.InvariantCulture) + " " + Currency;
    }
}

/// <summary>
/// Represents a time zone identifier
/// </summary>
public record TimeZoneId
{
    public string Value { get; }

    // For EF Core
    private TimeZoneId()
    {
        Value = string.Empty;
    }

    public TimeZoneId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TimeZone cannot be null or empty", nameof(value));

        // Validate that it's a valid timezone
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid timezone: {value}", nameof(value));
        }

        Value = value;
    }

    public TimeZoneInfo GetTimeZoneInfo() => TimeZoneInfo.FindSystemTimeZoneById(Value);

    public static TimeZoneId FromString(string value) => new(value);

    public static implicit operator string(TimeZoneId timeZoneId) => timeZoneId.Value;
    public static implicit operator TimeZoneId(string value) => new(value);
}

/// <summary>
/// Represents a slug for SEO-friendly URLs
/// </summary>
public record Slug
{
    public string Value { get; }

    // For EF Core
    private Slug()
    {
        Value = string.Empty;
    }

    public Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be null or empty", nameof(value));

        // Validate slug format (lowercase, alphanumeric, hyphens only)
        if (!IsValidSlug(value))
            throw new ArgumentException("Slug must contain only lowercase letters, numbers, and hyphens", nameof(value));

        Value = value;
    }

    private static bool IsValidSlug(string slug)
    {
        return slug.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-') &&
               !slug.StartsWith('-') &&
               !slug.EndsWith('-') &&
               !slug.Contains("--");
    }

    public static Slug FromString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        var slug = input
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove invalid characters
        slug = new string(slug.Where(c => char.IsLower(c) || char.IsDigit(c) || c == '-').ToArray());

        // Remove consecutive hyphens
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return new Slug(slug);
    }

    public static implicit operator string(Slug slug) => slug.Value;
}

/// <summary>
/// Represents geographical coordinates
/// </summary>
public record GeoCoordinates
{
    public double Latitude { get; }
    public double Longitude { get; }

    // For EF Core
    private GeoCoordinates() { }

    public GeoCoordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString()
    {
        return $"{Latitude:F6}, {Longitude:F6}";
    }
}
