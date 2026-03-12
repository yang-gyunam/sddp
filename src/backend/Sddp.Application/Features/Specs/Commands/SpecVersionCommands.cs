using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Commands;

/// <summary>
/// spec new create
/// </summary>
public sealed record CreateSpecNewVersionCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId) : ICommand<SpecDetailDto?>, IAuditableRequest<SpecDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SpecDetailDto? response) => AuditLog;
}

public sealed class CreateSpecNewVersionCommandHandler : IRequestHandler<CreateSpecNewVersionCommand, SpecDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpecNewVersionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDetailDto?> Handle(CreateSpecNewVersionCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var existingSpec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (existingSpec is null
            || existingSpec.TenantId != request.TenantId
            || existingSpec.ProjectId != request.ProjectId
            || !existingSpec.IsActive)
        {
            return null;
        }

        if (existingSpec.Status != SpecStatus.Locked)
        {
            throw new SddpException("VERSION_ERROR", "Only locked specs can have new versions created");
        }

        var newSpec = new Spec(
            existingSpec.TenantId,
            existingSpec.ProjectId,
            existingSpec.Code,
            existingSpec.Title,
            existingSpec.Description,
            existingSpec.Decision,
            existingSpec.Context,
            existingSpec.Scope,
            existingSpec.OutOfScope,
            existingSpec.Definitions,
            existingSpec.AcceptanceCriteria,
            existingSpec.Owners.ToNullableCsv(),
            existingSpec.ReviewTrigger,
            existingSpec.RequirementId,
            existingSpec.BornFromConversationId);

        newSpec.SetSupersedesSpec(existingSpec.Id);
        newSpec.SetCreatedBy(request.UserId);

        existingSpec.Expire();
        await (specRepo.UpdateAsync(existingSpec, cancellationToken)).ConfigureAwait(false);

        await (specRepo.AddAsync(newSpec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "create_version",
            "spec",
            newSpec.Id,
            new { newSpec.Code, PreviousSpecId = request.SpecId.ToString() },
            request.TenantId,
            request.ProjectId);

        return await (SpecMapping.MapToDetailDtoAsync(newSpec, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}
