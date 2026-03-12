using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record CreateSpecDto(
    string Code,
    string Title,
    string Description,
    string Decision,
    string Context = "",
    string Scope = "",
    string OutOfScope = "",
    string Definitions = "",
    string AcceptanceCriteria = "",
    string Owners = "",
    string ReviewTrigger = "",
    string? RequirementId = null,
    string? BornFromConversationId = null);

public record UpdateSpecDto(
    string Title,
    string Description,
    string Decision,
    string Context,
    string Scope,
    string OutOfScope,
    string Definitions,
    string AcceptanceCriteria,
    string Owners,
    string ReviewTrigger,
    string? RequirementId = null,
    string? BornFromConversationId = null);

public record SpecDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Code,
    string Title,
    string Description,
    string Decision,
    SpecStatus Status,
    string? RequirementId,
    string? BornFromConversationId,
    string? SupersedesSpecId,
    string Version,
    DateTimeOffset? LockedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SpecDetailDto(
    string Id,
    string TenantId,
    string ProjectId,
    string Code,
    string Title,
    string? Description,
    string? Decision,
    string? Context,
    string? Scope,
    string? OutOfScope,
    string? Definitions,
    string? AcceptanceCriteria,
    string? Owners,
    string? ReviewTrigger,
    SpecStatus Status,
    string? RequirementId,
    string? RequirementCode,
    string? RequirementTitle,
    string? BornFromConversationId,
    string? BornFromConversationName,
    string? BornFromConversationType,
    string? BornFromConversationDescription,
    string? SupersedesSpecId,
    string Version,
    UserRefDto CreatedBy,
    UserRefDto UpdatedBy,
    DateTimeOffset? LockedAt,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SpecPageDto(
    IEnumerable<SpecDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record SpecTransitionStatusDto(
    SpecStatus NewStatus);

public record SpecLinkRequirementDto(
    string RequirementId);

public record SpecRejectDto(
    string? Reason = null);

public record SignOffDto(
    string Id,
    string SpecId,
    UserRefDto Stakeholder,
    RoleType Role,
    SignOffDecision Decision,
    string? Conditions,
    string? Comments,
    DateTimeOffset? SignedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SignOffSummaryDto(
    string SpecId,
    int TotalCount,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int ConditionalCount,
    List<SignOffDto> SignOffs);

public record SubmitSignOffDto(
    SignOffDecision Decision,
    string? Conditions = null,
    string? Comments = null);

public record SpecSummaryDto(string Id, string Code, string Title, string Status);
