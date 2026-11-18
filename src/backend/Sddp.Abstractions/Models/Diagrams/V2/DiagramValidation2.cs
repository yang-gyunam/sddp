using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>Validates the SDDP schema, references, ownership, order and import metadata.</summary>
public static class DiagramValidation2
{
    private static readonly NullabilityInfoContext Nullability = new();
    private static void Require(bool condition, string error)
    {
        if (!condition) throw new InvalidDataException(error);
    }
    private static void OneOf(string value, params string[] allowed) =>
        Require(allowed.Contains(value, StringComparer.Ordinal), $"INVALID_MODEL: unsupported value '{value}'.");

    public static void Validate(DiagramRevision2 model)
    {
        Require(model is not null, "INVALID_MODEL: expected model.");
        Required(model!);
        Require(model!.SchemaVersion == 2, "UNSUPPORTED_SCHEMA: expected schema2.");
        Require(!string.IsNullOrWhiteSpace(model.DiagramId) && model.Revision > 0, "INVALID_MODEL: expected document and positive revision.");
        Require(!string.IsNullOrWhiteSpace(model.Source.Format) && !string.IsNullOrWhiteSpace(model.Source.Provider)
            && !string.IsNullOrWhiteSpace(model.Source.ProviderVersion), "INVALID_MODEL: incomplete source provenance.");
        OneOf(model.SourceState, "aligned", "stale", "unknown", "notApplicable");
        var maps = Collections(model);
        foreach (var (collection, entries) in maps)
            foreach (var (id, item) in entries)
            {
                Require(!string.IsNullOrWhiteSpace(id), "INVALID_MODEL: empty element ID.");
                Presentation(PresentationOf(item), null);
            }
        var root = new List<ElementRef>();
        void Roots(string collection, Func<object, bool>? predicate = null)
        {
            foreach (var (id, item) in maps[collection])
                if (predicate is null || predicate(item)) root.Add(new() { Collection = collection, Id = id });
        }
        void Endpoints<T>(IEnumerable<T> values, Func<T, string> from, Func<T, string> to, string collection)
        {
            foreach (var value in values) { Reference(collection, from(value)); Reference(collection, to(value)); }
        }
        void Reference(string collection, string id) =>
            Require(maps.TryGetValue(collection, out var entries) && entries.ContainsKey(id), $"INVALID_REFERENCE: {collection}/{id}.");

        switch (model.Content)
        {
            case GraphContent2 graph:
                Require(model.DiagramType == "graph", "INVALID_MODEL: content discriminator.");
                Roots("nodes"); Endpoints(graph.Edges.Values, e => e.From, e => e.To, "nodes"); break;
            case FlowchartContent2 flow:
                Require(model.DiagramType == "flowchart", "INVALID_MODEL: content discriminator.");
                Roots("nodes"); Endpoints(flow.Edges.Values, e => e.From, e => e.To, "nodes");
                foreach (var n in flow.Nodes.Values) OneOf(n.Kind, "process", "terminator", "decision"); break;
            case StateContent2 state:
                Require(model.DiagramType == "state", "INVALID_MODEL: content discriminator.");
                Roots("states"); Endpoints(state.Transitions.Values, e => e.From, e => e.To, "states");
                foreach (var (id, n) in state.States)
                {
                    OneOf(n.Kind, "state", "initial", "final");
                    if (n.Kind == "state") continue;
                    Require(n.Name == "", "INVALID_MODEL: pseudo-state has no name.");
                    Require(state.States.Values.Count(s => s.Kind == n.Kind) == 1, "INVALID_MODEL: duplicate pseudo-state.");
                    if (n.Kind == "initial") Require(!state.Transitions.Values.Any(e => e.To == id)
                        && state.Transitions.Values.Count(e => e.From == id) <= 1, "INVALID_REFERENCE: initial state direction.");
                    if (n.Kind == "final") Require(!state.Transitions.Values.Any(e => e.From == id), "INVALID_REFERENCE: final state direction.");
                }
                break;
            case ClassContent2 classes:
                Require(model.DiagramType == "class", "INVALID_MODEL: content discriminator.");
                Roots("types"); Endpoints(classes.Relations.Values, e => e.From, e => e.To, "types");
                foreach (var r in classes.Relations.Values) OneOf(r.Kind, "inheritance", "composition", "aggregation", "association", "dependency", "realization");
                foreach (var a in classes.Attributes.Values) { Reference("types", a.Owner); Visibility(a.Visibility); }
                Require(classes.Attributes.Values.Select(a => (a.Owner, a.Name)).Distinct().Count() == classes.Attributes.Count, "INVALID_MODEL: duplicate attribute name.");
                var parameters = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (id, op) in classes.Operations)
                {
                    Reference("types", op.Owner); Visibility(op.Visibility);
                    foreach (var p in op.ParameterOrder) { Reference("parameters", p); Require(parameters.Add(p), "INVALID_ORDER: parameter ownership."); }
                    Require(op.ParameterOrder.Select(p => classes.Parameters[p].Name).Distinct(StringComparer.Ordinal).Count() == op.ParameterOrder.Count, "INVALID_MODEL: duplicate parameter name.");
                    if (op.ParameterOrder.All(p => classes.Parameters[p].TypeName is not null))
                        Require(!classes.Operations.Any(pair => pair.Key != id && pair.Value.Owner == op.Owner && pair.Value.Name == op.Name
                            && pair.Value.ParameterOrder.Select(p => classes.Parameters.GetValueOrDefault(p)?.TypeName).SequenceEqual(op.ParameterOrder.Select(p => classes.Parameters[p].TypeName), StringComparer.Ordinal)), "INVALID_MODEL: duplicate operation signature.");
                }
                Require(parameters.Count == classes.Parameters.Count, "INVALID_REFERENCE: orphan parameter.");
                foreach (var (id, type) in classes.Types)
                    Presentation(type.Presentation, classes.Attributes.Where(a => a.Value.Owner == id).Select(a => Ref("attributes", a.Key))
                        .Concat(classes.Operations.Where(o => o.Value.Owner == id).Select(o => Ref("operations", o.Key))));
                break;
            case ErContent2 er:
                Require(model.DiagramType == "er", "INVALID_MODEL: content discriminator.");
                Roots("entities"); Endpoints(er.Relations.Values, r => r.From, r => r.To, "entities");
                foreach (var a in er.Attributes.Values)
                {
                    Reference("entities", a.Owner);
                    foreach (var key in a.Keys) OneOf(key, "primary", "foreign", "unique");
                    Require(a.Keys.Distinct(StringComparer.Ordinal).Count() == a.Keys.Count, "INVALID_MODEL: duplicate key role.");
                }
                Require(er.Attributes.Values.Select(a => (a.Owner, a.Name)).Distinct().Count() == er.Attributes.Count, "INVALID_MODEL: duplicate attribute name.");
                foreach (var r in er.Relations.Values) { Cardinality(r.FromCardinality); Cardinality(r.ToCardinality); }
                foreach (var (id, entity) in er.Entities)
                    Presentation(entity.Presentation, er.Attributes.Where(a => a.Value.Owner == id).Select(a => Ref("attributes", a.Key)));
                break;
            case C4Content2 c4:
                Require(model.DiagramType == "c4", "INVALID_MODEL: content discriminator.");
                OneOf(c4.View, "context", "container");
                Require(c4.Boundaries.Count <= (c4.View == "context" ? 0 : 1), "INVALID_MODEL: C4 boundary scope.");
                Roots("boundaries"); Roots("elements", e => ((C4Element2)e).Boundary is null);
                Endpoints(c4.Relations.Values, r => r.From, r => r.To, "elements");
                foreach (var e in c4.Elements.Values)
                {
                    OneOf(e.Kind, "person", "system", "externalSystem", "container", "database");
                    var inside = e.Kind is "container" or "database";
                    Require(!inside || c4.View == "container", "INVALID_MODEL: container in context view.");
                    if (inside) { Require(e.Boundary is not null, "INVALID_REFERENCE: internal C4 element needs boundary."); Reference("boundaries", e.Boundary!); }
                    else Require(e.Boundary is null && e.Technology is null, "INVALID_MODEL: external C4 boundary/technology.");
                }
                foreach (var (id, boundary) in c4.Boundaries)
                    Presentation(boundary.Presentation, c4.Elements.Where(e => e.Value.Boundary == id).Select(e => Ref("elements", e.Key)));
                break;
            case SequenceContent2 sequence:
                Require(model.DiagramType == "sequence", "INVALID_MODEL: content discriminator.");
                Roots("participants");
                foreach (var p in sequence.Participants.Values) OneOf(p.Kind, "actor", "participant");
                Sequence(sequence); break;
            default: throw new InvalidDataException("UNSUPPORTED_SCHEMA: content type.");
        }
        foreach (var entries in maps.Values)
            foreach (var item in entries.Values)
                if (item is not (ClassType2 or ErEntity2 or C4Boundary2)) Presentation(PresentationOf(item), []);
        Presentation(model.Presentation, root);
        if (model.SourceMap is { } sourceMap)
        {
            Require(sourceMap.SourceHash == "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(model.Source.Text))), "INVALID_MODEL: source hash mismatch.");
            Require(!string.IsNullOrWhiteSpace(sourceMap.Profile.Id) && sourceMap.Profile.Version > 0
                && !string.IsNullOrWhiteSpace(sourceMap.Adapter.Id) && !string.IsNullOrWhiteSpace(sourceMap.Adapter.Version), "INVALID_MODEL: source profile/adapter.");
            var targets = new HashSet<ElementRef>(); var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in sourceMap.Bindings)
            {
                Reference(binding.Element.Collection, binding.Element.Id);
                Require(targets.Add(binding.Element), "IDENTITY_CONFLICT: duplicate binding.");
                if (binding.Key is { } key)
                {
                    Require(key.Count > 0 && key.All(k => !string.IsNullOrWhiteSpace(k)), "INVALID_MODEL: empty source key.");
                    Require(keys.Add(System.Text.Json.JsonSerializer.Serialize(key)), "IDENTITY_CONFLICT: duplicate source key.");
                }
                if (binding.Range is { } range) Require(range.Start >= 0 && range.End >= range.Start
                    && range.End <= model.Source.Text.Length, "INVALID_MODEL: UTF16 range.");
            }
        }
    }

    private static ElementRef Ref(string collection, string id) => new() { Collection = collection, Id = id };
    private static void Visibility(string value) => OneOf(value, "public", "private", "protected", "internal", "unspecified");
    private static void Cardinality(string value) => OneOf(value, "zeroOrOne", "one", "zeroOrMore", "oneOrMore");
    private static DiagramPresentation2 PresentationOf(object value) =>
        (DiagramPresentation2)value.GetType().GetProperty("Presentation")!.GetValue(value)!;

    private static void Presentation(DiagramPresentation2 value, IEnumerable<ElementRef>? eligible)
    {
        Require(value.Properties.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value is not null), "INVALID_MODEL: presentation properties.");
        var expected = eligible?.ToHashSet();
        // A null scope defers container membership, but still checks properties/duplicates.
        if (value.Order.Count == 0) return;
        Require(value.Order.Count == value.Order.Distinct().Count(), "INVALID_ORDER: duplicate presentation target.");
        if (expected is not null) Require(expected.SetEquals(value.Order), "INVALID_ORDER: expected complete child permutation.");
    }

    private static Dictionary<string, Dictionary<string, object>> Collections(DiagramRevision2 model)
    {
        var result = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
        foreach (var field in model.Content.GetType().GetProperties())
        {
            if (!field.PropertyType.IsGenericType || field.PropertyType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) continue;
            var name = char.ToLowerInvariant(field.Name[0]) + field.Name[1..];
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in (IDictionary)field.GetValue(model.Content)!) values.Add((string)entry.Key, entry.Value!);
            result.Add(name, values);
        }
        return result;
    }

    private static void Required(object value)
    {
        if (value is string) return;
        if (value is IDictionary map) { foreach (DictionaryEntry item in map) { Require(item.Value is not null, "INVALID_MODEL: null map value."); Required(item.Value!); } return; }
        if (value is IEnumerable list) { foreach (var item in list) { Require(item is not null, "INVALID_MODEL: null list element."); Required(item!); } return; }
        if (!value.GetType().IsClass) return;
        foreach (var field in value.GetType().GetProperties())
        {
            var child = field.GetValue(value);
            if (child is null)
            {
                lock (Nullability) Require(Nullability.Create(field).ReadState == NullabilityState.Nullable, $"INVALID_MODEL: required {field.Name}.");
            }
            else Required(child);
        }
    }

    private static void Sequence(SequenceContent2 model)
    {
        var seenEvents = new HashSet<string>(StringComparer.Ordinal); var seenBranches = new HashSet<string>(StringComparer.Ordinal);
        void Walk(List<string> order, int depth)
        {
            Require(depth <= 64, "INVALID_ORDER: fragment nesting limit.");
            var stacks = new Dictionary<string, Stack<string>>(StringComparer.Ordinal);
            foreach (var id in order)
            {
                Require(model.Events.ContainsKey(id) && seenEvents.Add(id), "INVALID_ORDER: missing/reused/cyclic event.");
                switch (model.Events[id])
                {
                    case SequenceMessage2 message:
                        Require(model.Participants.ContainsKey(message.From) && model.Participants.ContainsKey(message.To), "INVALID_REFERENCE: message participant.");
                        OneOf(message.Kind, "solidArrow", "dashedArrow"); break;
                    case SequenceActivationStart2 start:
                        Require(model.Participants.ContainsKey(start.Participant), "INVALID_REFERENCE: activation participant.");
                        if (!stacks.TryGetValue(start.Participant, out var stack)) stacks[start.Participant] = stack = new();
                        stack.Push(id); break;
                    case SequenceActivationEnd2 end:
                        Require(model.Events.TryGetValue(end.Start, out var matching) && matching is SequenceActivationStart2, "INVALID_REFERENCE: activation start.");
                        var participant = ((SequenceActivationStart2)matching!).Participant;
                        Require(stacks.TryGetValue(participant, out var active) && active.Count > 0 && active.Pop() == end.Start, "INVALID_ORDER: activation LIFO/scope."); break;
                    case SequenceFragment2 fragment:
                        OneOf(fragment.Kind, "alt", "opt", "loop");
                        Require(fragment.BranchOrder.Count == 1 || fragment.Kind == "alt" && fragment.BranchOrder.Count == 2, "INVALID_ORDER: fragment branches.");
                        for (var i = 0; i < fragment.BranchOrder.Count; i++)
                        {
                            var bid = fragment.BranchOrder[i];
                            Require(model.Branches.ContainsKey(bid) && seenBranches.Add(bid), "INVALID_REFERENCE: branch ownership.");
                            var branch = model.Branches[bid];
                            Require(branch.Kind == (fragment.Kind == "alt" ? i == 0 ? "condition" : "else" : "body"), "INVALID_MODEL: branch kind.");
                            Walk(branch.Events, depth + 1);
                        }
                        break;
                    default: throw new InvalidDataException("UNSUPPORTED_SCHEMA: sequence event.");
                }
            }
            Require(stacks.Values.All(stack => stack.Count == 0), "INVALID_ORDER: unclosed activation.");
        }
        Walk(model.Timeline, 0);
        Require(seenEvents.Count == model.Events.Count && seenBranches.Count == model.Branches.Count, "INVALID_REFERENCE: orphan sequence element.");
    }
}
