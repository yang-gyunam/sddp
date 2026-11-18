using System.Text.Json;
using System.Text.Json.Nodes;
using Sddp.Abstractions.Models.Diagrams.V2;

namespace Sddp.Application.Services;

public static class DiagramDiff2
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly IComparer<string[]?> FieldPathComparer = Comparer<string[]?>.Create((left, right) =>
    {
        if (left is null || right is null) return left is null ? right is null ? 0 : -1 : 1;
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            var compared = StringComparer.Ordinal.Compare(left[i], right[i]);
            if (compared != 0) return compared;
        }
        return left.Length.CompareTo(right.Length);
    });
    public static DiagramChangeSet2 Compare(DiagramRevision2 left, DiagramRevision2 right)
    {
        DiagramValidation2.Validate(left); DiagramValidation2.Validate(right);
        if (left.DiagramId != right.DiagramId) throw new InvalidDataException("IDENTITY_CONFLICT: different documents.");
        if (left.DiagramType != right.DiagramType || left.Content is C4Content2 lc && right.Content is C4Content2 rc && lc.View != rc.View)
            throw new InvalidDataException("INCOMPATIBLE_DIAGRAM_TYPE");
        var l = JsonSerializer.SerializeToNode(left, Json)!.AsObject();
        var r = JsonSerializer.SerializeToNode(right, Json)!.AsObject();
        var lcontent = l["content"]!.AsObject(); var rcontent = r["content"]!.AsObject();
        var changes = new List<DiagramChange2>(); var provenance = new List<DiagramProvenanceChange2>();
        var root = new ElementRef { Collection = "diagram", Id = left.DiagramId };
        void Change(ElementRef target, string[]? field, string kind, string category, JsonNode? before, JsonNode? after) =>
            changes.Add(new(target, field, kind, category, before?.DeepClone(), after?.DeepClone()));
        void Field(ElementRef target, string[] path, JsonNode? before, JsonNode? after, string category = "semantic", bool had = true, bool has = true)
        {
            if (had != has || !JsonNode.DeepEquals(before, after)) Change(target, path, !had ? "added" : !has ? "removed" : "modified", category, before, after);
        }
        void Order(ElementRef owner, string field, JsonArray before, JsonArray after, string category)
        {
            var originalBefore = before; var originalAfter = after;
            // Empty presentation order means ordinal automatic order, not zero children.
            if (category == "presentation")
            {
                if (before.Count == 0) before = AutomaticOrder(owner, lcontent, left.DiagramType);
                if (after.Count == 0) after = AutomaticOrder(owner, rcontent, right.DiagramType);
            }
            // Membership is reported by added/removed elements or their owner change.
            var common = before.Where(a => after.Any(b => JsonNode.DeepEquals(a, b))).ToList();
            var next = after.Where(a => before.Any(b => JsonNode.DeepEquals(a, b))).ToList();
            var changed = !common.Zip(next).All(pair => JsonNode.DeepEquals(pair.First, pair.Second));

            if (changed) Change(owner, category == "presentation" ? ["presentation", "order"] : [field], "modified", category, originalBefore, originalAfter);
        }
        void Presentation(ElementRef target, JsonObject before, JsonObject after)
        {
            var bp = before["properties"]!.AsObject(); var ap = after["properties"]!.AsObject();
            foreach (var key in bp.Select(p => p.Key).Union(ap.Select(p => p.Key)).Order(StringComparer.Ordinal))
                Field(target, ["presentation", "properties", key], bp[key], ap[key], "presentation", bp.ContainsKey(key), ap.ContainsKey(key));
            Order(target, "order", before["order"]!.AsArray(), after["order"]!.AsArray(), "presentation");
        }
        foreach (var field in new[] { "source", "sourceState", "sourceMap" })
            if (!JsonNode.DeepEquals(l[field], r[field])) provenance.Add(new([field], l[field]?.DeepClone(), r[field]?.DeepClone()));
        Presentation(root, l["presentation"]!.AsObject(), r["presentation"]!.AsObject());
        if (lcontent["timeline"] is JsonArray lt && rcontent["timeline"] is JsonArray rt) Order(root, "timeline", lt, rt, "semantic");
        var lowners = Owners(lcontent, root); var rowners = Owners(rcontent, root);
        foreach (var collection in lcontent.Where(p => p.Value is JsonObject).Select(p => p.Key).Order(StringComparer.Ordinal))
        {
            var before = lcontent[collection]!.AsObject(); var after = rcontent[collection]!.AsObject();
            foreach (var id in before.Select(p => p.Key).Union(after.Select(p => p.Key)).Order(StringComparer.Ordinal))
            {
                var target = new ElementRef { Collection = collection, Id = id };
                if (!before.ContainsKey(id) || !after.ContainsKey(id))
                {
                    Change(target, null, before.ContainsKey(id) ? "removed" : "added", "semantic", before[id], after[id]);
                    continue;
                }
                var b = before[id]!.AsObject(); var a = after[id]!.AsObject();
                if (collection == "events" && !JsonNode.DeepEquals(b["type"], a["type"])) throw new InvalidDataException("IDENTITY_CONFLICT: event variant changed.");
                if (lowners.TryGetValue(target, out var bo) && rowners.TryGetValue(target, out var ao) && bo != ao)
                    Field(target, ["owner"], JsonSerializer.SerializeToNode(bo, Json), JsonSerializer.SerializeToNode(ao, Json));
                foreach (var field in b.Select(p => p.Key).Union(a.Select(p => p.Key)).Order(StringComparer.Ordinal))
                {
                    if (field == "presentation") Presentation(target, b[field]!.AsObject(), a[field]!.AsObject());
                    else if (field is "events" or "branchOrder" or "parameterOrder") Order(target, field, b[field]!.AsArray(), a[field]!.AsArray(), "semantic");
                    else if (left.DiagramType == "er" && field == "keys")
                    {
                        var bs = b[field]!.AsArray().Select(v => v!.GetValue<string>()).Order(StringComparer.Ordinal);
                        var ns = a[field]!.AsArray().Select(v => v!.GetValue<string>()).Order(StringComparer.Ordinal);
                        if (!bs.SequenceEqual(ns, StringComparer.Ordinal)) Field(target, [field], b[field], a[field]);
                    }
                    else Field(target, [field], b[field], a[field], had: b.ContainsKey(field), has: a.ContainsKey(field));
                }
            }
        }
        return new(changes.OrderBy(c => c.Target.Collection, StringComparer.Ordinal).ThenBy(c => c.Target.Id, StringComparer.Ordinal)
            .ThenBy(c => c.Category == "semantic" ? 0 : 1).ThenBy(c => c.Field, FieldPathComparer).ToArray(), provenance);
    }

    private static JsonArray AutomaticOrder(ElementRef owner, JsonObject content, string kind)
    {
        var eligible = new List<ElementRef>();
        foreach (var (collection, node) in content)
        {
            if (node is not JsonObject map) continue;
            foreach (var (id, item) in map)
            {
                var rootCollection = kind switch
                { "graph" or "flowchart" => "nodes", "state" => "states", "class" => "types", "er" => "entities", "sequence" => "participants", _ => "elements" };
                var include = owner.Collection == "diagram"
                    ? kind == "c4" ? collection == "boundaries" || collection == "elements" && item!["boundary"] is null : collection == rootCollection
                    : owner.Collection == "types" ? collection is "attributes" or "operations" && item!["owner"]?.GetValue<string>() == owner.Id
                    : owner.Collection == "entities" ? collection == "attributes" && item!["owner"]?.GetValue<string>() == owner.Id
                    : owner.Collection == "boundaries" && collection == "elements" && item!["boundary"]?.GetValue<string>() == owner.Id;
                if (include) eligible.Add(new() { Collection = collection, Id = id });
            }
        }
        return new JsonArray(eligible.OrderBy(e => e.Collection, StringComparer.Ordinal).ThenBy(e => e.Id, StringComparer.Ordinal)
            .Select(e => JsonSerializer.SerializeToNode(e, Json)).ToArray());
    }

    private static Dictionary<ElementRef, ElementRef> Owners(JsonObject content, ElementRef root)
    {
        var owners = new Dictionary<ElementRef, ElementRef>();
        void Add(JsonArray ids, string collection, ElementRef parent)
        {
            foreach (var id in ids) owners.Add(new() { Collection = collection, Id = id!.GetValue<string>() }, parent);
        }
        if (content["timeline"] is JsonArray timeline) Add(timeline, "events", root);
        if (content["branches"] is JsonObject branches)
            foreach (var (id, branch) in branches) Add(branch!["events"]!.AsArray(), "events", new() { Collection = "branches", Id = id });
        if (content["events"] is JsonObject events)
            foreach (var (id, item) in events)
                if (item!["branchOrder"] is JsonArray order) Add(order, "branches", new() { Collection = "events", Id = id });
        if (content["operations"] is JsonObject operations)
            foreach (var (id, operation) in operations) Add(operation!["parameterOrder"]!.AsArray(), "parameters", new() { Collection = "operations", Id = id });
        return owners;
    }
}
