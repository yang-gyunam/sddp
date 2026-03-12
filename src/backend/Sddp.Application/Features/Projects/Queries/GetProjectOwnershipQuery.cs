using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects.Queries;

/// <summary>
/// project get
/// Treemap: entity aggregated
/// </summary>
public sealed record GetProjectOwnershipQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<ProjectOwnershipDto>;

public sealed class GetProjectOwnershipQueryHandler
    : IRequestHandler<GetProjectOwnershipQuery, ProjectOwnershipDto>
{
    private readonly IRepository<Spec> _specRepo;
    private readonly IRepository<Requirement> _requirementRepo;
    private readonly IRepository<GlossaryTerm> _glossaryRepo;
    private readonly IRepository<TaskItem> _taskRepo;
    private readonly IRepository<ArtifactTracking> _artifactRepo;
    private readonly IRepository<User> _userRepo;

    public GetProjectOwnershipQueryHandler(
        IRepository<Spec> specRepo,
        IRepository<Requirement> requirementRepo,
        IRepository<GlossaryTerm> glossaryRepo,
        IRepository<TaskItem> taskRepo,
        IRepository<ArtifactTracking> artifactRepo,
        IRepository<User> userRepo)
    {
        _specRepo = specRepo;
        _requirementRepo = requirementRepo;
        _glossaryRepo = glossaryRepo;
        _taskRepo = taskRepo;
        _artifactRepo = artifactRepo;
        _userRepo = userRepo;
    }

    public async Task<ProjectOwnershipDto> Handle(
        GetProjectOwnershipQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        var projectId = request.ProjectId;

        var specs = await (LoadSpecs(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var requirements = await (LoadRequirements(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var glossaryTerms = await (LoadGlossaryTerms(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var tasks = await (LoadTasks(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var artifacts = await (LoadArtifacts(tenantId, projectId, cancellationToken)).ConfigureAwait(false);

        // Collect all owner user IDs for batch user lookup
        var ownerIds = new HashSet<GlobalUniqueId>();

        foreach (var s in specs)
        {
            var ownerId = s.GetPrimaryOwnerId();
            if (ownerId.HasValue) ownerIds.Add(ownerId.Value);
        }
        foreach (var r in requirements)
        {
            if (r.OwnerUserId.HasValue) ownerIds.Add(r.OwnerUserId.Value);
        }
        foreach (var g in glossaryTerms)
        {
            if (g.OwnerUserId.HasValue) ownerIds.Add(g.OwnerUserId.Value);
        }
        foreach (var t in tasks)
        {
            var effectiveOwner = t.AssigneeId ?? t.CreatorId;
            ownerIds.Add(effectiveOwner);
        }
        foreach (var a in artifacts)
        {
            if (a.OwnerUserId.HasValue) ownerIds.Add(a.OwnerUserId.Value);
        }

        // Batch load users
        var userMap = await (LoadUserDisplayNames(ownerIds, cancellationToken)).ConfigureAwait(false);

        // Build ownership items
        var items = new List<OwnershipItemDto>();

        foreach (var s in specs)
        {
            var ownerId = s.GetPrimaryOwnerId();
            items.Add(new OwnershipItemDto(
                EntityType: "Spec",
                EntityId: s.Id.ToString(),
                EntityName: !string.IsNullOrEmpty(s.Code) ? s.Code : s.Title,
                OwnerUserId: ownerId?.ToString(),
                OwnerName: ownerId.HasValue ? userMap.GetValueOrDefault(ownerId.Value) : null));
        }

        foreach (var r in requirements)
        {
            items.Add(new OwnershipItemDto(
                EntityType: "Requirement",
                EntityId: r.Id.ToString(),
                EntityName: !string.IsNullOrEmpty(r.Code) ? r.Code : r.Title,
                OwnerUserId: r.OwnerUserId?.ToString(),
                OwnerName: r.OwnerUserId.HasValue ? userMap.GetValueOrDefault(r.OwnerUserId.Value) : null));
        }

        foreach (var g in glossaryTerms)
        {
            items.Add(new OwnershipItemDto(
                EntityType: "GlossaryTerm",
                EntityId: g.Id.ToString(),
                EntityName: g.Term,
                OwnerUserId: g.OwnerUserId?.ToString(),
                OwnerName: g.OwnerUserId.HasValue ? userMap.GetValueOrDefault(g.OwnerUserId.Value) : null));
        }

        foreach (var t in tasks)
        {
            var effectiveOwner = t.AssigneeId ?? t.CreatorId;
            items.Add(new OwnershipItemDto(
                EntityType: "Task",
                EntityId: t.Id.ToString(),
                EntityName: t.Title,
                OwnerUserId: effectiveOwner.ToString(),
                OwnerName: userMap.GetValueOrDefault(effectiveOwner)));
        }

        foreach (var a in artifacts)
        {
            items.Add(new OwnershipItemDto(
                EntityType: "Artifact",
                EntityId: a.Id.ToString(),
                EntityName: a.ArtifactPath,
                OwnerUserId: a.OwnerUserId?.ToString(),
                OwnerName: a.OwnerUserId.HasValue ? userMap.GetValueOrDefault(a.OwnerUserId.Value) : null));
        }

        return new ProjectOwnershipDto(items);
    }

    // ============================================
    // Data Loading
    // ============================================

    private async Task<IReadOnlyList<Spec>> LoadSpecs(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var items = await (_specRepo.FindAsync(
            predicate: s => s.TenantId == tenantId
                && s.ProjectId == projectId
                && s.IsActive
                && s.ValidTo == null,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.ToList();
    }

    private async Task<IReadOnlyList<Requirement>> LoadRequirements(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var items = await (_requirementRepo.FindAsync(
            predicate: r => r.TenantId == tenantId
                && r.ProjectId == projectId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.ToList();
    }

    private async Task<IReadOnlyList<GlossaryTerm>> LoadGlossaryTerms(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var items = await (_glossaryRepo.FindAsync(
            predicate: g => g.TenantId == tenantId
                && g.ProjectId == projectId
                && g.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.ToList();
    }

    private async Task<IReadOnlyList<TaskItem>> LoadTasks(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var items = await (_taskRepo.FindAsync(
            predicate: t => t.TenantId == tenantId
                && t.ProjectId == projectId
                && t.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.ToList();
    }

    private async Task<IReadOnlyList<ArtifactTracking>> LoadArtifacts(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var items = await (_artifactRepo.FindAsync(
            predicate: a => a.TenantId == tenantId
                && a.ProjectId == projectId
                && a.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.ToList();
    }

    private async Task<Dictionary<GlobalUniqueId, string>> LoadUserDisplayNames(
        IEnumerable<GlobalUniqueId> userIds, CancellationToken ct)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0) return new Dictionary<GlobalUniqueId, string>();

        var users = await (_userRepo.FindAsync(
            predicate: u => idList.Contains(u.Id) && u.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return users.ToDictionary(u => u.Id, u => u.DisplayName);
    }

}
