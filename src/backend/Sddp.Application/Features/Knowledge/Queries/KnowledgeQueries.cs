using System.Data;
using Dapper;
using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Knowledge.Queries;

public sealed record GetKnowledgeTreeQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<IReadOnlyList<KnowledgePageTreeItemDto>>;

public sealed class GetKnowledgeTreeQueryHandler
    : IRequestHandler<GetKnowledgeTreeQuery, IReadOnlyList<KnowledgePageTreeItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKnowledgeTreeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<KnowledgePageTreeItemDto>> Handle(
        GetKnowledgeTreeQuery request,
        CancellationToken cancellationToken)
    {
        var pages = await KnowledgeQueryHelpers.GetVisiblePagesAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.UserId,
            request.CanViewAllDrafts,
            request.IncludeArchived,
            cancellationToken);
        var parentIds = pages
            .Where(page => page.ParentId.HasValue)
            .Select(page => page.ParentId!.Value)
            .ToHashSet();

        return pages
            .OrderBy(page => page.ParentId.HasValue)
            .ThenBy(page => page.Position)
            .ThenBy(page => page.Title, StringComparer.OrdinalIgnoreCase)
            .Select(page => KnowledgeMapping.ToTreeItem(
                page,
                parentIds.Contains(page.Id)))
            .ToArray();
    }
}

public sealed record GetKnowledgePageQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<KnowledgePageDto?>;

public sealed class GetKnowledgePageQueryHandler
    : IRequestHandler<GetKnowledgePageQuery, KnowledgePageDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKnowledgePageQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageDto?> Handle(
        GetKnowledgePageQuery request,
        CancellationToken cancellationToken)
    {
        var pages = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.Id == request.PageId
                && page.TenantId == request.TenantId
                && page.ProjectId == request.ProjectId
                && page.IsActive,
            cancellationToken);
        var page = pages.FirstOrDefault();
        return page is not null
            && KnowledgeQueryHelpers.IsVisible(
                page,
                request.UserId,
                request.CanViewAllDrafts,
                request.IncludeArchived)
            ? KnowledgeMapping.ToDto(page)
            : null;
    }
}

public sealed record GetKnowledgePageVersionsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    GlobalUniqueId UserId,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<IReadOnlyList<KnowledgePageVersionSummaryDto>>;

public sealed class GetKnowledgePageVersionsQueryHandler
    : IRequestHandler<GetKnowledgePageVersionsQuery, IReadOnlyList<KnowledgePageVersionSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKnowledgePageVersionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<KnowledgePageVersionSummaryDto>> Handle(
        GetKnowledgePageVersionsQuery request,
        CancellationToken cancellationToken)
    {
        await KnowledgeQueryHelpers.EnsureVisiblePageAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.PageId,
            request.UserId,
            request.CanViewAllDrafts,
            request.IncludeArchived,
            cancellationToken);
        var versions = await _unitOfWork.Repository<KnowledgePageVersion>().FindAsync(
            version => version.TenantId == request.TenantId
                && version.ProjectId == request.ProjectId
                && version.PageId == request.PageId
                && version.IsActive,
            cancellationToken);

        return versions
            .OrderByDescending(version => version.Revision)
            .Select(KnowledgeMapping.ToVersionSummary)
            .ToArray();
    }
}

public sealed record GetKnowledgePageVersionQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PageId,
    int Revision,
    GlobalUniqueId UserId,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<KnowledgePageVersionDto?>;

public sealed class GetKnowledgePageVersionQueryHandler
    : IRequestHandler<GetKnowledgePageVersionQuery, KnowledgePageVersionDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKnowledgePageVersionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KnowledgePageVersionDto?> Handle(
        GetKnowledgePageVersionQuery request,
        CancellationToken cancellationToken)
    {
        await KnowledgeQueryHelpers.EnsureVisiblePageAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.PageId,
            request.UserId,
            request.CanViewAllDrafts,
            request.IncludeArchived,
            cancellationToken);
        var versions = await _unitOfWork.Repository<KnowledgePageVersion>().FindAsync(
            version => version.TenantId == request.TenantId
                && version.ProjectId == request.ProjectId
                && version.PageId == request.PageId
                && version.Revision == request.Revision
                && version.IsActive,
            cancellationToken);
        var version = versions.FirstOrDefault();
        return version is null ? null : KnowledgeMapping.ToVersionDto(version);
    }
}

