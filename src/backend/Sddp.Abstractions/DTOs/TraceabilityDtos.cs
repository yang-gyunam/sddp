namespace Sddp.Abstractions.DTOs;

/// <summary>
/// (FK)
/// </summary>
public record TraceabilityNodeDto(
    string Id,
    string EntityType,
    string Label,
    string? Code,
    string? Status,
    int Layer,
    string? ParentId);

/// <summary>
/// (Relationship)
/// </summary>
public record TraceabilityCrossLinkDto(
    string Id,
    string SourceId,
    string TargetId,
    string Type,
    string TypeLabel);

/// <summary>
///
/// </summary>
public record TraceabilityMapStatsDto(
    int ConversationCount,
    int RequirementCount,
    int SpecCount,
    int TaskCount,
    int ArtifactCount,
    int CrossLinkCount);

/// <summary>
///
/// </summary>
public record TraceabilityMapDto(
    IReadOnlyList<TraceabilityNodeDto> Nodes,
    IReadOnlyList<TraceabilityCrossLinkDto> CrossLinks,
    TraceabilityMapStatsDto Stats);
