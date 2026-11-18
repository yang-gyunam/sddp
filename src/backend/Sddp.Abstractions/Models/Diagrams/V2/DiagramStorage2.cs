using System.Text.Json;

namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>Persisted schema discriminator plus a detached normalized view.</summary>
public sealed record StoredDiagramModel2(int StoredSchemaVersion, DiagramRevision2 Model);

public static class DiagramStorage2
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static StoredDiagramModel2 Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        DiagramRevision2Converter.RejectDuplicateKeys(document.RootElement);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("schemaVersion", out var version) ||
            version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schema))
            throw new InvalidDataException("Expected an integer schemaVersion.");
        var model = schema switch
        {
            1 => DiagramBridge2.Promote(document.RootElement.Deserialize<DiagramRevision>(Json)!),
            2 => document.RootElement.Deserialize<DiagramRevision2>(Json)!,
            _ => throw new InvalidDataException("UNSUPPORTED_SCHEMA")
        };
        DiagramValidation2.Validate(model);
        return new(schema, model);
    }
    public static string Write(DiagramRevision2 model)
    {
        DiagramValidation2.Validate(model);
        return JsonSerializer.Serialize(model, Json);
    }
    /// <summary>Reuses the current origin policy; typed authored diagrams have no projected origins.</summary>
    public static DiagramRevision AccessView(DiagramRevision2 model)
    {
        DiagramValidation2.Validate(model);
        if (model.Content is GraphContent2) return DiagramBridge2.ProjectionView(model);
        if (model.Source.Format == DiagramProjectionSource.Format || model.Source.Provider == "sddp-entities")
            throw new InvalidDataException("Typed authored content cannot impersonate a projection.");
        return new DiagramRevision
        {
            SchemaVersion = 1, DiagramId = model.DiagramId, Revision = model.Revision,
            Source = model.Source, Presentation = new(), Nodes = new(), Edges = new()
        };
    }
}
