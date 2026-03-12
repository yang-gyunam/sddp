using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects.Queries;

/// <summary>
/// project get (FK + Relationship)
/// Layer 0: Conversation → Layer 1: Requirement → Layer 2: Spec → Layer 3: Task, Artifact
/// </summary>
public sealed record GetTraceabilityMapQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<TraceabilityMapDto>;

public sealed class GetTraceabilityMapQueryHandler
    : IRequestHandler<GetTraceabilityMapQuery, TraceabilityMapDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTraceabilityMapQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TraceabilityMapDto> Handle(
        GetTraceabilityMapQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        var projectId = request.ProjectId;

        // 1. Load all active entities for the project
        var conversations = await (LoadConversations(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var requirements = await (LoadRequirements(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var specs = await (LoadSpecs(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var tasks = await (LoadTasks(tenantId, projectId, cancellationToken)).ConfigureAwait(false);
        var artifacts = await (LoadArtifacts(tenantId, projectId, cancellationToken)).ConfigureAwait(false);

        // 2. Build TraceabilityNodeDto list with FK-based ParentId
        var nodes = new List<TraceabilityNodeDto>();

        // Layer 0: Conversations (root nodes, no parent)
        foreach (var c in conversations)
        {
            nodes.Add(new TraceabilityNodeDto(
                Id: c.Id.ToString(),
                EntityType: "Conversation",
                Label: FormatConversationLabel(c),
                Code: null,
                Status: c.ConversationType.ToString(),
                Layer: 0,
                ParentId: null));
        }

        // Layer 1: Requirements (parent = ConversationId if exists)
        foreach (var r in requirements)
        {
            nodes.Add(new TraceabilityNodeDto(
                Id: r.Id.ToString(),
                EntityType: "Requirement",
                Label: r.Title,
                Code: r.Code,
                Status: r.Status.ToString(),
                Layer: 1,
                ParentId: r.ConversationId?.ToString()));
        }

        // Layer 2: Specs (parent = RequirementId ?? BornFromConversationId)
        foreach (var s in specs)
        {
            var parentId = s.RequirementId?.ToString()
                ?? s.BornFromConversationId?.ToString();

            nodes.Add(new TraceabilityNodeDto(
                Id: s.Id.ToString(),
                EntityType: "Spec",
                Label: s.Title,
                Code: s.Code,
                Status: s.Status.ToString(),
                Layer: 2,
                ParentId: parentId));
        }

        // Layer 3: Tasks (parent = first Spec or Requirement from LinkedItems)
        foreach (var t in tasks)
        {
            var parentId = ResolveTaskParentId(t);

            nodes.Add(new TraceabilityNodeDto(
                Id: t.Id.ToString(),
                EntityType: "Task",
                Label: t.Title,
                Code: null,
                Status: t.Status.ToString(),
                Layer: 3,
                ParentId: parentId));
        }

        // Layer 3: Artifacts (parent = SpecId)
        foreach (var a in artifacts)
        {
            nodes.Add(new TraceabilityNodeDto(
                Id: a.Id.ToString(),
                EntityType: "Artifact",
                Label: a.ArtifactPath,
                Code: null,
                Status: null,
                Layer: 3,
                ParentId: a.SpecId.ToString()));
        }

        // 3. Load active relationships and convert to TraceabilityCrossLinkDto
        var crossLinks = await (LoadCrossLinks(tenantId, projectId, cancellationToken)).ConfigureAwait(false);

        // 4. Build stats
        var stats = new TraceabilityMapStatsDto(
            ConversationCount: conversations.Count,
            RequirementCount: requirements.Count,
            SpecCount: specs.Count,
            TaskCount: tasks.Count,
            ArtifactCount: artifacts.Count,
            CrossLinkCount: crossLinks.Count);

        return new TraceabilityMapDto(nodes, crossLinks, stats);
    }

    // ============================================
    // Data Loading
    // ============================================

    private async Task<IReadOnlyList<Conversation>> LoadConversations(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Conversation>();
        var items = await (repo.FindAsync(
            predicate: c => c.TenantId == tenantId
                && c.ProjectId == projectId
                && c.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
    }

    private async Task<IReadOnlyList<Requirement>> LoadRequirements(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Requirement>();
        var items = await (repo.FindAsync(
            predicate: r => r.TenantId == tenantId
                && r.ProjectId == projectId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.OrderBy(r => r.Code).ToList();
    }

    private async Task<IReadOnlyList<Spec>> LoadSpecs(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Spec>();
        var items = await (repo.FindAsync(
            predicate: s => s.TenantId == tenantId
                && s.ProjectId == projectId
                && s.IsActive
                && s.ValidTo == null,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.OrderBy(s => s.Code).ToList();
    }

    private async Task<IReadOnlyList<TaskItem>> LoadTasks(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<TaskItem>();
        var items = await (repo.FindAsync(
            predicate: t => t.TenantId == tenantId
                && t.ProjectId == projectId
                && t.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.OrderBy(t => t.Title).ToList();
    }

    private async Task<IReadOnlyList<ArtifactTracking>> LoadArtifacts(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<ArtifactTracking>();
        var items = await (repo.FindAsync(
            predicate: a => a.TenantId == tenantId
                && a.ProjectId == projectId
                && a.IsActive,
            cancellationToken: ct)).ConfigureAwait(false);

        return items.OrderBy(a => a.ArtifactPath).ToList();
    }

    private async Task<IReadOnlyList<TraceabilityCrossLinkDto>> LoadCrossLinks(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Relationship>();
        var relationships = await (repo.FindAsync(
            predicate: r => r.TenantId == tenantId
                && r.ProjectId == projectId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken: ct)).ConfigureAwait(false);

        return relationships
            .Select(r => new TraceabilityCrossLinkDto(
                Id: r.Id.ToString(),
                SourceId: r.FromEntityId.ToString(),
                TargetId: r.ToEntityId.ToString(),
                Type: r.Type.ToString(),
                TypeLabel: RelationshipMapping.GetRelationTypeLabel(r.Type)))
            .ToList();
    }

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Task LinkedItems Spec Requirement ParentId
    /// </summary>
    private static string? ResolveTaskParentId(TaskItem task)
    {
        if (task.LinkedItems is null || task.LinkedItems.Count == 0)
            return null;

        // Prefer Spec link first, then Requirement
        var specLink = task.LinkedItems
            .FirstOrDefault(l => string.Equals(l.LinkedType, "spec", StringComparison.OrdinalIgnoreCase));
        if (specLink is not null)
            return specLink.LinkedEntityId.ToString();

        var reqLink = task.LinkedItems
            .FirstOrDefault(l => string.Equals(l.LinkedType, "requirement", StringComparison.OrdinalIgnoreCase));
        if (reqLink is not null)
            return reqLink.LinkedEntityId.ToString();

        return null;
    }

    private static string FormatConversationLabel(Conversation conversation)
    {
        var prefix = conversation.ConversationType == ConversationType.Channel ? "#" : "";
        var desc = !string.IsNullOrWhiteSpace(conversation.Description)
            ? $" \u2014 {conversation.Description}"
            : "";
        return $"{prefix}{conversation.Name}{desc}";
    }
}
