using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

public sealed partial class GetPrimaryFlowQueryHandler
{
    /// <summary>
    /// Spec flow: Conversation → Requirement → Current → GlossaryTerms → Artifacts
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildSpecFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();

        var specRepo = _unitOfWork.Repository<Spec>();
        var current = (await (specRepo.FindAsync(
            s => s.Id == request.EntityId
                && s.TenantId == request.TenantId
                && s.IsActive
                && s.ValidTo == null,
            ct)).ConfigureAwait(false)).FirstOrDefault();

        if (current is null) return nodes;

        // Upstream conversation
        if (current.BornFromConversationId.HasValue)
        {
            var conv = await (FindConversation(request.TenantId, current.BornFromConversationId.Value, ct)).ConfigureAwait(false);
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

        // Upstream requirement
        if (current.RequirementId.HasValue)
        {
            var reqRepo = _unitOfWork.Repository<Requirement>();
            var req = (await (reqRepo.FindAsync(
                r => r.Id == current.RequirementId.Value
                    && r.TenantId == request.TenantId
                    && r.IsActive
                    && r.ValidTo == null,
                ct)).ConfigureAwait(false)).FirstOrDefault();

            if (req is not null)
            {
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: req.Id.ToString(),
                    EntityType: "Requirement",
                    Label: req.Title,
                    Code: req.Code,
                    Depth: 0,
                    IsCurrent: false));
            }
        }

        // Current spec
        nodes.Add(new PrimaryFlowNodeDto(
            Id: current.Id.ToString(),
            EntityType: "Spec",
            Label: current.Title,
            Code: current.Code,
            Depth: 0,
            IsCurrent: true));

        // GlossaryTerms via relationship table
        var specIds = new List<GlobalUniqueId> { current.Id };
        await (AppendGlossaryTerms(nodes, request.TenantId, request.ProjectId, specIds, ct)).ConfigureAwait(false);

        // Artifacts
        await (AppendArtifacts(nodes, request.TenantId, request.ProjectId, specIds, ct)).ConfigureAwait(false);

        return nodes;
    }

    /// <summary>
    /// Artifact flow: Conversation → Requirement → Spec(parent) → Current Artifact + Sibling Artifacts
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildArtifactFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();

        // Find current artifact
        var current = (await (artifactRepo.FindAsync(
            a => a.Id == request.EntityId
                && a.TenantId == request.TenantId
                && a.ProjectId == request.ProjectId
                && a.IsActive, ct)).ConfigureAwait(false)).FirstOrDefault();
        if (current is null) return nodes;

        // Find parent Spec (no ValidTo filter — artifact may reference a versioned spec)
        var specRepo = _unitOfWork.Repository<Spec>();
        var spec = (await (specRepo.FindAsync(
            s => s.Id == current.SpecId
                && s.TenantId == request.TenantId
                && s.IsActive, ct)).ConfigureAwait(false)).FirstOrDefault();

        if (spec is not null)
        {
            // Upstream Conversation (from Spec)
            if (spec.BornFromConversationId.HasValue)
            {
                var conv = await (FindConversation(request.TenantId, spec.BornFromConversationId.Value, ct)).ConfigureAwait(false);
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

            // Upstream Requirement (from Spec)
            if (spec.RequirementId.HasValue)
            {
                var reqRepo = _unitOfWork.Repository<Requirement>();
                var req = (await (reqRepo.FindAsync(
                    r => r.Id == spec.RequirementId.Value
                        && r.TenantId == request.TenantId
                        && r.IsActive
                        && r.ValidTo == null,
                    ct)).ConfigureAwait(false)).FirstOrDefault();

                if (req is not null)
                {
                    nodes.Add(new PrimaryFlowNodeDto(
                        Id: req.Id.ToString(),
                        EntityType: "Requirement",
                        Label: req.Title,
                        Code: req.Code,
                        Depth: 0,
                        IsCurrent: false));
                }
            }

            // Parent Spec
            nodes.Add(new PrimaryFlowNodeDto(
                Id: spec.Id.ToString(),
                EntityType: "Spec",
                Label: spec.Title,
                Code: spec.Code,
                Depth: 0,
                IsCurrent: false));
        }

        // Current Artifact
        nodes.Add(new PrimaryFlowNodeDto(
            Id: current.Id.ToString(),
            EntityType: "Artifact",
            Label: current.ArtifactPath,
            Code: null,
            Depth: 0,
            IsCurrent: true));

        // Sibling Artifacts (same Spec, excluding current)
        if (spec is not null)
        {
            var siblings = await (artifactRepo.FindAsync(
                a => a.SpecId == spec.Id
                    && a.Id != current.Id
                    && a.TenantId == request.TenantId
                    && a.ProjectId == request.ProjectId
                    && a.IsActive, ct)).ConfigureAwait(false);

            foreach (var sibling in siblings)
            {
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: sibling.Id.ToString(),
                    EntityType: "Artifact",
                    Label: sibling.ArtifactPath,
                    Code: null,
                    Depth: 0,
                    IsCurrent: false));
            }
        }

        return nodes;
    }

    /// <summary>
    /// GlossaryTerm flow: Conversation → Requirement → Spec(source) → Current GlossaryTerm
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildGlossaryTermFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();
        var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();

        // Find current glossary term
        var current = (await (glossaryRepo.FindAsync(
            g => g.Id == request.EntityId
                && g.TenantId == request.TenantId
                && g.IsActive
                && g.ValidTo == null, ct)).ConfigureAwait(false)).FirstOrDefault();
        if (current is null) return nodes;

        var addedIds = new HashSet<string>();

        // Upstream via SourceSpecId (no ValidTo filter — may reference a versioned spec)
        if (current.SourceSpecId.HasValue)
        {
            var specRepo = _unitOfWork.Repository<Spec>();
            var spec = (await (specRepo.FindAsync(
                s => s.Id == current.SourceSpecId.Value
                    && s.TenantId == request.TenantId
                    && s.IsActive, ct)).ConfigureAwait(false)).FirstOrDefault();

            if (spec is not null)
            {
                // Upstream Conversation (from Spec)
                if (spec.BornFromConversationId.HasValue)
                {
                    var conv = await (FindConversation(request.TenantId, spec.BornFromConversationId.Value, ct)).ConfigureAwait(false);
                    if (conv is not null && addedIds.Add(conv.Id.ToString()))
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

                // Upstream Requirement (from Spec)
                if (spec.RequirementId.HasValue)
                {
                    var reqRepo = _unitOfWork.Repository<Requirement>();
                    var req = (await (reqRepo.FindAsync(
                        r => r.Id == spec.RequirementId.Value
                            && r.TenantId == request.TenantId
                            && r.IsActive
                            && r.ValidTo == null, ct)).ConfigureAwait(false)).FirstOrDefault();

                    if (req is not null && addedIds.Add(req.Id.ToString()))
                    {
                        nodes.Add(new PrimaryFlowNodeDto(
                            Id: req.Id.ToString(),
                            EntityType: "Requirement",
                            Label: req.Title,
                            Code: req.Code,
                            Depth: 0,
                            IsCurrent: false));
                    }
                }

                // Source Spec
                addedIds.Add(spec.Id.ToString());
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: spec.Id.ToString(),
                    EntityType: "Spec",
                    Label: spec.Title,
                    Code: spec.Code,
                    Depth: 0,
                    IsCurrent: false));
            }
        }

        // Direct SourceConversationId (if different from spec's conversation)
        if (current.SourceConversationId.HasValue)
        {
            var convId = current.SourceConversationId.Value.ToString();
            if (addedIds.Add(convId))
            {
                var conv = await (FindConversation(request.TenantId, current.SourceConversationId.Value, ct)).ConfigureAwait(false);
                if (conv is not null)
                {
                    nodes.Insert(0, new PrimaryFlowNodeDto(
                        Id: conv.Id.ToString(),
                        EntityType: "Conversation",
                        Label: FormatConversationLabel(conv),
                        Code: null,
                        Depth: 0,
                        IsCurrent: false));
                }
            }
        }

        // Direct SourceRequirementId (if different from spec's requirement)
        if (current.SourceRequirementId.HasValue)
        {
            var reqId = current.SourceRequirementId.Value.ToString();
            if (addedIds.Add(reqId))
            {
                var reqRepo = _unitOfWork.Repository<Requirement>();
                var req = (await (reqRepo.FindAsync(
                    r => r.Id == current.SourceRequirementId.Value
                        && r.TenantId == request.TenantId
                        && r.IsActive
                        && r.ValidTo == null, ct)).ConfigureAwait(false)).FirstOrDefault();

                if (req is not null)
                {
                    // Insert after conversations but before spec
                    var insertIdx = nodes.FindIndex(n => n.EntityType == "Spec");
                    if (insertIdx < 0) insertIdx = nodes.Count;
                    nodes.Insert(insertIdx, new PrimaryFlowNodeDto(
                        Id: req.Id.ToString(),
                        EntityType: "Requirement",
                        Label: req.Title,
                        Code: req.Code,
                        Depth: 0,
                        IsCurrent: false));
                }
            }
        }

        // Current GlossaryTerm
        nodes.Add(new PrimaryFlowNodeDto(
            Id: current.Id.ToString(),
            EntityType: "GlossaryTerm",
            Label: current.Term,
            Code: null,
            Depth: 0,
            IsCurrent: true));

        return nodes;
    }
}
