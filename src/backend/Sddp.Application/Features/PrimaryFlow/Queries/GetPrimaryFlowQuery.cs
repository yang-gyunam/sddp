using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Application.Features.Relationships.Queries;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

/// <summary>
/// Primary Flow get (FK)
/// Conversation → Requirement → Spec → GlossaryTerm / Artifact
/// </summary>
public sealed record GetPrimaryFlowQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string EntityType,
    GlobalUniqueId EntityId) : IQuery<IReadOnlyList<PrimaryFlowNodeDto>>, IRelationshipReadQuery;

public sealed partial class GetPrimaryFlowQueryHandler
    : IRequestHandler<GetPrimaryFlowQuery, IReadOnlyList<PrimaryFlowNodeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRelationshipEndpointAccess _access;

    public GetPrimaryFlowQueryHandler(IUnitOfWork unitOfWork, IRelationshipEndpointAccess access)
    {
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<IReadOnlyList<PrimaryFlowNodeDto>> Handle(
        GetPrimaryFlowQuery request,
        CancellationToken cancellationToken)
    {
        // Keep unknown types as the existing empty flow; known roots use the same
        // project, permission and private membership policy as relationship graphs.
        var name = request.EntityType.ToLowerInvariant();
        if (name is not ("conversation" or "requirement" or "spec" or "artifact" or "glossaryterm" or "task" or "diagram"))
            return [];
        await _access.RequireRootAsync(request.TenantId, request.ProjectId,
            new(Enum.Parse<EntityType>(name, true), request.EntityId), cancellationToken).ConfigureAwait(false);

        var nodes = name switch
        {
            "conversation" => await (BuildConversationFlow(request, cancellationToken)).ConfigureAwait(false),
            "requirement" => await (BuildRequirementFlow(request, cancellationToken)).ConfigureAwait(false),
            "spec" => await (BuildSpecFlow(request, cancellationToken)).ConfigureAwait(false),
            "artifact" => await (BuildArtifactFlow(request, cancellationToken)).ConfigureAwait(false),
            "glossaryterm" => await (BuildGlossaryTermFlow(request, cancellationToken)).ConfigureAwait(false),
            "task" => await (BuildTaskFlow(request, cancellationToken)).ConfigureAwait(false),
            _ => []
        };
        var readable = await _access.ReadAsync(request.TenantId, request.ProjectId,
            nodes.Select(n => new RelationshipEndpoint(Enum.Parse<EntityType>(n.EntityType), GlobalUniqueId.Parse(n.Id))),
            cancellationToken).ConfigureAwait(false);
        return nodes.Where(n => readable.ContainsKey(new(Enum.Parse<EntityType>(n.EntityType), GlobalUniqueId.Parse(n.Id)))).ToArray();
    }
}
