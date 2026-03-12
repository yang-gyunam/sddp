namespace Sddp.Abstractions.ValueObjects;

/// <summary>
/// UTC-based timestamp value object
/// Supports ISO 8601 serialization
/// </summary>
public readonly struct Timestamp : IEquatable<Timestamp>, IComparable<Timestamp>
{
    private readonly DateTimeOffset _value;

    private Timestamp(DateTimeOffset value)
    {
        _value = value.ToUniversalTime();
    }

    /// <summary>
    /// Creates a Timestamp from the current UTC time
    /// </summary>
    public static Timestamp Now => new(DateTimeOffset.UtcNow);

    /// <summary>
    /// Empty Timestamp
    /// </summary>
    public static Timestamp Empty => new(DateTimeOffset.MinValue);

    /// <summary>
    /// Creates a Timestamp from DateTimeOffset
    /// </summary>
    public static Timestamp FromDateTimeOffset(DateTimeOffset value) => new(value);

    /// <summary>
    /// Creates a Timestamp from DateTime (converted to UTC)
    /// </summary>
    public static Timestamp FromDateTime(DateTime value)
    {
        var dto = value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : new DateTimeOffset(value);
        return new Timestamp(dto);
    }

    /// <summary>
    /// Parses from an ISO 8601 string
    /// </summary>
    public static Timestamp Parse(string value)
    {
        return new Timestamp(DateTimeOffset.Parse(value));
    }

    /// <summary>
    /// Tries to parse from an ISO 8601 string
    /// </summary>
    public static bool TryParse(string? value, out Timestamp result)
    {
        if (DateTimeOffset.TryParse(value, out var dto))
        {
            result = new Timestamp(dto);
            return true;
        }

        result = Empty;
        return false;
    }

    /// <summary>
    /// Converts to DateTimeOffset
    /// </summary>
    public DateTimeOffset ToDateTimeOffset() => _value;

    /// <summary>
    /// Converts to UTC DateTime
    /// </summary>
    public DateTime ToUtcDateTime() => _value.UtcDateTime;

    /// <summary>
    /// Converts to the specified time zone
    /// </summary>
    public DateTimeOffset ToTimeZone(TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTime(_value, timeZone);
    }

    /// <summary>
    /// Converts to an ISO 8601 string
    /// </summary>
    public string ToIso8601() => _value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

    /// <summary>
    /// Returns whether the timestamp is empty
    /// </summary>
    public bool IsEmpty => _value == DateTimeOffset.MinValue;

    // Implicit conversions
    public static implicit operator DateTimeOffset(Timestamp timestamp) => timestamp._value;
    public static implicit operator Timestamp(DateTimeOffset dto) => new(dto);

    // Comparison operators
    public static bool operator ==(Timestamp left, Timestamp right) => left.Equals(right);
    public static bool operator !=(Timestamp left, Timestamp right) => !left.Equals(right);
    public static bool operator <(Timestamp left, Timestamp right) => left._value < right._value;
    public static bool operator >(Timestamp left, Timestamp right) => left._value > right._value;
    public static bool operator <=(Timestamp left, Timestamp right) => left._value <= right._value;
    public static bool operator >=(Timestamp left, Timestamp right) => left._value >= right._value;

    public bool Equals(Timestamp other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is Timestamp other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => ToIso8601();

    public int CompareTo(Timestamp other) => _value.CompareTo(other._value);
}
