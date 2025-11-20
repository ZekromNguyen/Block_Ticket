namespace Event.Domain.ValueObjects;

/// <summary>
/// Represents a physical address
/// </summary>
public record Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public GeoCoordinates? Coordinates { get; }

    // For EF Core
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    public Address(
        string street,
        string city,
        string state,
        string postalCode,
        string country,
        GeoCoordinates? coordinates = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be null or empty", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be null or empty", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be null or empty", nameof(state));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be null or empty", nameof(postalCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be null or empty", nameof(country));

        Street = street.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
        Coordinates = coordinates;
    }

    public string GetFullAddress()
    {
        return $"{Street}, {City}, {State} {PostalCode}, {Country}";
    }

    public override string ToString()
    {
        return GetFullAddress();
    }
}

/// <summary>
/// Represents a seat position in a venue
/// </summary>
public record SeatPosition
{
    public string Section { get; }
    public string Row { get; }
    public string Number { get; }

    // For EF Core
    private SeatPosition()
    {
        Section = string.Empty;
        Row = string.Empty;
        Number = string.Empty;
    }

    public SeatPosition(string section, string row, string number)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section cannot be null or empty", nameof(section));

        if (string.IsNullOrWhiteSpace(row))
            throw new ArgumentException("Row cannot be null or empty", nameof(row));

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Number cannot be null or empty", nameof(number));

        Section = section.Trim().ToUpperInvariant();
        Row = row.Trim().ToUpperInvariant();
        Number = number.Trim().ToUpperInvariant();
    }

    public string GetDisplayName()
    {
        return $"Section {Section}, Row {Row}, Seat {Number}";
    }

    public override string ToString()
    {
        return $"{Section}-{Row}-{Number}";
    }
}

/// <summary>
/// Represents a date range with timezone awareness
/// </summary>
public record DateTimeRange
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public TimeZoneId TimeZone { get; }

    public DateTimeRange(DateTime startDate, DateTime endDate, TimeZoneId timeZone)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");

        StartDate = startDate;
        EndDate = endDate;
        TimeZone = timeZone;
    }

    // Overloaded constructor with UTC as default timezone
    public DateTimeRange(DateTime startDate, DateTime endDate)
        : this(startDate, endDate, new TimeZoneId("UTC"))
    {
    }

    public TimeSpan Duration => EndDate - StartDate;

    public bool Contains(DateTime dateTime)
    {
        return dateTime >= StartDate && dateTime <= EndDate;
    }

    public bool Overlaps(DateTimeRange other)
    {
        return StartDate < other.EndDate && EndDate > other.StartDate;
    }

    public DateTime GetStartInTimeZone()
    {
        var timeZoneInfo = TimeZone.GetTimeZoneInfo();
        return TimeZoneInfo.ConvertTimeFromUtc(StartDate, timeZoneInfo);
    }

    public DateTime GetEndInTimeZone()
    {
        var timeZoneInfo = TimeZone.GetTimeZoneInfo();
        return TimeZoneInfo.ConvertTimeFromUtc(EndDate, timeZoneInfo);
    }
}

/// <summary>
/// Represents capacity constraints
/// </summary>
public record Capacity
{
    public int Total { get; init; }
    public int Held { get; init; }
    public int Sold { get; init; }
    public int Available => Total - Held - Sold;
    public int Reserved => Held + Sold;

    public Capacity(int total, int held = 0, int sold = 0)
    {
        if (total < 0) throw new ArgumentException("Total capacity cannot be negative.");
        if (held < 0) throw new ArgumentException("Held capacity cannot be negative.");
        if (sold < 0) throw new ArgumentException("Sold capacity cannot be negative.");
        if (held + sold > total) throw new ArgumentException("Held and sold capacity cannot exceed total capacity.");

        Total = total;
        Held = held;
        Sold = sold;
    }

    public bool CanReserve(int quantity) => quantity > 0 && quantity <= Available;

    public Capacity Reserve(int quantity)
    {
        if (!CanReserve(quantity)) throw new InvalidOperationException("Not enough capacity available");
        return this with { Held = Held + quantity };
    }

    public Capacity Release(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        if (quantity > Held) throw new InvalidOperationException("Cannot release more capacity than is held.");
        return this with { Held = Held - quantity };
    }

    public Capacity Confirm(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        if (quantity > Held) throw new InvalidOperationException("Cannot confirm more capacity than is held.");
        return this with { Held = Held - quantity, Sold = Sold + quantity };
    }

    public decimal UtilizationPercentage => Total == 0 ? 0 : (decimal)Reserved / Total * 100;
}
