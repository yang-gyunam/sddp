using Microsoft.EntityFrameworkCore;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Services.Diagrams;

public sealed class DiagramOriginScopeReader(SddpDbContext db) : IDiagramOriginScopeReader, IRelationshipEndpointReader
{
    public Task<HashSet<GlobalUniqueId>> ReadScopedOriginIdsAsync(EntityType type, GlobalUniqueId[] ids,
        GlobalUniqueId tenant, GlobalUniqueId project, GlobalUniqueId? user, CancellationToken ct)
    {
        var query = Scoped(type, tenant, project, user);
        if (query is null || type == EntityType.Diagram) return Task.FromResult<HashSet<GlobalUniqueId>>([]);
        return query.Where(e => ids.Contains(e.Id)).Select(e => e.Id).TagWith("Diagram origin scope").ToHashSetAsync(ct);
    }

    public Task<List<ScopedRelationshipEndpoint>> ReadScopedAsync(EntityType type, GlobalUniqueId[] ids,
        GlobalUniqueId tenant, GlobalUniqueId project, GlobalUniqueId? user, CancellationToken ct)
    {
        var query = Scoped(type, tenant, project, user);
        if (query is null) return Task.FromResult<List<ScopedRelationshipEndpoint>>([]);
        return query.Where(e => ids.Contains(e.Id)).TagWith("Relationship endpoints").ToListAsync(ct);
    }

    private IQueryable<ScopedRelationshipEndpoint>? Scoped(EntityType type, GlobalUniqueId tenant,
        GlobalUniqueId project, GlobalUniqueId? user)
    {
        var conversations = db.Conversations.Where(c => c.IsActive && c.TenantId == tenant && c.ProjectId == project &&
            (c.Visibility != ConversationVisibility.Private || (user.HasValue &&
                db.ConversationMembers.Any(m => m.ConversationId == c.Id && m.IsActive && m.UserId == user))));
        return type switch
        {
            EntityType.Conversation => conversations.Select(c => new ScopedRelationshipEndpoint { Id = c.Id,
                Label = (c.ConversationType == ConversationType.Channel ? "#" : "") + c.Name +
                    (string.IsNullOrWhiteSpace(c.Description) ? "" : " — " + c.Description) }),
            EntityType.Topic => from topic in db.Topics
                                join forum in conversations.OfType<Forum>() on topic.ForumId equals forum.Id
                                where topic.IsActive
                                select new ScopedRelationshipEndpoint { Id = topic.Id, Label = topic.Title, Status = topic.Status.ToString() },
            EntityType.Spec => db.Specs.Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.Title, Code = e.Code, Status = e.Status.ToString() }),
            EntityType.Requirement => db.Requirements.Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.Title, Code = e.Code, Status = e.Status.ToString() }),
            EntityType.Artifact => db.ArtifactTrackings.Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.EntityName ?? e.ArtifactPath }),
            EntityType.GlossaryTerm => db.GlossaryTerms.Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.Term }),
            EntityType.Task => db.TaskItems.Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.Title }),
            EntityType.Diagram => db.Set<Diagram>().Where(e => e.IsActive && e.TenantId == tenant && e.ProjectId == project)
                .Select(e => new ScopedRelationshipEndpoint { Id = e.Id, Label = e.Title, ModelJson = e.ModelJson, DiagramKind = e.Kind }),
            _ => null
        };
    }
}
