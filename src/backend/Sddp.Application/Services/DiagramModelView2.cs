using System.Text.Json;
using System.Text.Json.Nodes;
using Sddp.Abstractions.Models.Diagrams.V2;

namespace Sddp.Application.Services;

/// <summary>Private normalized SDDP view used for identity comparison and ID remapping.</summary>
internal sealed class DiagramModelView2
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public JsonObject Root { get; }
    public JsonObject Content => Root["content"]!.AsObject();
    public string Kind => Root["diagramType"]!.GetValue<string>();
    public Dictionary<ElementRef, JsonObject> Elements { get; } = new();
    public Dictionary<ElementRef, ElementRef> Owners { get; } = new();
    public DiagramModelView2(DiagramRevision2 model)
    {
        Root = JsonSerializer.SerializeToNode(model, Json)!.AsObject();
        var diagram = new ElementRef { Collection = "diagram", Id = model.DiagramId };
        foreach (var (collection, map) in Content)
            if (map is JsonObject objects)
                foreach (var (id, element) in objects) Elements.Add(new() { Collection = collection, Id = id }, element!.AsObject());
        foreach (var (reference, element) in Elements)
        {
            if (element["owner"] is JsonValue owner) Owners[reference] = new() { Collection = Kind == "class" ? "types" : "entities", Id = owner.GetValue<string>() };
            else if (Kind == "c4" && reference.Collection == "elements" && element["boundary"] is JsonValue boundary) Owners[reference] = new() { Collection = "boundaries", Id = boundary.GetValue<string>() };
            else Owners[reference] = diagram;
            foreach (var field in new[] { "events", "branchOrder", "parameterOrder" })
                if (element[field] is JsonArray ids)
                    foreach (var id in ids) Owners[new() { Collection = ChildCollection(field), Id = id!.GetValue<string>() }] = reference;
        }
        // Child ownership assignments must override any earlier default root assignment.
        foreach (var (reference, element) in Elements)
            foreach (var field in new[] { "events", "branchOrder", "parameterOrder" })
                if (element[field] is JsonArray ids)
                    foreach (var id in ids) Owners[new() { Collection = ChildCollection(field), Id = id!.GetValue<string>() }] = reference;
    }
    public static string ChildCollection(string field) => field switch { "branchOrder" => "branches", "parameterOrder" => "parameters", _ => "events" };
    public string? ReferenceCollection(ElementRef element, string field) => (Kind, element.Collection, field) switch
    {
        ("graph" or "flowchart", "edges", "from" or "to") => "nodes",
        ("state", "transitions", "from" or "to") => "states",
        ("class", "relations", "from" or "to") => "types",
        ("class", "attributes" or "operations", "owner") => "types",
        ("er", "relations", "from" or "to") => "entities",
        ("er", "attributes", "owner") => "entities",
        ("c4", "elements", "boundary") => "boundaries",
        ("c4", "relations", "from" or "to") => "elements",
        ("sequence", "events", "from" or "to" or "participant") => "participants",
        ("sequence", "events", "start") => "events",
        _ => null
    };
    public string? Signature(ElementRef reference, IReadOnlyDictionary<ElementRef, ElementRef>? mapped, bool includeOwner = true)
    {
        if (!Elements.TryGetValue(reference, out var element)) return null;
        var result = new JsonObject();
        if (includeOwner && Owners.TryGetValue(reference, out var parent))
        {
            if (parent.Collection != "diagram" && mapped is not null && !mapped.TryGetValue(parent, out parent!)) return null;
            result["$owner"] = JsonSerializer.SerializeToNode(parent, Json);
        }
        foreach (var (field, value) in element)
        {
            if (field == "presentation") continue;
            if (field is "events" or "branchOrder" or "parameterOrder")
            {
                var children = new JsonArray();
                foreach (var id in value!.AsArray())
                {
                    var child = Signature(new() { Collection = ChildCollection(field), Id = id!.GetValue<string>() }, mapped, false);
                    if (child is null) return null;
                    children.Add(child);
                }
                result[field] = children;
            }
            else if (ReferenceCollection(reference, field) is { } collection && value is JsonValue scalar)
            {
                var target = new ElementRef { Collection = collection, Id = scalar.GetValue<string>() };
                if (mapped is not null && !mapped.TryGetValue(target, out target!)) return null;
                result[field] = JsonSerializer.SerializeToNode(target, Json);
            }
            else if (Kind == "er" && field == "keys") result[field] = new JsonArray(value!.AsArray().Select(n => n!.GetValue<string>()).Order(StringComparer.Ordinal).Select(s => JsonValue.Create(s) as JsonNode).ToArray());
            else result[field] = value?.DeepClone();
        }
        return Canonical(result);
    }

    public DiagramRevision2 Remap(IReadOnlyDictionary<ElementRef, ElementRef> map, DiagramRevision2? baseline = null)
    {
        var output = Root.DeepClone().AsObject(); var content = output["content"]!.AsObject();
        var previous = baseline is null ? null : new DiagramModelView2(baseline);
        foreach (var collection in content.Where(p => p.Value is JsonObject).Select(p => p.Key).ToArray())
        {
            var remapped = new JsonObject();
            foreach (var (id, item) in content[collection]!.AsObject())
            {
                var reference = new ElementRef { Collection = collection, Id = id }; var target = map[reference];
                var element = item!.DeepClone().AsObject();
                foreach (var field in element.Select(p => p.Key).ToArray())
                {
                    if (field is "events" or "branchOrder" or "parameterOrder")
                    {
                        var childCollection = ChildCollection(field);
                        element[field] = new JsonArray(element[field]!.AsArray().Select(n => JsonValue.Create(map[new() { Collection = childCollection, Id = n!.GetValue<string>() }].Id) as JsonNode).ToArray());
                    }
                    else if (ReferenceCollection(reference, field) is { } refCollection && element[field] is JsonValue scalar)
                        element[field] = map[new() { Collection = refCollection, Id = scalar.GetValue<string>() }].Id;
                }
                if (previous?.Elements.TryGetValue(target, out var old) == true) element["presentation"] = old["presentation"]!.DeepClone();
                else RemapOrder(element["presentation"]!.AsObject(), map);
                remapped.Add(target.Id, element);
            }
            content[collection] = remapped;
        }
        if (content["timeline"] is JsonArray timeline) content["timeline"] = new JsonArray(timeline.Select(n => JsonValue.Create(map[new() { Collection = "events", Id = n!.GetValue<string>() }].Id) as JsonNode).ToArray());
        if (previous is not null)
        {
            var incoming = output["presentation"]!["properties"]!.DeepClone().AsObject();
            output["presentation"] = previous.Root["presentation"]!.DeepClone();
            foreach (var (key, value) in incoming) output["presentation"]!["properties"]![key] = value?.DeepClone();
        }
        else RemapOrder(output["presentation"]!.AsObject(), map);
        if (output["sourceMap"] is JsonObject sourceMap)
            foreach (var binding in sourceMap["bindings"]!.AsArray())
            {
                var oldRef = binding!["element"]!.Deserialize<ElementRef>(Json)!;
                binding["element"] = JsonSerializer.SerializeToNode(map[oldRef], Json);
            }
        var model = output.Deserialize<DiagramRevision2>(Json)!;
        // Reconcile explicit display orders with the new ownership/membership.
        var view = new DiagramModelView2(model);
        void Reconcile(JsonObject presentation, ElementRef parent)
        {
            var order = presentation["order"]!.AsArray(); if (order.Count == 0) return;
            var eligible = view.Elements.Keys.Where(r => DisplayChild(view, r, parent)).ToHashSet();
            var refs = order.Select(n => n!.Deserialize<ElementRef>(Json)!).Where(eligible.Contains).Distinct().ToList();
            refs.AddRange(eligible.Except(refs).OrderBy(r => r.Collection, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal));
            presentation["order"] = new JsonArray(refs.Select(r => JsonSerializer.SerializeToNode(r, Json)).ToArray());
        }
        foreach (var (reference, element) in view.Elements) Reconcile(element["presentation"]!.AsObject(), reference);
        Reconcile(view.Root["presentation"]!.AsObject(), new() { Collection = "diagram", Id = model.DiagramId });
        var result = view.Root.Deserialize<DiagramRevision2>(Json)!;
        DiagramValidation2.Validate(result); return result;
    }
    private static bool DisplayChild(DiagramModelView2 view, ElementRef child, ElementRef parent)
    {
        if (view.Owners.GetValueOrDefault(child) != parent) return false;
        return parent.Collection switch
        {
            "diagram" => view.Kind switch { "graph" or "flowchart" => child.Collection == "nodes", "state" => child.Collection == "states", "sequence" => child.Collection == "participants", "class" => child.Collection == "types", "er" => child.Collection == "entities", _ => child.Collection is "elements" or "boundaries" },
            "types" => child.Collection is "attributes" or "operations", "entities" => child.Collection == "attributes", "boundaries" => child.Collection == "elements", _ => false
        };
    }
    private static void RemapOrder(JsonObject presentation, IReadOnlyDictionary<ElementRef, ElementRef> map) =>
        presentation["order"] = new JsonArray(presentation["order"]!.AsArray().Select(n => JsonSerializer.SerializeToNode(map[n!.Deserialize<ElementRef>(Json)!], Json)).ToArray());
    internal static string Canonical(JsonNode? value) => value switch
    {
        JsonObject map => "{" + string.Join(",", map.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => JsonSerializer.Serialize(p.Key) + ":" + Canonical(p.Value))) + "}",
        JsonArray list => "[" + string.Join(",", list.Select(Canonical)) + "]", _ => value?.ToJsonString() ?? "null"
    };
}
