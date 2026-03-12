using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

public sealed partial class GetPrimaryFlowQueryHandler
{
    /// <summary>
    /// Conversation flow: Current → Requirements(by level) → Specs → GlossaryTerms → Artifacts
    /// </summary>
    private async Task<IReadOnlyList<PrimaryFlowNodeDto>> BuildConversationFlow(
        GetPrimaryFlowQuery request,
        CancellationToken ct)
    {
        var nodes = new List<PrimaryFlowNodeDto>();

        // Current conversation
        var conversation = await (FindConversation(request.TenantId, request.EntityId, ct)).ConfigureAwait(false);
        if (conversation is null) return nodes;

        nodes.Add(new PrimaryFlowNodeDto(
            Id: conversation.Id.ToString(),
            EntityType: "Conversation",
            Label: FormatConversationLabel(conversation),
            Code: null,
            Depth: 0,
            IsCurrent: true));

        // Downstream requirements (FK: requirement.conversation_id)
        var requirements = await (FindRequirementsByConversation(
            request.TenantId, request.EntityId, ct)).ConfigureAwait(false);

        foreach (var req in requirements)
        {
            nodes.Add(new PrimaryFlowNodeDto(
                Id: req.Id.ToString(),
                EntityType: "Requirement",
                Label: req.Title,
                Code: req.Code,
                Depth: LevelToDepth(req.Level),
                IsCurrent: false));
        }

        // Downstream specs (FK: spec.born_from_conversation_id)
        var specs = await (FindSpecsByConversation(
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

        // Artifacts (FK: artifact_tracking.spec_id)
        await (AppendArtifacts(nodes, request.TenantId, request.ProjectId, specIds, ct)).ConfigureAwait(false);

        return nodes;
    }
}
