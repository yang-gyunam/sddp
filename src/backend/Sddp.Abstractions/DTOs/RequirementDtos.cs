using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record CreateRequirementDto(
    string Code,
    string Title,
    string Description,
    RequirementPriority Priority = RequirementPriority.Medium,
    string? ParentId = null,
    string? OwnerUserId = null);

public record UpdateRequirementDto(
    string Title,
    string Description,
    RequirementPriority? Priority = null,
    string? OwnerUserId = null,
    string? ParentId = null);

public record RequirementDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Code,
    string Title,
    string Description,
    RequirementLevel Level,
    RequirementPriority Priority,
    RequirementStatus Status,
    string? ParentId,
    string? ConversationId,
    string Version,
    int ChildrenCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record RequirementAncestorDto(
    string Id,
    string Code,
    string Title,
    RequirementLevel Level);

public record LinkedSpecDto(
    string Id,
    string Code,
    string Title,
    SpecStatus Status);

public record RequirementDetailDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Code,
    string Title,
    string Description,
    RequirementLevel Level,
    RequirementPriority Priority,
    RequirementStatus Status,
    string? ParentId,
    string? ParentCode,
    string? ParentTitle,
    RequirementLevel? ParentLevel,
    string? ConversationId,
    string? ConversationName,
    string? ConversationDescription,
    ConversationType? ConversationType,
    string Version,
    IEnumerable<RequirementAncestorDto> Ancestors,
    IEnumerable<RequirementAncestorDto> Siblings,
    IEnumerable<RequirementDto> Children,
    IEnumerable<LinkedSpecDto> LinkedSpecs,
    UserRefDto? Owner,
    UserRefDto CreatedBy,
    UserRefDto UpdatedBy,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record RequirementPageDto(
    IEnumerable<RequirementDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record RequirementTreeNodeDto(
    string Id,
    string Code,
    string Title,
    RequirementLevel Level,
    RequirementPriority Priority,
    RequirementStatus Status,
    string? ParentId,
    int ChildrenCount,
    List<RequirementTreeNodeDto> Children);

public record RequirementSummaryDto(
    string Id,
    string Code,
    string Title,
    RequirementLevel Level);

public record TransitionStatusDto(
    RequirementStatus NewStatus);

public record LinkConversationDto(
    string ConversationId);

public record LinkedRequirementDto(
    string Id,
    string Code,
    string Title,
    RequirementLevel Level,
    RequirementPriority Priority,
    DateTimeOffset LinkedAt);

public record RequirementVersionDto(
    string Id,
    string Code,
    string Title,
    string Description,
    RequirementLevel Level,
    RequirementPriority Priority,
    RequirementStatus Status,
    string? ParentId,
    string? ConversationId,
    string Version,
    UserRefDto? Owner,
    UserRefDto? CreatedBy,
    UserRefDto? UpdatedBy,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
