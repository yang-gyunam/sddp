namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>A transient source candidate key maps to an existing identity, or null for a new element.</summary>
public sealed record DiagramIdentityMapping2(string CandidateKey, ElementRef? Target);
/// <summary>Explicit decisions bound to the exact source and baseline preview.</summary>
public sealed record DiagramIdentityResolution2(string Fingerprint, IReadOnlyList<DiagramIdentityMapping2> Mappings, IReadOnlyList<ElementRef> Removed);
/// <summary>Evidence for an automatic/explicit match or unresolved candidate.</summary>
public sealed record DiagramIdentityItem2(string CandidateKey, string Collection, string Label, string Reason, ElementRef? Target, IReadOnlyList<ElementRef> Candidates);
/// <summary>Non-persisting import preview; errors never expose an applicable partial model.</summary>
public sealed record DiagramIdentityPreview2(string Fingerprint, DiagramRevision2? Candidate, IReadOnlyList<DiagramDiagnostic2> Diagnostics,
    IReadOnlyList<DiagramIdentityItem2> IdentityReport, IReadOnlyList<ElementRef> Removed, DiagramChangeSet2? Changes);
