using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.PrimaryFlow.Queries;

/// <summary>
/// Primary Flow get (FK)
/// Conversation → Requirement → Spec → GlossaryTerm / Artifact
/// </summary>
public sealed record GetPrimaryFlowQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string EntityType,
    GlobalUniqueId EntityId) : IQuery<IReadOnlyList<PrimaryFlowNodeDto>>;

public sealed partial class GetPrimaryFlowQueryHandler
    : IRequestHandler<GetPrimaryFlowQuery, IReadOnlyList<PrimaryFlowNodeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPrimaryFlowQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PrimaryFlowNodeDto>> Handle(
        GetPrimaryFlowQuery request,
        CancellationToken cancellationToken)
    {
        return request.EntityType.ToLowerInvariant() switch
        {
            "conversation" => await (BuildConversationFlow(request, cancellationToken)).ConfigureAwait(false),
            "requirement" => await (BuildRequirementFlow(request, cancellationToken)).ConfigureAwait(false),
            "spec" => await (BuildSpecFlow(request, cancellationToken)).ConfigureAwait(false),
            "artifact" => await (BuildArtifactFlow(request, cancellationToken)).ConfigureAwait(false),
            "glossaryterm" => await (BuildGlossaryTermFlow(request, cancellationToken)).ConfigureAwait(false),
            "task" => await (BuildTaskFlow(request, cancellationToken)).ConfigureAwait(false),
            _ => []
        };
    }
}
