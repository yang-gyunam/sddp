using Sddp.Abstractions.Models.Diagrams.V2;

namespace Sddp.Abstractions.DTOs;

/// <summary>Create an authored typed diagram or a validated graph projection.</summary>
public sealed record CreateDiagramV2Dto(string Title, string Kind, DiagramRevision2 Model, string? ChangeSummary = null);
/// <summary>Explicit schema2 save with optimistic concurrency.</summary>
public sealed record UpdateDiagramV2Dto(string Title, int ExpectedRevision, DiagramRevision2 Model, string? ChangeSummary = null);
/// <summary>Current metadata, stored schema and normalized typed model.</summary>
public sealed record DiagramV2Dto(string Id, string Title, string Kind, int Revision, int StoredSchemaVersion, DiagramRevision2 Model,
    string CreatedBy, string UpdatedBy, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
/// <summary>Summary across both stored schemas.</summary>
public sealed record DiagramV2ListItemDto(string Id, string Title, string Kind, int Revision, DateTimeOffset UpdatedAt, int StoredSchemaVersion, string DiagramType);
/// <summary>Readable paged diagram summaries.</summary>
public sealed record DiagramV2ListDto(IReadOnlyList<DiagramV2ListItemDto> Items, int TotalCount, int Page, int PageSize);
/// <summary>Immutable stored version and normalized model.</summary>
public sealed record DiagramV2VersionDto(int Revision, string Title, int StoredSchemaVersion, DiagramRevision2 Model, string? ChangeSummary, string CreatedBy, DateTimeOffset CreatedAt);
/// <summary>Exact structural changes, separate from source metadata changes.</summary>
public sealed record DiagramV2ComparisonDto(string DiagramId, int LeftRevision, int RightRevision, IReadOnlyList<DiagramChange2> Changes, IReadOnlyList<DiagramProvenanceChange2> ProvenanceChanges);
/// <summary>Normalized codec result and the schema of the input file.</summary>
public sealed record DiagramV2ModelDto(DiagramRevision2 Model, int InputSchemaVersion = 2);

/// <summary>Non-persisting source import bound to an optional saved target and current draft.</summary>
public sealed record DiagramSourcePreviewDto(string Text, DiagramProfile2? Profile = null,
    string AdapterId = "sddp-mermaid-subset", Guid? DiagramId = null, int? ExpectedRevision = null,
    DiagramRevision2? Baseline = null, DiagramIdentityResolution2? Resolution = null);
