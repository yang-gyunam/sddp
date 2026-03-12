namespace Sddp.Abstractions.DTOs;

public record CreateRelationshipDto(
    string FromEntityType,
    string FromEntityId,
    string ToEntityType,
    string ToEntityId,
    string Type,
    string? Reason = null,
    string? SourceSpecId = null,
    string? SourceDecisionId = null);

public record RelationshipDto(
    string Id,
    string TenantId,
    string ProjectId,
    string FromEntityType,
    string FromEntityId,
    string? FromEntityLabel,
    string? FromEntityCode,
    string ToEntityType,
    string ToEntityId,
    string? ToEntityLabel,
    string? ToEntityCode,
    string Type,
    string? Reason,
    string? SourceSpecId,
    string? SourceDecisionId,
    UserRefDto CreatedBy,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsCurrent,
    DateTimeOffset CreatedAt);

public record RelationshipListDto(
    IEnumerable<RelationshipDto> Incoming,
    IEnumerable<RelationshipDto> Outgoing);

public record GraphNodeDto(
    string Id,
    string EntityType,
    string Label,
    string? Status,
    int Depth);

public record GraphEdgeDto(
    string Id,
    string SourceId,
    string TargetId,
    string Type,
    string TypeLabel);

public record RelationshipGraphDto(
    IEnumerable<GraphNodeDto> Nodes,
    IEnumerable<GraphEdgeDto> Edges);

public record RelationshipValidationResultDto(
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage);

public record DecisionImpactDto(
    int TotalCount,
    IEnumerable<DecisionImpactItemDto> Items);

public record DecisionImpactItemDto(
    string EntityType,
    string EntityId,
    string Label,
    string RelationType);
