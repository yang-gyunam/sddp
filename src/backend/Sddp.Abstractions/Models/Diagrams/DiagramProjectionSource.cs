using System.Text.Json;
using System.Text.Json.Serialization;
using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.Models.Diagrams;

/// <summary>Versioned origin identities, independent of rendering and layout.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramOrigin([property: JsonRequired] EntityType Type, [property: JsonRequired] string Id)
{
    [JsonIgnore]
    public string Key => $"{(int)Type}:{Id}";
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramProjectionSource([property: JsonRequired] int Version, [property: JsonRequired] DiagramOrigin[] Origins)
{
    public const string Format = "sddp-projection";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    public static DiagramOrigin[]? ReadForKind(DiagramRevision model, string? kind)
    {
        DiagramValidation.Validate(model);
        var origins = Read(model);
        if (kind == "derived" && origins is null || kind == "authored" && origins is not null)
            throw new InvalidDataException("Derived diagrams require projection provenance; authored diagrams cannot use reserved projection sources.");
        return origins;
    }

    public static DiagramOrigin[]? Read(DiagramRevision model)
    {
        if (model.Source.Format != Format && model.Source.Provider != "sddp-entities") return null;
        if (model.Source.Format != Format || model.Source.Provider != "sddp-entities" || model.Source.ProviderVersion != "1")
            throw new InvalidDataException("Unsupported projection source.");
        DiagramProjectionSource? source;
        try { source = JsonSerializer.Deserialize<DiagramProjectionSource>(model.Source.Text, Json); }
        catch (JsonException ex) { throw new InvalidDataException("Invalid projection source.", ex); }
        if (source?.Version != 1 || source.Origins is not { Length: > 0 and <= 100 })
            throw new InvalidDataException("Expected projection source version 1 and 1 to 100 origins.");
        ValidateOrigins(source.Origins);
        if (!source.Origins.Select(o => o.Key).ToHashSet(StringComparer.Ordinal).SetEquals(model.Nodes.Keys))
            throw new InvalidDataException("Projection node keys must match its origins.");
        return source.Origins;
    }

    public static void ValidateOrigins(DiagramOrigin[] origins)
    {
        if (origins is not { Length: > 0 and <= 100 } || origins.Any(o => o is null
            || (int)o.Type is < 0 or > 6 || !Guid.TryParseExact(o.Id, "D", out var id) || id == Guid.Empty
            || o.Id != id.ToString("D")) || origins.Select(o => o.Key).Distinct(StringComparer.Ordinal).Count() != origins.Length)
            throw new InvalidDataException("Expected 1 to 100 unique, supported origin identities with canonical UUIDs; Diagram recursion is unsupported.");
    }

    public static DiagramSource Create(DiagramOrigin[] origins) => new()
    {
        Format = Format, Provider = "sddp-entities", ProviderVersion = "1",
        Text = JsonSerializer.Serialize(new DiagramProjectionSource(1, origins), Json)
    };
}
