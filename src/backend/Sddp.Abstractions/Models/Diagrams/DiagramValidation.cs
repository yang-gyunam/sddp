namespace Sddp.Abstractions.Models.Diagrams;

public static class DiagramValidation
{
    public static void Validate(DiagramRevision model)
    {
        if (model.SchemaVersion != 1)
            throw new InvalidDataException($"Unsupported schemaVersion {model.SchemaVersion}.");
        if (model.Revision < 1 || string.IsNullOrWhiteSpace(model.DiagramId))
            throw new InvalidDataException("Expected a diagramId and positive revision.");
        if (model.Source is not { Text: not null } source || string.IsNullOrWhiteSpace(source.Format)
            || string.IsNullOrWhiteSpace(source.Provider) || string.IsNullOrWhiteSpace(source.ProviderVersion))
            throw new InvalidDataException("Incomplete source provenance.");

        ValidatePresentation(model.Presentation);
        if (model.Nodes is null || model.Edges is null)
            throw new InvalidDataException("Expected node and edge maps.");
        foreach (var (id, node) in model.Nodes)
        {
            if (string.IsNullOrWhiteSpace(id) || node?.Label is null)
                throw new InvalidDataException($"Invalid node '{id}'.");
            ValidatePresentation(node.Presentation);
        }
        foreach (var (id, edge) in model.Edges)
        {
            if (string.IsNullOrWhiteSpace(id) || edge?.Label is null || edge.From is null || edge.To is null
                || !model.Nodes.ContainsKey(edge.From) || !model.Nodes.ContainsKey(edge.To))
                throw new InvalidDataException($"Invalid edge '{id}' or unknown endpoint.");
            ValidatePresentation(edge.Presentation);
        }
    }

    private static void ValidatePresentation(Dictionary<string, string> properties)
    {
        if (properties is null || properties.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
            throw new InvalidDataException("Expected named presentation properties with string values.");
    }
}
