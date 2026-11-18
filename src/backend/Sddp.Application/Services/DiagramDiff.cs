using Sddp.Abstractions.Models.Diagrams;

namespace Sddp.Application.Services;

public static class DiagramDiff
{
    public static IReadOnlyList<DiagramChange> Compare(DiagramRevision left, DiagramRevision right)
    {
        DiagramValidation.Validate(left);
        DiagramValidation.Validate(right);
        if (left.DiagramId != right.DiagramId)
            throw new InvalidDataException("Revisions belong to different diagrams.");

        var changes = new List<DiagramChange>();
        ComparePresentation("diagram", left.DiagramId, left.Presentation, right.Presentation, changes);
        CompareEntries("node", left.Nodes, right.Nodes, changes,
            node => node with { Presentation = new(node.Presentation) }, (id, before, after) =>
        {
            CompareValue("node", id, "label", "semantic", before.Label, after.Label, changes);
            ComparePresentation("node", id, before.Presentation, after.Presentation, changes);
        });
        CompareEntries("edge", left.Edges, right.Edges, changes,
            edge => edge with { Presentation = new(edge.Presentation) }, (id, before, after) =>
        {
            CompareValue("edge", id, "from", "semantic", before.From, after.From, changes);
            CompareValue("edge", id, "to", "semantic", before.To, after.To, changes);
            CompareValue("edge", id, "label", "semantic", before.Label, after.Label, changes);
            ComparePresentation("edge", id, before.Presentation, after.Presentation, changes);
        });
        return changes;
    }

    private static void CompareEntries<T>(string target, Dictionary<string, T> left, Dictionary<string, T> right,
        List<DiagramChange> changes, Func<T, T> snapshot, Action<string, T, T> compare) where T : class
    {
        foreach (var id in Keys(left, right))
        {
            if (!left.TryGetValue(id, out var before))
                changes.Add(new DiagramChange(target, id, null, "added", "semantic", null, snapshot(right[id])));
            else if (!right.TryGetValue(id, out var after))
                changes.Add(new DiagramChange(target, id, null, "removed", "semantic", snapshot(before), null));
            else
                compare(id, before, after);
        }
    }

    private static void ComparePresentation(string target, string id,
        Dictionary<string, string> left, Dictionary<string, string> right, List<DiagramChange> changes)
    {
        foreach (var field in Keys(left, right))
            CompareValue(target, id, field, "presentation", left.GetValueOrDefault(field),
                right.GetValueOrDefault(field), changes);
    }

    private static IEnumerable<string> Keys<T>(Dictionary<string, T> left, Dictionary<string, T> right) =>
        left.Keys.Union(right.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal);

    private static void CompareValue(string target, string id, string field, string category,
        string? before, string? after, List<DiagramChange> changes)
    {
        if (before == after)
            return;
        var kind = before is null ? "added" : after is null ? "removed" : "modified";
        changes.Add(new DiagramChange(target, id, field, kind, category, before, after));
    }
}
