namespace Sddp.Abstractions.ValueObjects;

/// <summary>
/// Semantic Versioning (Major.Minor.Patch)
/// </summary>
public readonly struct SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public SemanticVersion(int major, int minor, int patch)
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Initial version (1.0.0).
    /// </summary>
    public static SemanticVersion Initial => new(1, 0, 0);

    /// <summary>
    /// Empty version (0.0.0).
    /// </summary>
    public static SemanticVersion Empty => new(0, 0, 0);

    /// <summary>
    /// Increments the major version (breaking change).
    /// </summary>
    public SemanticVersion IncrementMajor() => new(Major + 1, 0, 0);

    /// <summary>
    /// Increments the minor version (feature addition).
    /// </summary>
    public SemanticVersion IncrementMinor() => new(Major, Minor + 1, 0);

    /// <summary>
    /// Increments the patch version (bug fix).
    /// </summary>
    public SemanticVersion IncrementPatch() => new(Major, Minor, Patch + 1);

    /// <summary>
    /// Parses a version string such as "1.2.3".
    /// </summary>
    public static SemanticVersion Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Version string cannot be empty", nameof(value));

        var parts = value.Split('.');
        if (parts.Length != 3)
            throw new FormatException($"Invalid version format: {value}. Expected Major.Minor.Patch");

        return new SemanticVersion(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2])
        );
    }

    /// <summary>
    /// Attempts to parse a version string.
    /// </summary>
    public static bool TryParse(string? value, out SemanticVersion result)
    {
        result = Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) ||
            !int.TryParse(parts[2], out var patch))
            return false;

        if (major < 0 || minor < 0 || patch < 0)
            return false;

        result = new SemanticVersion(major, minor, patch);
        return true;
    }

    // Comparison operators
    public static bool operator ==(SemanticVersion left, SemanticVersion right) => left.Equals(right);
    public static bool operator !=(SemanticVersion left, SemanticVersion right) => !left.Equals(right);
    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    public bool Equals(SemanticVersion other) =>
        Major == other.Major && Minor == other.Minor && Patch == other.Patch;

    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

    public int CompareTo(SemanticVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        return Patch.CompareTo(other.Patch);
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
