using System.Text.Json.Serialization;

namespace Sddp.Abstractions.Models.Diagrams;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramRevision
{
    public required int SchemaVersion { get; init; }
    public required string DiagramId { get; init; }
    public required int Revision { get; init; }
    public required DiagramSource Source { get; init; }

    public required Dictionary<string, string> Presentation
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }

    public required Dictionary<string, DiagramNode> Nodes
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }

    public required Dictionary<string, DiagramEdge> Edges
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramSource
{
    public required string Format { get; init; }
    public required string Provider { get; init; }
    public required string ProviderVersion { get; init; }
    public required string Text { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramNode
{
    public required string Label { get; init; }

    public required Dictionary<string, string> Presentation
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramEdge
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Label { get; init; }

    public required Dictionary<string, string> Presentation
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

internal static class DiagramKeys
{
    // An adapter can supply a dictionary with its own comparer. Bind its actual
    // entries to the model's identity rules before validation or diff lookups.
    public static Dictionary<string, T> Ordinal<T>(Dictionary<string, T> entries)
    {
        // Defer null validation to DiagramValidation, also used after JSON binding.
        if (entries is null)
            return null!;
        if (entries.Comparer == StringComparer.Ordinal || entries.Comparer == EqualityComparer<string>.Default)
            return entries;

        var result = new Dictionary<string, T>(entries.Count, StringComparer.Ordinal);
        foreach (var (key, value) in entries)
        {
            if (!result.TryAdd(key, value))
                throw new InvalidDataException($"Duplicate diagram key '{key}'.");
        }
        return result;
    }
}
