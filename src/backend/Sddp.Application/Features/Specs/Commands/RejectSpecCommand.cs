using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Commands;

/// <summary>
/// spec
/// </summary>
public sealed record RejectSpecCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId,
    string? Reason) : ICommand<SpecDto?>, IAuditableRequest<SpecDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SpecDto? response) => AuditLog;
}

public sealed class RejectSpecCommandHandler : IRequestHandler<RejectSpecCommand, SpecDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public RejectSpecCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDto?> Handle(RejectSpecCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return null;
        }

        var previousStatus = spec.Status;
        var rejectResult = spec.Reject();
        rejectResult.EnsureSuccess("REJECT_ERROR");
        spec.SetUpdatedBy(request.UserId);
        spec.RecordStatusChange(request.UserId, previousStatus, spec.Status, request.Reason);

        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "status",
            "spec",
            spec.Id,
            new
            {
                from = previousStatus.ToString(),
                to = spec.Status.ToString(),
                reason = request.Reason
            },
            request.TenantId,
            request.ProjectId);

        return SpecMapping.MapToDto(spec);
    }
}
