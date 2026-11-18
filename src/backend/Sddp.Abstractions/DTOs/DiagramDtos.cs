using Sddp.Abstractions.Models.Diagrams;

namespace Sddp.Abstractions.DTOs;

/// <summary>Creates a project diagram from a common model.</summary>
public sealed record CreateDiagramDto(string Title, string Kind, DiagramRevision Model, string? ChangeSummary = null);
/// <summary>Saves against a known revision.</summary>
public sealed record UpdateDiagramDto(string Title, int ExpectedRevision, DiagramRevision Model, string? ChangeSummary = null);
/// <summary>Current diagram and its canonical revision.</summary>
public sealed record DiagramDto(string Id, string Title, string Kind, int Revision, DiagramRevision Model,
    string CreatedBy, string UpdatedBy, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
/// <summary>Diagram list summary.</summary>
public sealed record DiagramListItemDto(string Id, string Title, string Kind, int Revision, DateTimeOffset UpdatedAt);
/// <summary>Paged project diagrams.</summary>
public sealed record DiagramListDto(IReadOnlyList<DiagramListItemDto> Items, int TotalCount, int Page, int PageSize);
/// <summary>Immutable diagram revision.</summary>
public sealed record DiagramVersionDto(int Revision, string Title, DiagramRevision Model, string? ChangeSummary,
    string CreatedBy, DateTimeOffset CreatedAt);
/// <summary>Immutable revision metadata.</summary>
public sealed record DiagramVersionListItemDto(int Revision, string Title, string? ChangeSummary,
    string CreatedBy, DateTimeOffset CreatedAt);
/// <summary>Structural changes between two saved revisions.</summary>
public sealed record DiagramComparisonDto(string DiagramId, int LeftRevision, int RightRevision,
    IReadOnlyList<DiagramChange> Changes);

/// <summary>Decode exactly one of YAML text or original file bytes (base64).</summary>
public sealed record DecodeDiagramYamlDto(string? Text = null, string? ContentBase64 = null);
/// <summary>Canonical common model returned without persistence.</summary>
public sealed record DiagramModelDto(DiagramRevision Model);
/// <summary>Canonical YAML text returned without persistence.</summary>
public sealed record DiagramYamlDto(string Text);
/// <summary>Existing readable entities to project; no Diagram recursion.</summary>
public sealed record DiagramProjectionDto(DiagramOrigin[] Origins);
