using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record CreateGlossaryTermDto(
    string Term,
    string Definition,
    TermCategory Category,
    string? Source = null,
    string? Synonyms = null,
    string? Abbreviation = null,
    List<string>? UsageExamples = null,
    List<string>? RelatedTermIds = null,
    string? SourceSpecId = null,
    string? SourceConversationId = null,
    string? SourceRequirementId = null,
    string? OwnerUserId = null);

public record UpdateGlossaryTermDto(
    string? Definition = null,
    TermCategory? Category = null,
    string? Source = null,
    string? Synonyms = null,
    string? Abbreviation = null,
    List<string>? UsageExamples = null,
    List<string>? RelatedTermIds = null,
    string? SourceSpecId = null,
    string? SourceConversationId = null,
    string? SourceRequirementId = null,
    string? OwnerUserId = null);

public record GlossaryTermDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Term,
    string Definition,
    TermCategory Category,
    GlossaryTermStatus Status,
    string? Synonyms,
    string? Abbreviation,
    string Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record GlossaryTermDetailDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Term,
    string Definition,
    TermCategory Category,
    GlossaryTermStatus Status,
    List<string> UsageExamples,
    List<string> RelatedTermIds,
    string? Source,
    string? Synonyms,
    string? Abbreviation,
    UserRefDto DefinedBy,
    UserRefDto? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    string? ReplacedByTermId,
    string? ReplacedByTermName,
    string? SourceSpecId,
    string? SourceSpecCode,
    string? SourceSpecTitle,
    string? SourceConversationId,
    string? SourceConversationName,
    string? SourceConversationType,
    string? SourceRequirementId,
    string? SourceRequirementCode,
    string? SourceRequirementTitle,
    UserRefDto? Owner,
    string Version,
    UserRefDto CreatedBy,
    UserRefDto UpdatedBy,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record GlossaryTermSummaryDto(
    string Id,
    string Term,
    string Definition,
    TermCategory Category,
    GlossaryTermStatus Status);

public record GlossaryTermPageDto(
    IEnumerable<GlossaryTermDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record GlossaryConflictResultDto(
    bool HasConflict,
    List<GlossaryConflictDto> Conflicts);

public record GlossaryConflictDto(
    string TermId,
    string Term,
    string ConflictType,
    string Message);

public record GlossaryTermUsageDto(
    string TermId,
    string Term,
    int UsageCount,
    List<GlossaryTermUsageItemDto> Usages);

public record GlossaryTermUsageItemDto(
    string EntityType,
    string EntityId,
    string EntityTitle,
    string FieldName);

public record DeprecateGlossaryTermDto(
    string? ReplacementTermId = null);

public record GlossaryTermVersionDto(
    string Id,
    string Term,
    string Definition,
    TermCategory Category,
    GlossaryTermStatus Status,
    string? Synonyms,
    string? Abbreviation,
    string Version,
    UserRefDto? DefinedBy,
    UserRefDto? UpdatedBy,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
