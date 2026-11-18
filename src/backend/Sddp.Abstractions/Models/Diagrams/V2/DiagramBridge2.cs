namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>Pure schema1 normalization; never rewrites a stored revision or guesses its kind.</summary>
public static class DiagramBridge2
{
    public static DiagramPresentation2 Presentation(Dictionary<string, string> properties) => new()
    {
        Properties = new(properties, StringComparer.Ordinal), Order = []
    };

    public static DiagramRevision2 Promote(DiagramRevision model)
    {
        DiagramValidation.Validate(model);
        return new DiagramRevision2
        {
            SchemaVersion = 2, DiagramId = model.DiagramId, Revision = model.Revision, DiagramType = "graph",
            Source = model.Source with { }, SourceState = "unknown", SourceMap = null,
            Presentation = Presentation(model.Presentation),
            Content = new GraphContent2
            {
                Nodes = model.Nodes.ToDictionary(p => p.Key, p => new GraphNode2
                    { Label = p.Value.Label, Presentation = Presentation(p.Value.Presentation) }, StringComparer.Ordinal),
                Edges = model.Edges.ToDictionary(p => p.Key, p => new GraphEdge2
                    { From = p.Value.From, To = p.Value.To, Label = p.Value.Label, Presentation = Presentation(p.Value.Presentation) }, StringComparer.Ordinal)
            }
        };
    }

    /// <summary>Only the compatibility graph can be read by the existing projection access checker.</summary>
    public static DiagramRevision ProjectionView(DiagramRevision2 model)
    {
        if (model.Content is not GraphContent2 graph) throw new InvalidDataException("Projection requires graph content.");
        return new DiagramRevision
        {
            SchemaVersion = 1, DiagramId = model.DiagramId, Revision = model.Revision, Source = model.Source,
            Presentation = new(model.Presentation.Properties, StringComparer.Ordinal),
            Nodes = graph.Nodes.ToDictionary(p => p.Key, p => new DiagramNode
                { Label = p.Value.Label, Presentation = new(p.Value.Presentation.Properties, StringComparer.Ordinal) }, StringComparer.Ordinal),
            Edges = graph.Edges.ToDictionary(p => p.Key, p => new DiagramEdge
                { From = p.Value.From, To = p.Value.To, Label = p.Value.Label, Presentation = new(p.Value.Presentation.Properties, StringComparer.Ordinal) }, StringComparer.Ordinal)
        };
    }
}
