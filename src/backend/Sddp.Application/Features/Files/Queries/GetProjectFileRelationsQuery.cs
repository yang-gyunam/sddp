using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Files.Queries;

public sealed record GetProjectFileRelationsQuery(GlobalUniqueId TenantId, GlobalUniqueId ProjectId, GlobalUniqueId UserId, string Path)
    : IQuery<ProjectFileRelationsDto>;

/// <summary>Resolves current, explicit SDDP links and enforces scope before returning any labels.</summary>
public sealed class GetProjectFileRelationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProjectFileRelationsQuery, ProjectFileRelationsDto>
{
    public async Task<ProjectFileRelationsDto> Handle(GetProjectFileRelationsQuery request, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null || !project.IsActive || project.TenantId != request.TenantId)
            throw new NotFoundException("Project", request.ProjectId.ToString());

        var artifacts = await unitOfWork.Repository<ArtifactTracking>().FindAsync(
            a => a.TenantId == request.TenantId && a.ProjectId == request.ProjectId && a.IsActive, cancellationToken).ConfigureAwait(false);
        var matched = artifacts.Where(a => RepositoryPath(project.ArtifactRootPath, a.ArtifactPath) == request.Path).ToList();
        var mappings = await unitOfWork.Repository<ArtifactToSpecMapping>().FindAsync(
            m => m.TenantId == request.TenantId && m.ProjectId == request.ProjectId && m.IsActive && m.ArtifactPath == request.Path,
            cancellationToken).ConfigureAwait(false);

        var ids = new Dictionary<EntityType, HashSet<GlobalUniqueId>>();
        foreach (var type in new[] { EntityType.Artifact, EntityType.Task, EntityType.Spec, EntityType.Requirement, EntityType.Conversation })
            ids[type] = [];
        void Add(EntityType type, GlobalUniqueId? id) { if (id.HasValue && ids.TryGetValue(type, out var set)) set.Add(id.Value); }
        foreach (var artifact in matched)
        {
            Add(EntityType.Artifact, artifact.Id);
            Add(EntityType.Spec, artifact.SpecId);
            Add(EntityType.Requirement, artifact.SourceRequirementId);
            Add(EntityType.Conversation, artifact.SourceConversationId);
        }
        foreach (var mapping in mappings) Add(EntityType.Spec, mapping.SpecId);

        // One hop from the exact file anchors. Do not pull in unrelated sibling artifacts.
        var anchors = ids.ToDictionary(kv => kv.Key, kv => kv.Value.ToHashSet());
        var relationships = await unitOfWork.Repository<Relationship>().FindAsync(
            r => r.TenantId == request.TenantId && r.ProjectId == request.ProjectId && r.IsActive && r.ValidTo == null,
            cancellationToken).ConfigureAwait(false);
        foreach (var relationship in relationships)
        {
            if (anchors.TryGetValue(relationship.FromEntityType, out var from) && from.Contains(relationship.FromEntityId))
                Add(relationship.ToEntityType, relationship.ToEntityId);
            if (anchors.TryGetValue(relationship.ToEntityType, out var to) && to.Contains(relationship.ToEntityId))
                Add(relationship.FromEntityType, relationship.FromEntityId);
        }

        var specIds = ids[EntityType.Spec].ToList();
        var specs = await unitOfWork.Repository<Spec>().FindAsync(
            s => s.TenantId == request.TenantId && s.ProjectId == request.ProjectId && s.IsActive && specIds.Contains(s.Id),
            cancellationToken).ConfigureAwait(false);
        foreach (var spec in specs)
        {
            Add(EntityType.Requirement, spec.RequirementId);
            Add(EntityType.Conversation, spec.BornFromConversationId);
        }
        var requirementIds = ids[EntityType.Requirement].ToList();
        var requirements = await unitOfWork.Repository<Requirement>().FindAsync(
            r => r.TenantId == request.TenantId && r.ProjectId == request.ProjectId && r.IsActive && requirementIds.Contains(r.Id),
            cancellationToken).ConfigureAwait(false);
        foreach (var requirement in requirements) Add(EntityType.Conversation, requirement.ConversationId);

        var conversationIds = ids[EntityType.Conversation].ToList();
        var conversations = await unitOfWork.Repository<Conversation>().FindAsync(
            c => c.TenantId == request.TenantId && c.ProjectId == request.ProjectId && c.IsActive && conversationIds.Contains(c.Id),
            cancellationToken).ConfigureAwait(false);
        var memberships = await unitOfWork.Repository<ConversationMember>().FindAsync(
            m => m.UserId == request.UserId && m.IsActive && conversationIds.Contains(m.ConversationId), cancellationToken).ConfigureAwait(false);
        var memberIds = memberships.Select(m => m.ConversationId).ToHashSet();
        var visibleConversations = conversations.Where(c =>
            (c.Visibility != ConversationVisibility.Private && c.ConversationType != ConversationType.DirectMessage) || memberIds.Contains(c.Id)).ToList();

        var items = new List<ProjectFileLinkDto>();
        items.AddRange(artifacts.Where(a => ids[EntityType.Artifact].Contains(a.Id)).Select(a => new ProjectFileLinkDto(a.Id.ToString(), "Artifact", a.ArtifactPath)));
        items.AddRange(specs.Select(s => new ProjectFileLinkDto(s.Id.ToString(), "Spec", s.Title, s.Code)));
        items.AddRange(requirements.Select(r => new ProjectFileLinkDto(r.Id.ToString(), "Requirement", r.Title, r.Code)));
        items.AddRange(visibleConversations.Select(c => new ProjectFileLinkDto(c.Id.ToString(), "Conversation", c.Name)));

        // Only visible, scoped entities can be used to discover linked tasks.
        var visibleIds = items.Select(i => GlobalUniqueId.Parse(i.Id)).ToList();
        // TaskLinkedItem is a hard-delete join table; IsActive is explicitly unmapped.
        var links = await unitOfWork.Repository<TaskLinkedItem>().FindIncludingInactiveAsync(
            l => visibleIds.Contains(l.LinkedEntityId), cancellationToken).ConfigureAwait(false);
        var visibleKeys = items.Select(i => $"{i.Type.ToLowerInvariant()}/{i.Id}").ToHashSet();
        var taskIds = links.Where(l => visibleKeys.Contains($"{l.LinkedType.ToLowerInvariant()}/{l.LinkedEntityId}"))
            .Select(l => l.TaskId).Concat(ids[EntityType.Task]).Distinct().ToList();
        var tasks = await unitOfWork.Repository<TaskItem>().FindAsync(
            t => t.TenantId == request.TenantId && t.ProjectId == request.ProjectId && t.IsActive && taskIds.Contains(t.Id),
            cancellationToken).ConfigureAwait(false);
        items.AddRange(tasks.Select(t => new ProjectFileLinkDto(t.Id.ToString(), "Task", t.Title)));

        var ownerIds = matched.Where(a => a.OwnerUserId.HasValue).Select(a => a.OwnerUserId!.Value).Distinct().ToList();
        var owners = await unitOfWork.Repository<User>().FindAsync(u => u.IsActive && ownerIds.Contains(u.Id), cancellationToken).ConfigureAwait(false);
        return new(owners.Select(u => new UserRefDto(u.Id.ToString(), u.DisplayName, u.AvatarUrl)).ToList(),
            items.DistinctBy(i => (i.Type, i.Id)).OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string RepositoryPath(string? root, string path)
        => string.Join('/', new[] { root?.Replace('\\', '/').Trim('/'), path.Replace('\\', '/').TrimStart('/') }
            .Where(p => !string.IsNullOrEmpty(p) && p != "."));
}
