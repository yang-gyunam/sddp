using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

public sealed partial class GetPrimaryFlowQueryHandler
{
    private async Task<Conversation?> FindConversation(
        GlobalUniqueId tenantId, GlobalUniqueId conversationId, CancellationToken ct)
    {
        // Try Channel first (most common)
        var channelRepo = _unitOfWork.Repository<Channel>();
        var channel = (await (channelRepo.FindAsync(
            c => c.Id == conversationId && c.TenantId == tenantId && c.IsActive,
            ct)).ConfigureAwait(false)).FirstOrDefault();
        if (channel is not null) return channel;

        // Try Forum
        var forumRepo = _unitOfWork.Repository<Forum>();
        var forum = (await (forumRepo.FindAsync(
            f => f.Id == conversationId && f.TenantId == tenantId && f.IsActive,
            ct)).ConfigureAwait(false)).FirstOrDefault();
        if (forum is not null) return forum;

        // Try DirectMessage
        var dmRepo = _unitOfWork.Repository<DirectMessage>();
        var dm = (await (dmRepo.FindAsync(
            d => d.Id == conversationId && d.TenantId == tenantId && d.IsActive,
            ct)).ConfigureAwait(false)).FirstOrDefault();
        return dm;
    }

    private async Task<IReadOnlyList<Requirement>> FindRequirementsByConversation(
        GlobalUniqueId tenantId, GlobalUniqueId conversationId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Requirement>();
        var requirements = await (repo.FindAsync(
            r => r.ConversationId == conversationId
                && r.TenantId == tenantId
                && r.IsActive
                && r.ValidTo == null,
            ct)).ConfigureAwait(false);

        return requirements
            .OrderBy(r => r.Level)
            .ThenByDescending(r => r.CreatedAt.ToDateTimeOffset())
            .ToList();
    }

    private async Task<IReadOnlyList<Spec>> FindSpecsByConversation(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, GlobalUniqueId conversationId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Spec>();
        var specs = await (repo.FindAsync(
            s => s.BornFromConversationId == conversationId
                && s.TenantId == tenantId
                && s.ProjectId == projectId
                && s.IsActive
                && s.ValidTo == null,
            ct)).ConfigureAwait(false);

        return specs.OrderByDescending(s => s.CreatedAt.ToDateTimeOffset()).ToList();
    }

    private async Task<IReadOnlyList<Spec>> FindSpecsByRequirement(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, GlobalUniqueId requirementId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Spec>();
        var specs = await (repo.FindAsync(
            s => s.RequirementId == requirementId
                && s.TenantId == tenantId
                && s.ProjectId == projectId
                && s.IsActive
                && s.ValidTo == null,
            ct)).ConfigureAwait(false);

        return specs.OrderByDescending(s => s.CreatedAt.ToDateTimeOffset()).ToList();
    }

    private async Task AppendGlossaryTerms(
        List<PrimaryFlowNodeDto> nodes,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        List<GlobalUniqueId> specIds,
        CancellationToken ct)
    {
        if (specIds.Count == 0) return;

        var relRepo = _unitOfWork.Repository<Relationship>();
        var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();

        // Find relationships from specs to glossary terms
        var glossaryRelationships = await (relRepo.FindAsync(
            r => r.TenantId == tenantId
                && r.ProjectId == projectId
                && r.FromEntityType == Abstractions.Enums.EntityType.Spec
                && r.ToEntityType == Abstractions.Enums.EntityType.GlossaryTerm
                && r.IsActive
                && r.ValidTo == null,
            ct)).ConfigureAwait(false);

        var glossaryTermIds = glossaryRelationships
            .Where(r => specIds.Contains(r.FromEntityId))
            .Select(r => r.ToEntityId)
            .Distinct()
            .ToList();

        foreach (var termId in glossaryTermIds)
        {
            var term = (await (glossaryRepo.FindAsync(
                g => g.Id == termId
                    && g.TenantId == tenantId
                    && g.IsActive
                    && g.ValidTo == null,
                ct)).ConfigureAwait(false)).FirstOrDefault();

            if (term is not null)
            {
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: term.Id.ToString(),
                    EntityType: "GlossaryTerm",
                    Label: term.Term,
                    Code: null,
                    Depth: 0,
                    IsCurrent: false));
            }
        }
    }

    private async Task AppendArtifacts(
        List<PrimaryFlowNodeDto> nodes,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        List<GlobalUniqueId> specIds,
        CancellationToken ct)
    {
        if (specIds.Count == 0) return;

        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();

        foreach (var specId in specIds)
        {
            var artifacts = await (artifactRepo.FindAsync(
                a => a.SpecId == specId
                    && a.TenantId == tenantId
                    && a.ProjectId == projectId
                    && a.IsActive,
                ct)).ConfigureAwait(false);

            foreach (var artifact in artifacts)
            {
                nodes.Add(new PrimaryFlowNodeDto(
                    Id: artifact.Id.ToString(),
                    EntityType: "Artifact",
                    Label: artifact.ArtifactPath,
                    Code: null,
                    Depth: 0,
                    IsCurrent: false));
            }
        }
    }

    private static string FormatConversationLabel(Conversation conversation)
    {
        var prefix = conversation.ConversationType == ConversationType.Channel ? "#" : "";
        var desc = !string.IsNullOrWhiteSpace(conversation.Description)
            ? $" — {conversation.Description}"
            : "";
        return $"{prefix}{conversation.Name}{desc}";
    }

    private static int LevelToDepth(RequirementLevel level) => level switch
    {
        RequirementLevel.A => 0,
        RequirementLevel.B => 1,
        RequirementLevel.C => 2,
        _ => 0
    };
}
