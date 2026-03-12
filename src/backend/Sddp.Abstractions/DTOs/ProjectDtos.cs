using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record ProjectDto(
    string Id,
    string TenantId,
    string Code,
    string Name,
    string? Description,
    UserRefDto Owner,
    string Status,
    string CreatedAt,
    string UpdatedAt);

public record ProjectDetailDto(
    string Id,
    string TenantId,
    string Code,
    string Name,
    string? Description,
    UserRefDto Owner,
    string Status,
    string CreatedAt,
    string UpdatedAt,
    ProjectStatisticsDto Statistics,
    IReadOnlyList<ProjectMemberDto> Members);

public record ProjectStatisticsDto(
    StatPairDto Conversations,
    StatPairDto Requirements,
    StatPairDto Specs,
    StatPairDto Artifacts,
    StatPairDto Tasks,
    StatPairDto Glossary,
    StatPairDto Effort);

public record StatPairDto(int Total, int Secondary);

public record ProjectMemberDto(
    string UserId,
    string PersonId,
    string DisplayName,
    string Role,
    string? AvatarUrl,
    string? LastActivityAt,
    bool IsOnline);

public record CreateProjectDto(
    string Code,
    string Name,
    string? Description);

public record UpdateProjectDto(
    string Name,
    string? Description);

public record OwnershipItemDto(
    string EntityType,
    string EntityId,
    string EntityName,
    string? OwnerUserId,
    string? OwnerName);

public record ProjectOwnershipDto(
    IReadOnlyList<OwnershipItemDto> Items);

public record AddProjectMemberDto(
    string UserId,
    string Role);
