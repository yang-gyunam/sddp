using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sddp.Abstractions.Models.Diagrams.V2;

namespace Sddp.Application.Services;

/// <summary>Conservative host identity reconciliation, independent of parser implementation details.</summary>
public static class DiagramIdentityService2
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static string Hash(string text) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static string Key(DiagramBinding2 binding) => binding.Key is not null
        ? JsonSerializer.Serialize(binding.Key) : JsonSerializer.Serialize(new { binding.Element.Collection, binding.Range });

    public static DiagramIdentityPreview2 Preview(IDiagramParser2 parser, string text, DiagramProfile2? profile,
        string diagramId, int revision, DiagramRevision2? baseline = null, DiagramIdentityResolution2? resolution = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (baseline is not null) DiagramValidation2.Validate(baseline);
        var fingerprint = Hash(DiagramModelView2.Canonical(JsonSerializer.SerializeToNode(new { text, profile, adapter = parser.Capability.Id, version = parser.Capability.Version, baseline }, Json)));
        var parsed = parser.Parse(text, profile, diagramId, revision);
        if (parsed.Candidate is not { } incoming || parsed.Diagnostics.Any(d => d.Severity == "error")) return new(fingerprint, null, parsed.Diagnostics, [], [], null);
        try { DiagramValidation2.Validate(incoming); }
        catch (InvalidDataException ex) { return new(fingerprint, null, [new("INVALID_MODEL", "error", ex.Message)], [], [], null); }
        var next = new DiagramModelView2(incoming); var previous = baseline is null ? null : new DiagramModelView2(baseline);
        if (incoming.SourceMap is null || incoming.SourceMap.Bindings.Count != next.Elements.Count)
            return new(fingerprint, null, [new("UNSUPPORTED_CAPABILITY", "error", "Identity preview requires a source binding for every element.")], [], [], null);
        var bindings = incoming.SourceMap.Bindings.ToDictionary(b => b.Element);
        var keys = bindings.ToDictionary(p => p.Key, p => Hash(Key(p.Value)));
        var matches = new Dictionary<ElementRef, ElementRef>(); var reasons = new Dictionary<ElementRef, string>();
        DiagramIdentityPreview2 Failure(string code, string message) =>
            new(fingerprint, null, [new(code, "error", message)], [], [], null);
        ElementRef[] Removed() => previous?.Elements.Keys.Except(matches.Values)
            .OrderBy(r => r.Collection, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal).ToArray() ?? [];
        DiagramIdentityItem2 Item(ElementRef reference, IReadOnlyList<ElementRef> candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var element = next.Elements[reference];
            var label = element["name"] ?? element["label"] ?? element["text"] ?? element["type"];
            return new(keys[reference], reference.Collection, label?.GetValue<string>() ?? reference.Collection,
                reasons.GetValueOrDefault(reference, "ambiguous"), matches.GetValueOrDefault(reference), candidates);
        }
        if (baseline is not null && (baseline.DiagramId != diagramId || baseline.Revision != revision || baseline.DiagramType != incoming.DiagramType
            || baseline.Content is C4Content2 bc && incoming.Content is C4Content2 nc && bc.View != nc.View))
            return Failure("INCOMPATIBLE_DIAGRAM_TYPE", "Document, revision, diagram type and C4 view must match the baseline.");
        if (keys.Values.Distinct(StringComparer.Ordinal).Count() != keys.Count) return Failure("IDENTITY_AMBIGUOUS", "Parser candidates do not have unique transient source keys.");

        var aligned = false;
        if (baseline?.SourceMap is { } oldMap && oldMap.Profile == incoming.SourceMap.Profile)
        {
            var oldParse = parser.Parse(baseline.Source.Text, oldMap.Profile, diagramId, revision).Candidate;
            if (oldParse?.SourceMap is { } reparsed)
            {
                // Verify claimed source keys/ranges against the previous source parser, not sourceState.
                var parserBindings = reparsed.Bindings.Select(b => (b.Element.Collection, Key(b), b.Range)).ToHashSet();
                var verified = oldMap.Bindings.Where(b => parserBindings.Contains((b.Element.Collection, Key(b), b.Range)))
                    .ToLookup(b => (b.Element.Collection, Key(b)));
                ElementRef? UniquePrevious(DiagramBinding2 binding)
                {
                    var candidates = verified[(binding.Element.Collection, Key(binding))];
                    return candidates.Count() == 1 ? candidates.First().Element : null;
                }
                foreach (var (reference, binding) in bindings.Where(p => p.Value.Key is not null))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (UniquePrevious(binding) is not { } old) continue;
                    matches.Add(reference, old);
                    reasons[reference] = "namedKey";
                }
                var originalMap = new Dictionary<ElementRef, ElementRef>();
                var originalTargets = new HashSet<ElementRef>();
                foreach (var binding in reparsed.Bindings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (UniquePrevious(binding) is { } old && originalTargets.Add(old))
                        originalMap.Add(binding.Element, old);
                }
                if (originalMap.Count == new DiagramModelView2(oldParse).Elements.Count && originalMap.Count == previous!.Elements.Count)
                {
                    try { aligned = !DiagramDiff2.Compare(baseline, new DiagramModelView2(oldParse).Remap(originalMap)).Changes.Any(c => c.Category == "semantic"); }
                    catch (InvalidDataException) { aligned = false; }
                }
                if (aligned && baseline.Source.Text == text)
                    foreach (var (reference, binding) in bindings.Where(p => p.Value.Key is null))
                    {
                        if (UniquePrevious(binding) is not { } old) continue;
                        matches.Add(reference, old);
                        reasons[reference] = "exactSource";
                    }
            }
        }
        if (aligned && previous is not null)
        {
            var anonymous = baseline!.SourceMap!.Bindings.Where(b => b.Key is null).Select(b => b.Element).ToHashSet();
            var progressed = true;
            while (progressed)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progressed = false;
                var freshGroups = next.Elements.Keys.Where(r => bindings[r].Key is null && !matches.ContainsKey(r))
                    .Select(r => (Reference: r, Signature: next.Signature(r, matches))).Where(p => p.Signature is not null)
                    .GroupBy(p => (p.Reference.Collection, p.Signature)).ToDictionary(g => g.Key, g => g.Select(p => p.Reference).ToArray());
                var oldGroups = anonymous.Except(matches.Values)
                    .Select(r => (Reference: r, Signature: previous.Signature(r, null))).Where(p => p.Signature is not null)
                    .GroupBy(p => (p.Reference.Collection, p.Signature)).ToDictionary(g => g.Key, g => g.Select(p => p.Reference).ToArray());
                foreach (var (key, fresh) in freshGroups)
                    if (fresh.Length == 1 && oldGroups.TryGetValue(key, out var old) && old.Length == 1)
                    { matches.Add(fresh[0], old[0]); reasons[fresh[0]] = "uniqueSemantic"; progressed = true; }
            }
        }
        if (resolution is not null)
        {
            if (resolution.Fingerprint != fingerprint) return Failure("IDENTITY_CONFLICT", "The preview fingerprint is stale.");
            var byKey = keys.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal); var supplied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in resolution.Mappings)
            {
                if (!supplied.Add(mapping.CandidateKey) || !byKey.TryGetValue(mapping.CandidateKey, out var reference)) return Failure("IDENTITY_CONFLICT", "Unknown or duplicate candidate key.");
                cancellationToken.ThrowIfCancellationRequested();
                if (matches.ContainsKey(reference)) return Failure("IDENTITY_CONFLICT", "Cannot override a confirmed identity.");
                if (mapping.Target is not null && (previous is null || !previous.Elements.ContainsKey(mapping.Target) || mapping.Target.Collection != reference.Collection)) return Failure("IDENTITY_CONFLICT", "Invalid existing identity target.");
                matches[reference] = mapping.Target ?? reference; reasons[reference] = "explicit";
            }
            if (matches.Values.Distinct().Count() != matches.Count) return Failure("IDENTITY_CONFLICT", "An identity target was used more than once.");
            if (!Removed().ToHashSet().SetEquals(resolution.Removed) || resolution.Removed.Count != resolution.Removed.Distinct().Count()) return Failure("IDENTITY_CONFLICT", "All removed identities must be explicitly reviewed.");
        }
        var unmatched = next.Elements.Keys.Where(r => !matches.ContainsKey(r)).ToArray();
        var removed = Removed();
        if (unmatched.Length > 0 && removed.Length > 0)
        {
            // Each option is serialized once per unresolved input in the existing public DTO.
            var options = removed.GroupBy(r => r.Collection).ToDictionary(g => g.Key, g => g.ToArray());
            const int maxCandidateReferences = 4096;
            const int maxReviewBytes = 1024 * 1024;
            DiagramIdentityPreview2 Limit() => Failure("IDENTITY_REVIEW_LIMIT", "Too many identity choices. Import smaller changes.");
            long references = 0;
            // Reserve space for the envelope and diagnostic. Count each UTF-8 JSON item
            // (including escaped identifiers/labels) plus its array separator.
            long bytes = 512;
            var copies = unmatched.GroupBy(r => r.Collection).ToDictionary(g => g.Key, g => g.LongCount());
            foreach (var (collection, targets) in options)
            {
                long repeats = copies.GetValueOrDefault(collection);
                references += repeats * targets.Length;
                if (references > maxCandidateReferences) return Limit();
                foreach (var target in targets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bytes += (1 + repeats) * (1L + JsonSerializer.SerializeToUtf8Bytes(target, Json).Length);
                    if (bytes > maxReviewBytes) return Limit();
                }
            }
            var report = new List<DiagramIdentityItem2>();
            foreach (var reference in next.Elements.Keys)
            {
                var item = Item(reference, []);
                bytes += 1L + JsonSerializer.SerializeToUtf8Bytes(item, Json).Length;
                if (bytes > maxReviewBytes) return Limit();
                if (item.Target is null && options.TryGetValue(reference.Collection, out var targets))
                    item = item with { Candidates = targets };
                report.Add(item);
            }
            return new(fingerprint, null, [new("IDENTITY_AMBIGUOUS", "error",
                "Review unmatched source elements and existing identities before applying.")], report, removed, null);
        }
        foreach (var reference in unmatched) { matches.Add(reference, reference); reasons[reference] = "new"; }
        var candidate = next.Remap(matches, baseline);
        var diff = baseline is null ? null : DiagramDiff2.Compare(baseline, candidate);
        return new(fingerprint, candidate, [], next.Elements.Keys.Select(r => Item(r, [])).ToArray(), removed, diff);
    }
}
