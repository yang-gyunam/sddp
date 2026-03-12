using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Queries;

/// <summary>
/// Conversation Requirement get
/// </summary>
public sealed record GetLinkedRequirementsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId) : IQuery<IEnumerable<LinkedRequirementDto>>;

public sealed class GetLinkedRequirementsQueryHandler : IRequestHandler<GetLinkedRequirementsQuery, IEnumerable<LinkedRequirementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLinkedRequirementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LinkedRequirementDto>> Handle(GetLinkedRequirementsQuery request, CancellationToken cancellationToken)
    {
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var requirements = await (requirementRepo.FindAsync(
            r => r.ConversationId == request.ConversationId
                && r.TenantId == request.TenantId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        return requirements
            .OrderBy(r => r.Level)
            .ThenByDescending(r => r.CreatedAt.ToDateTimeOffset())
            .Select(r => new LinkedRequirementDto(
                r.Id.ToString(),
                r.Code,
                r.Title,
                r.Level,
                r.Priority,
                r.UpdatedAt.ToDateTimeOffset()));
    }
}
