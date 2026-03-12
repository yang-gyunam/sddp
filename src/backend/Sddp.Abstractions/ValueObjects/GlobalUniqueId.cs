namespace Sddp.Abstractions.ValueObjects;

/// <summary>
/// UUIDv7-based globally unique identifier.
/// Time-sortable and optimized for database indexing.
/// </summary>
public readonly struct GlobalUniqueId : IEquatable<GlobalUniqueId>, IComparable<GlobalUniqueId>
{
    private readonly Guid _value;

    private GlobalUniqueId(Guid value)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a new GlobalUniqueId (UUIDv7).
    /// </summary>
    public static GlobalUniqueId NewId()
    {
        return new GlobalUniqueId(Guid.CreateVersion7());
    }

    /// <summary>
    /// Empty ID.
    /// </summary>
    public static GlobalUniqueId Empty => new(Guid.Empty);

    /// <summary>
    /// Parses a GlobalUniqueId from a string.
    /// </summary>
    public static GlobalUniqueId Parse(string value)
    {
        return new GlobalUniqueId(Guid.Parse(value));
    }

    /// <summary>
    /// Attempts to parse a GlobalUniqueId from a string.
    /// </summary>
    public static bool TryParse(string? value, out GlobalUniqueId result)
    {
        if (Guid.TryParse(value, out var guid))
        {
            result = new GlobalUniqueId(guid);
            return true;
        }

        result = Empty;
        return false;
    }

    /// <summary>
    /// Creates a GlobalUniqueId from a Guid.
    /// </summary>
    public static GlobalUniqueId FromGuid(Guid guid)
    {
        return new GlobalUniqueId(guid);
    }

    /// <summary>
    /// Converts to Guid.
    /// </summary>
    public Guid ToGuid() => _value;

    /// <summary>
    /// Indicates whether the ID is empty.
    /// </summary>
    public bool IsEmpty => _value == Guid.Empty;

    // Implicit conversions
    public static implicit operator Guid(GlobalUniqueId id) => id._value;
    public static implicit operator GlobalUniqueId(Guid guid) => new(guid);
    public static implicit operator string(GlobalUniqueId id) => id.ToString();

    // Comparison operators
    public static bool operator ==(GlobalUniqueId left, GlobalUniqueId right) => left.Equals(right);
    public static bool operator !=(GlobalUniqueId left, GlobalUniqueId right) => !left.Equals(right);

    public bool Equals(GlobalUniqueId other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is GlobalUniqueId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString();

    public int CompareTo(GlobalUniqueId other) => _value.CompareTo(other._value);
}
