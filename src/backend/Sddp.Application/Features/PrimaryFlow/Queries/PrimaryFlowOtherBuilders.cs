using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

public sealed partial class GetPrimaryFlowQueryHandler
{
    /// <summary>
    /// Requirement flow: Conversation → Parent → Current → Children → Specs → GlossaryTerms → Artifacts
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildRequirementFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();

        var reqRepo = _unitOfWork.Repository<Requirement>();
        var current = (await (reqRepo.FindAsync(
            r => r.Id == request.EntityId
                && r.TenantId == request.TenantId
                && r.IsActive
                && r.ValidTo == null,
            ct)).ConfigureAwait(false)).FirstOrDefault();

        if (current is null) return nodes;

        // Upstream conversation
        if (current.ConversationId.HasValue)
        {
            var conv = await (FindConversation(request.TenantId, current.ConversationId.Value, ct)).ConfigureAwait(false);
            if (conv is not null)
            {
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: conv.Id.ToString(),
                    EntityType: "Conversation",
                    Label: FormatConversationLabel(conv),
                    Code: null,
                    Depth: 0,
                    IsCurrent: false));
            }
        }

        // Parent requirement
        bool hasParent = false;
        if (current.ParentId.HasValue)
        {
            var parent = (await (reqRepo.FindAsync(
                r => r.Id == current.ParentId.Value
                    && r.TenantId == request.TenantId
                    && r.IsActive
                    && r.ValidTo == null,
                ct)).ConfigureAwait(false)).FirstOrDefault();

            if (parent is not null)
            {
                hasParent = true;
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: parent.Id.ToString(),
                    EntityType: "Requirement",
                    Label: parent.Title,
                    Code: parent.Code,
                    Depth: 0,
                    IsCurrent: false));
            }
        }

        // Current requirement
        nodes.Add(new PrimaryFlowNodeDto(
            Id: current.Id.ToString(),
            EntityType: "Requirement",
            Label: current.Title,
            Code: current.Code,
            Depth: hasParent ? 1 : 0,
            IsCurrent: true));

        // Children requirements
        var children = await (reqRepo.FindAsync(
            r => r.ParentId == request.EntityId
                && r.TenantId == request.TenantId
                && r.IsActive
                && r.ValidTo == null,
            ct)).ConfigureAwait(false);

        var currentDepth = hasParent ? 1 : 0;
        foreach (var child in children.OrderBy(c => c.Level).ThenByDescending(c => c.CreatedAt.ToDateTimeOffset()))
        {
            nodes.Add(new PrimaryFlowNodeDto(
                Id: child.Id.ToString(),
                EntityType: "Requirement",
                Label: child.Title,
                Code: child.Code,
                Depth: currentDepth + 1,
                IsCurrent: false));
        }

        // Downstream specs (FK: spec.requirement_id)
        var specs = await (FindSpecsByRequirement(
            request.TenantId, request.ProjectId, request.EntityId, ct)).ConfigureAwait(false);

        var specIds = new List<GlobalUniqueId>();
        foreach (var spec in specs)
        {
            specIds.Add(spec.Id);
            nodes.Add(new PrimaryFlowNodeDto(
                Id: spec.Id.ToString(),
                EntityType: "Spec",
                Label: spec.Title,
                Code: spec.Code,
                Depth: 0,
                IsCurrent: false));
        }

        // GlossaryTerms via relationship table
        await (AppendGlossaryTerms(nodes, request.TenantId, request.ProjectId, specIds, ct)).ConfigureAwait(false);

        // Artifacts
        await (AppendArtifacts(nodes, request.TenantId, request.ProjectId, specIds, ct)).ConfigureAwait(false);

        return nodes;
    }

    /// <summary>
    /// Task flow: show linked items as primary flow (Conversation → Requirement → Spec → Artifact)
    /// Task itself is current node; linked items shown upstream sorted by type order.
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildTaskFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.EntityId, ct)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId) return nodes;

        // Build linked items as upstream flow
        var typeOrder = new[] { "conversation", "requirement", "spec", "artifact" };

        foreach (var linkedItem in (task.LinkedItems ?? [])
            .OrderBy(l => Array.IndexOf(typeOrder, l.LinkedType.ToLowerInvariant())))
        {
            var linkedType = linkedItem.LinkedType.ToLowerInvariant();
            string entityType;
            string label = linkedType;

            switch (linkedType)
            {
                case "conversation":
                    entityType = "Conversation";
                    var conv = await (FindConversation(request.TenantId, linkedItem.LinkedEntityId, ct)).ConfigureAwait(false);
                    if (conv is not null) label = FormatConversationLabel(conv);
                    break;
                case "requirement":
                    entityType = "Requirement";
                    var reqRepo = _unitOfWork.Repository<Requirement>();
                    var req = (await (reqRepo.FindAsync(
                        r => r.Id == linkedItem.LinkedEntityId && r.TenantId == request.TenantId && r.IsActive && r.ValidTo == null, ct)).ConfigureAwait(false))
                        .FirstOrDefault();
                    if (req is not null) label = req.Title;
                    break;
                case "spec":
                    entityType = "Spec";
                    var specRepo = _unitOfWork.Repository<Spec>();
                    var spec = (await (specRepo.FindAsync(
                        s => s.Id == linkedItem.LinkedEntityId && s.TenantId == request.TenantId && s.IsActive && s.ValidTo == null, ct)).ConfigureAwait(false))
                        .FirstOrDefault();
                    if (spec is not null) label = spec.Title;
                    break;
                case "artifact":
                    entityType = "Artifact";
                    var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();
                    var artifact = (await (artifactRepo.FindAsync(
                        a => a.Id == linkedItem.LinkedEntityId && a.TenantId == request.TenantId && a.IsActive, ct)).ConfigureAwait(false))
                        .FirstOrDefault();
                    if (artifact is not null) label = artifact.ArtifactPath;
                    break;
                default:
                    continue;
            }

            nodes.Add(new PrimaryFlowNodeDto(
                Id: linkedItem.LinkedEntityId.ToString(),
                EntityType: entityType,
                Label: label,
                Code: null,
                Depth: 0,
                IsCurrent: false));
        }

        // Current task node
        nodes.Add(new PrimaryFlowNodeDto(
            Id: task.Id.ToString(),
            EntityType: "Task",
            Label: task.Title,
            Code: null,
            Depth: 0,
            IsCurrent: true));

        return nodes;
    }
}