public sealed record SearchKnowledgePagesQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    string Query,
    int Limit,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<IReadOnlyList<KnowledgeSearchResultDto>>;

public sealed class SearchKnowledgePagesQueryHandler
    : IRequestHandler<SearchKnowledgePagesQuery, IReadOnlyList<KnowledgeSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDbConnection? _connection;

    public SearchKnowledgePagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public SearchKnowledgePagesQueryHandler(
        IUnitOfWork unitOfWork,
        IDbConnection connection)
    {
        _unitOfWork = unitOfWork;
        _connection = connection;
    }

    public async Task<IReadOnlyList<KnowledgeSearchResultDto>> Handle(
        SearchKnowledgePagesQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();
        if (query.Length < 2)
        {
            return [];
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        if (_connection is not null
            && !string.IsNullOrWhiteSpace(_connection.ConnectionString))
        {
            return await SearchPostgresAsync(
                request,
                query,
                limit,
                cancellationToken);
        }

        var normalized = query.ToLowerInvariant();
        var candidates = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.TenantId == request.TenantId
                && page.ProjectId == request.ProjectId
                && page.IsActive
                && (page.Title.ToLower().Contains(normalized)
                    || page.ContentText.ToLower().Contains(normalized)
                    || page.TagsJson.ToLower().Contains(normalized)),
            cancellationToken);

        return candidates
            .Where(page => KnowledgeQueryHelpers.IsVisible(
                page,
                request.UserId,
                request.CanViewAllDrafts,
                request.IncludeArchived))
            .OrderByDescending(page => page.Title.Contains(
                query,
                StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(page => page.UpdatedAt)
            .Take(limit)
            .Select(KnowledgeMapping.ToSearchResult)
            .ToArray();
    }

    private async Task<IReadOnlyList<KnowledgeSearchResultDto>> SearchPostgresAsync(
        SearchKnowledgePagesQuery request,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH search_query AS (
                SELECT websearch_to_tsquery('simple', @Query) AS value
            )
            SELECT
                page.id AS "Id",
                page.parent_id AS "ParentId",
                page.title AS "Title",
                page.slug AS "Slug",
                CASE
                    WHEN length(page.content_text) <= 240 THEN page.content_text
                    ELSE LEFT(page.content_text, 237) || '...'
                END AS "Summary",
                page.status AS "Status",
                page.tags_json::text AS "TagsJson",
                page.revision AS "Revision",
                page.updated_by AS "UpdatedBy",
                page.updated_at AS "UpdatedAt"
            FROM knowledge_pages page
            CROSS JOIN search_query
            WHERE page.tenant_id = @TenantId
              AND page.project_id = @ProjectId
              AND page.is_active = TRUE
              AND (
                    page.status = 'Published'
                    OR (page.status = 'Draft' AND (@CanViewAllDrafts OR page.created_by = @UserId))
                    OR (page.status = 'Archived' AND @IncludeArchived)
                  )
              AND (
                    to_tsvector(
                        'simple',
                        COALESCE(page.title, '') || ' '
                        || COALESCE(page.content_text, '') || ' '
                        || COALESCE(page.tags_json::text, '')
                    ) @@ search_query.value
                    OR page.title ILIKE @Pattern ESCAPE '\'
                    OR page.tags_json::text ILIKE @Pattern ESCAPE '\'
                  )
            ORDER BY
                (page.title ILIKE @Pattern ESCAPE '\') DESC,
                ts_rank(
                    to_tsvector(
                        'simple',
                        COALESCE(page.title, '') || ' '
                        || COALESCE(page.content_text, '') || ' '
                        || COALESCE(page.tags_json::text, '')
                    ),
                    search_query.value
                ) DESC,
                page.updated_at DESC
            LIMIT @Limit;
            """;
        var rows = await _connection!.QueryAsync<KnowledgeSearchRow>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = request.TenantId.ToGuid(),
                    ProjectId = request.ProjectId.ToGuid(),
                    UserId = request.UserId.ToGuid(),
                    Query = query,
                    Pattern = $"%{EscapeLikePattern(query)}%",
                    request.CanViewAllDrafts,
                    request.IncludeArchived,
                    Limit = limit,
                },
                cancellationToken: cancellationToken));

        return rows
            .Select(row => new KnowledgeSearchResultDto(
                row.Id.ToString(),
                row.ParentId?.ToString(),
                row.Title,
                row.Slug,
                row.Summary,
                row.Status,
                KnowledgeContentParser.DeserializeTags(row.TagsJson),
                row.Revision,
                row.UpdatedBy.ToString(),
                new DateTimeOffset(DateTime.SpecifyKind(row.UpdatedAt, DateTimeKind.Utc))))
            .ToArray();
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record KnowledgeSearchRow(
        Guid Id,
        Guid? ParentId,
        string Title,
        string Slug,
        string Summary,
        string Status,
        string TagsJson,
        int Revision,
        Guid UpdatedBy,
        // Npgsql materializes `timestamp with time zone` as DateTime (Kind=Utc), not
        // DateTimeOffset. Declaring DateTimeOffset here makes Dapper fail to find a matching
        // constructor and the whole search endpoint returns an error.
        DateTime UpdatedAt);
}

public sealed record GetKnowledgeBacklinksQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId,
    KnowledgeTargetType TargetType,
    GlobalUniqueId TargetId,
    bool CanViewAllDrafts,
    bool IncludeArchived)
    : IQuery<IReadOnlyList<KnowledgeBacklinkDto>>;

public sealed class GetKnowledgeBacklinksQueryHandler
    : IRequestHandler<GetKnowledgeBacklinksQuery, IReadOnlyList<KnowledgeBacklinkDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetKnowledgeBacklinksQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<KnowledgeBacklinkDto>> Handle(
        GetKnowledgeBacklinksQuery request,
        CancellationToken cancellationToken)
    {
        // The target must be visible to this reader, not merely present in the project. Otherwise a
        // non-member could enumerate the Pages that mention a private conversation's Decision.
        if (!await KnowledgeTargetVisibility.IsTargetVisibleAsync(
                _unitOfWork,
                request.TenantId,
                request.ProjectId,
                request.UserId,
                request.TargetType,
                request.TargetId,
                cancellationToken))
        {
            return [];
        }

        var links = await _unitOfWork.Repository<KnowledgePageLink>().FindAsync(
            link => link.TenantId == request.TenantId
                && link.ProjectId == request.ProjectId
                && link.TargetType == request.TargetType
                && link.TargetId == request.TargetId
                && link.IsActive,
            cancellationToken);
        if (links.Count == 0)
        {
            return [];
        }

        var pageIds = links.Select(link => link.PageId).ToHashSet();
        var pages = await _unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.TenantId == request.TenantId
                && page.ProjectId == request.ProjectId
                && pageIds.Contains(page.Id)
                && page.IsActive,
            cancellationToken);

        return pages
            .Where(page => KnowledgeQueryHelpers.IsVisible(
                page,
                request.UserId,
                request.CanViewAllDrafts,
                request.IncludeArchived))
            .OrderByDescending(page => page.UpdatedAt)
            .Select(page => new KnowledgeBacklinkDto(
                page.Id.ToString(),
                page.Title,
                page.Slug,
                page.Status.ToString(),
                page.Revision,
                page.UpdatedAt.ToDateTimeOffset()))
            .ToArray();
    }
}

internal static class KnowledgeQueryHelpers
{
    public static async Task<IReadOnlyList<KnowledgePage>> GetVisiblePagesAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        bool canViewAllDrafts,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var pages = await unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.TenantId == tenantId
                && page.ProjectId == projectId
                && page.IsActive,
            cancellationToken);
        return pages
            .Where(page => IsVisible(page, userId, canViewAllDrafts, includeArchived))
            .ToArray();
    }

    public static bool IsVisible(
        KnowledgePage page,
        GlobalUniqueId userId,
        bool canViewAllDrafts,
        bool includeArchived) =>
        page.Status switch
        {
            KnowledgePageStatus.Published => true,
            KnowledgePageStatus.Draft => canViewAllDrafts || page.CreatedBy == userId,
            KnowledgePageStatus.Archived => includeArchived,
            _ => false
        };

    public static async Task<KnowledgePage> EnsureVisiblePageAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId pageId,
        GlobalUniqueId userId,
        bool canViewAllDrafts,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var pages = await unitOfWork.Repository<KnowledgePage>().FindAsync(
            page => page.Id == pageId
                && page.TenantId == tenantId
                && page.ProjectId == projectId
                && page.IsActive,
            cancellationToken);
        var page = pages.FirstOrDefault();
        if (page is null || !IsVisible(page, userId, canViewAllDrafts, includeArchived))
        {
            throw new NotFoundException("KnowledgePage", pageId.ToString());
        }

        return page;
    }
}
