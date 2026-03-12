namespace Sddp.Abstractions.ValueObjects;

/// <summary>
/// Spec owner list value object.
/// Stored internally as a comma-separated string and exposed through semantic helper methods.
/// </summary>
public readonly record struct SpecOwners
{
    private readonly string _csv;

    private SpecOwners(string csv)
    {
        _csv = csv;
    }

    public static SpecOwners Empty => new(string.Empty);

    public bool IsEmpty => string.IsNullOrWhiteSpace(_csv);

    public static SpecOwners FromCsv(string? csv)
    {
        var entries = NormalizeEntries(csv);
        return entries.Count == 0
            ? Empty
            : new SpecOwners(string.Join(',', entries));
    }

    public static SpecOwners FromEntries(IEnumerable<string>? entries)
    {
        if (entries is null)
        {
            return Empty;
        }

        var normalizedEntries = NormalizeEntries(entries);
        return normalizedEntries.Count == 0
            ? Empty
            : new SpecOwners(string.Join(',', normalizedEntries));
    }

    public string ToCsv()
    {
        return _csv ?? string.Empty;
    }

    public string? ToNullableCsv()
    {
        return IsEmpty ? null : ToCsv();
    }

    public IReadOnlyList<string> GetEntries()
    {
        return IsEmpty
            ? []
            : ToCsv().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public IReadOnlyList<GlobalUniqueId> GetOwnerIds()
    {
        if (IsEmpty)
        {
            return [];
        }

        var ids = new List<GlobalUniqueId>();
        foreach (var entry in GetEntries())
        {
            if (GlobalUniqueId.TryParse(entry, out var ownerId))
            {
                ids.Add(ownerId);
            }
        }

        return ids;
    }

    public GlobalUniqueId? GetPrimaryOwnerId()
    {
        foreach (var ownerId in GetOwnerIds())
        {
            return ownerId;
        }

        return null;
    }

    public bool Contains(GlobalUniqueId ownerId)
    {
        if (ownerId.IsEmpty || IsEmpty)
        {
            return false;
        }

        var ownerIdText = ownerId.ToString();
        return GetEntries().Any(entry => string.Equals(entry, ownerIdText, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
    {
        return ToCsv();
    }

    private static List<string> NormalizeEntries(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return NormalizeEntries(csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static List<string> NormalizeEntries(IEnumerable<string> entries)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var value = entry.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }
}
