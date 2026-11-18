using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>Dispatches typed content using SDDP diagramType without a provider discriminator.</summary>
public sealed class DiagramRevision2Converter : JsonConverter<DiagramRevision2>
{
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        AllowOutOfOrderMetadataProperties = true
    };

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Wire
    {
        public required int SchemaVersion { get; init; }
        public required string DiagramId { get; init; }
        public required int Revision { get; init; }
        public required string DiagramType { get; init; }
        public required DiagramSource Source { get; init; }
        public required string SourceState { get; init; }
        public required DiagramSourceMap2? SourceMap { get; init; }
        public required DiagramPresentation2 Presentation { get; init; }
        public required JsonElement Content { get; init; }
    }

    public override DiagramRevision2 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        RejectDuplicateKeys(document.RootElement);
        var wire = document.RootElement.Deserialize<Wire>(WireJson) ?? throw new JsonException("Expected schema2 model.");
        if (wire.SchemaVersion != 2) throw new JsonException("UNSUPPORTED_SCHEMA: expected schemaVersion 2.");
        var contentType = wire.DiagramType switch
        {
            "graph" => typeof(GraphContent2), "flowchart" => typeof(FlowchartContent2),
            "sequence" => typeof(SequenceContent2), "class" => typeof(ClassContent2),
            "state" => typeof(StateContent2), "er" => typeof(ErContent2), "c4" => typeof(C4Content2),
            _ => throw new JsonException("UNSUPPORTED_SCHEMA: unknown diagramType.")
        };
        var content = (DiagramContent2?)wire.Content.Deserialize(contentType, WireJson)
            ?? throw new JsonException("Expected typed content.");
        content = CanonicalContent(content);
        return new DiagramRevision2
        {
            SchemaVersion = wire.SchemaVersion, DiagramId = wire.DiagramId, Revision = wire.Revision,
            DiagramType = wire.DiagramType, Source = wire.Source, SourceState = wire.SourceState,
            SourceMap = wire.SourceMap, Presentation = wire.Presentation, Content = content
        };
    }

    public override void Write(Utf8JsonWriter writer, DiagramRevision2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", value.SchemaVersion);
        writer.WriteString("diagramId", value.DiagramId);
        writer.WriteNumber("revision", value.Revision);
        writer.WriteString("diagramType", value.DiagramType);
        writer.WritePropertyName("source"); JsonSerializer.Serialize(writer, value.Source, WireJson);
        writer.WriteString("sourceState", value.SourceState);
        writer.WritePropertyName("sourceMap"); JsonSerializer.Serialize(writer, value.SourceMap, WireJson);
        writer.WritePropertyName("presentation"); JsonSerializer.Serialize(writer, value.Presentation, WireJson);
        writer.WritePropertyName("content");
        var content = CanonicalContent(value.Content);
        JsonSerializer.Serialize(writer, content, content.GetType(), WireJson);
        writer.WriteEndObject();
    }

    private static DiagramContent2 CanonicalContent(DiagramContent2 content) => content is ErContent2 er && er.Attributes is not null && er.Attributes.Values.All(a => a is not null && a.Keys is not null) ? er with
    {
        Attributes = er.Attributes.ToDictionary(p => p.Key, p => p.Value with
        { Keys = p.Value.Keys.OrderBy(k => k switch { "primary" => 0, "foreign" => 1, _ => 2 }).ToList() }, StringComparer.Ordinal)
    } : content;

    internal static void RejectDuplicateKeys(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in value.EnumerateObject())
            {
                if (!keys.Add(field.Name)) throw new JsonException($"Duplicate diagram key '{field.Name}'.");
                RejectDuplicateKeys(field.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectDuplicateKeys(item);
    }
}
