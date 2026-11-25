using System.Text.Json;
using Dapper;
using MediatR;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships.Queries;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.ProjectSearch;

public sealed record ProjectSearchQuery(GlobalUniqueId Tenant, GlobalUniqueId Project, GlobalUniqueId User,
    IReadOnlyList<string> Permissions, ProjectSearchExpression Expression, string Menu, string Sort, int Offset,
    string? GitCommit, bool FilesUnavailable, string Fingerprint, ProjectSearchCursor? Cursor, bool IncludeAiConversationMessages) : IQuery<ProjectSearchPage>, IRelationshipReadQuery;

public sealed class ProjectSearchQueryHandler(IProjectSearchSession session, IDiagramListReader diagrams,
    IDiagramOriginAccess origins, IProjectSearchChatSource? chats = null) : IRequestHandler<ProjectSearchQuery, ProjectSearchPage>
{
    public static readonly IReadOnlySet<string> Menus = new HashSet<string>(StringComparer.Ordinal)
    { "dashboard","specs","requirements","conversations","knowledge","glossary","files","diagrams","artifacts","tasks","effort","traceability","ai-chat" };
    private static readonly Guid[] AiMessageTypes = WellKnownCodes.MessageTypeToId
        .Where(pair => pair.Key.IsAiGenerated()).Select(pair => pair.Value).ToArray();

    private static readonly string Sources = LoadSources();
    private static string LoadSources()
    {
        using var stream = typeof(ProjectSearchQueryHandler).Assembly.GetManifestResourceStream(
            "Sddp.Application.Features.ProjectSearch.ProjectSearch.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public async Task<ProjectSearchPage> Handle(ProjectSearchQuery request, CancellationToken ct)
    {
        bool Can(string permission) => request.Permissions.Contains(permission);
        Guid[] diagramIds = [];
        if (Can(PermissionCodes.DiagramRead))
        {
            var candidates = await diagrams.ReadCandidatesAsync(request.Tenant, request.Project, null, ct);
            var readable = await origins.ReadableOrigins(candidates.SelectMany(d => d.Origins).Distinct(), ct);
            diagramIds = candidates.Where(d => d.Origins.All(readable.Contains)).Select(d => Guid.Parse(d.Item.Id)).ToArray();
        }
        var parameters = request.Expression.Parameters;
        var terms = request.Expression.Terms;
        var chatDocuments = chats is null ? null : await chats.ReadAsync(session, request.Tenant, request.Project, request.User, terms, ct);
        parameters.AddDynamicParams(new
        {
            Tenant = request.Tenant.ToGuid(),
            Project = request.Project.ToGuid(),
            User = request.User.ToGuid(),
            request.GitCommit,
            request.Menu,
            Offset = request.Cursor is null ? request.Offset : 0,
            Terms = terms,
            AfterScore = request.Cursor?.Score,
            AfterTime = request.Cursor?.Time.ToUniversalTime(),
            AfterMenu = request.Cursor?.Menu,
            AfterKey = request.Cursor?.Key,
            Phrase = string.Join(" ", terms),
            Specs = Can(PermissionCodes.SpecRead),
            Requirements = Can(PermissionCodes.RequirementRead),
            request.IncludeAiConversationMessages,
            AiMessageTypes,
            Conversations = Can(PermissionCodes.ConversationRead),
            Tasks = Can(PermissionCodes.TaskRead),
            Glossary = Can(PermissionCodes.GlossaryRead),
            Knowledge = Can(PermissionCodes.KnowledgeRead),
            AllDrafts = Can(PermissionCodes.KnowledgePublish) || Can(PermissionCodes.KnowledgeAdmin),
            Archived = Can(PermissionCodes.KnowledgeAdmin),
            Audit = Can(PermissionCodes.AuditRead),
            Diagrams = diagramIds,
            Chats = JsonSerializer.Serialize(chatDocuments ?? [], JsonSerializerOptions.Web)
        });
        var order = request.Sort == "date" ? "updated_at DESC,menu,key" : "score DESC,updated_at DESC,menu,key";
        var after = request.Cursor is null ? "TRUE" :
            "(updated_at < @AfterTime OR (updated_at = @AfterTime AND (menu,key) > (@AfterMenu,@AfterKey)))";
        if (request.Cursor is not null && request.Sort == "relevance")
            after = $"(score < @AfterScore OR (score = @AfterScore AND {after}))";
        var sql = Sources + $$"""
            , hits AS MATERIALIZED (
                SELECT menu,key,target,kind,title,code,left(substring(body from greatest(1,coalesce(pos,1)-60) for 240),240) snippet,
                       location,status,version,updated_at,revision,
                       CASE WHEN menu='files' AND pos IS NOT NULL THEN 1 + length(left(body,pos-1)) - length(replace(left(body,pos-1),chr(10),'')) END line,
                       CASE WHEN lower(code)=lower(@Phrase) OR lower(title)=lower(@Phrase) THEN 3
                            WHEN strpos(lower(title),lower(@Phrase))>0 THEN 2
                            WHEN EXISTS (SELECT 1 FROM unnest(@Terms::text[]) term WHERE strpos(lower(title),lower(term))>0) THEN 1 ELSE 0 END score
                FROM documents LEFT JOIN LATERAL (SELECT min(nullif(strpos(lower(body),lower(term)),0)) pos FROM unnest(@Terms::text[]) term) x ON TRUE
                WHERE {{request.Expression.Predicate}}
            ), counts AS (SELECT menu,count(*) n FROM hits GROUP BY menu),
            page AS (SELECT * FROM hits WHERE (@Menu='all' OR menu=@Menu) AND {{after}} ORDER BY {{order}} LIMIT 11 OFFSET @Offset)
            SELECT coalesce((SELECT jsonb_object_agg(menu,n) FROM counts),'{}'::jsonb)::text AS Counts,
                coalesce((SELECT jsonb_agg((to_jsonb(p) - 'updated_at') || jsonb_build_object('updatedAt',p.updated_at) ORDER BY {{order}}) FROM (
                    SELECT menu,key,target,kind,title,code,snippet,location,status,version,updated_at,revision,line,score,
                        CASE WHEN kind IN ('message','topic-message') THEN (SELECT m.created_at FROM messages m WHERE m.id=page.key::uuid) END AS "CreatedAt" FROM page
                ) p),'[]'::jsonb)::text AS Items
            """;
        var result = await session.Connection.QuerySingleAsync<(string Counts, string Items)>(
            new CommandDefinition(sql, parameters, session.Transaction, cancellationToken: ct));
        var counts = JsonSerializer.Deserialize<Dictionary<string, int>>(result.Counts)!;
        using var items = JsonDocument.Parse(result.Items);
        var rows = items.RootElement.EnumerateArray().Take(10)
            .Select(item => item.Deserialize<ProjectSearchHit>(JsonSerializerOptions.Web)!).ToArray();
        var hasMore = items.RootElement.GetArrayLength() > rows.Length;
        var last = rows.LastOrDefault();
        var cursor = hasMore && last is not null ? new ProjectSearchCursor(request.Fingerprint,
            items.RootElement[rows.Length - 1].GetProperty("score").GetInt32(), last.UpdatedAt, last.Menu, last.Key).Encode() : null;
        return new(rows, counts, hasMore ? checked(request.Offset + rows.Length) : null, cursor, terms, request.FilesUnavailable,
            AiChatsAvailable: chatDocuments is not null);
    }
}
