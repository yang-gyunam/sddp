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
/// spec status
/// </summary>
public sealed record TransitionSpecStatusCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId,
    SpecStatus NewStatus) : ICommand<SpecDto?>, IAuditableRequest<SpecDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SpecDto? response) => AuditLog;
}

public sealed class TransitionSpecStatusCommandHandler : IRequestHandler<TransitionSpecStatusCommand, SpecDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransitionSpecStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDto?> Handle(TransitionSpecStatusCommand request, CancellationToken cancellationToken)
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
        var transitionResult = spec.TransitionTo(request.NewStatus);
        transitionResult.EnsureSuccess("TRANSITION_ERROR");
        spec.SetUpdatedBy(request.UserId);
        spec.RecordStatusChange(request.UserId, previousStatus, request.NewStatus);

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
                to = request.NewStatus.ToString(),
                reason = (string?)null
            },
            request.TenantId,
            request.ProjectId);

        return SpecMapping.MapToDto(spec);
    }
}
