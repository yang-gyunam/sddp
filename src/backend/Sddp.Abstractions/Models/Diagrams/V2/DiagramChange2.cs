using System.Text.Json.Nodes;

namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>An SDDP element/field difference; detached snapshots, never an executable patch.</summary>
public sealed record DiagramChange2(ElementRef Target, string[]? Field, string Kind, string Category, JsonNode? Before, JsonNode? After);
/// <summary>Source metadata changes are separate from structural/presentation counts.</summary>
public sealed record DiagramProvenanceChange2(string[] Field, JsonNode? Before, JsonNode? After);
/// <summary>Complete structural comparison of two validated revisions of the same document/type.</summary>
public sealed record DiagramChangeSet2(IReadOnlyList<DiagramChange2> Changes, IReadOnlyList<DiagramProvenanceChange2> ProvenanceChanges);
