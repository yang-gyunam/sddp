using System.Text.Json;

namespace Sddp.Abstractions.DTOs;

public sealed record KnowledgePageTreeItemDto(
    string Id,
    string? ParentId,
    string Title,
    string Slug,
    string Status,
    int Position,
    int Revision,
    bool HasChildren,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);

public sealed record KnowledgePageDto(
    string Id,
    string TenantId,
    string ProjectId,
    string? ParentId,
    string Title,
    string Slug,
    JsonElement Content,
    IReadOnlyList<string> Tags,
    string Status,
    int Position,
    int Revision,
    string CreatedBy,
    string UpdatedBy,
    string? PublishedBy,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateKnowledgePageDto(
    string Title,
    string? ParentId = null,
    string? Template = null);

public sealed record UpdateKnowledgePageDto(
    string Title,
    string Slug,
    JsonElement Content,
    IReadOnlyList<string>? Tags,
    string? ChangeSummary);

public sealed record MoveKnowledgePageDto(
    string? ParentId,
    int Position);

public sealed record ArchiveKnowledgePageDto(
    bool IncludeDescendants = false);

public sealed record KnowledgePageVersionSummaryDto(
    string Id,
    int Revision,
    string Title,
    string Status,
    string? ChangeSummary,
    string CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record KnowledgePageVersionDto(
    string Id,
    string PageId,
    int Revision,
    string Title,
    string Slug,
    JsonElement Content,
    IReadOnlyList<string> Tags,
    string Status,
    string? ChangeSummary,
    string CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record KnowledgeSearchResultDto(
    string Id,
    string? ParentId,
    string Title,
    string Slug,
    string Summary,
    string Status,
    IReadOnlyList<string> Tags,
    int Revision,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);

public sealed record KnowledgeBacklinkDto(
    string PageId,
    string PageTitle,
    string PageSlug,
    string Status,
    int Revision,
    DateTimeOffset UpdatedAt);
