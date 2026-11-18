namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>Provider-independent parsing or identity diagnostic with UTF16 source coordinates.</summary>
public sealed record DiagramDiagnostic2(string Code, string Severity, string Message, DiagramSourceRange2? SourceRange = null, string[]? ModelPath = null, ElementRef? Target = null);
/// <summary>No candidate is exposed when any parser or common-model error occurs.</summary>
public sealed record DiagramParseResult2(DiagramRevision2? Candidate, IReadOnlyList<DiagramDiagnostic2> Diagnostics);
/// <summary>Replaceable parser capability; source generation is a separate capability.</summary>
public sealed record DiagramParserCapability2(string Id, string Version, IReadOnlyList<DiagramProfile2> Profiles, bool GeneratesEditedSource);
/// <summary>Text adapter boundary. IDs come from the SDDP host, never external AST/DOM identity.</summary>
public interface IDiagramParser2
{
    DiagramParserCapability2 Capability { get; }
    DiagramParseResult2 Parse(string text, DiagramProfile2? profile, string diagramId, int revision);
}
