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
public sealed record SubmitSpecForReviewCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId) : ICommand<SpecDto?>, IAuditableRequest<SpecDto?>
{
    public AuditLogRequest? AuditLog { get; set; }

    public AuditLogRequest? GetAuditLogRequest(SpecDto? response) => AuditLog;
}

public sealed class SubmitSpecForReviewCommandHandler : IRequestHandler<SubmitSpecForReviewCommand, SpecDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnalysisTriggerService _analysisTrigger;

    public SubmitSpecForReviewCommandHandler(
        IUnitOfWork unitOfWork,
        IAnalysisTriggerService analysisTrigger)
    {
        _unitOfWork = unitOfWork;
        _analysisTrigger = analysisTrigger;
    }

    public async Task<SpecDto?> Handle(SubmitSpecForReviewCommand request, CancellationToken cancellationToken)
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
        var submitResult = spec.SubmitForReview();
        submitResult.EnsureSuccess("SUBMIT_ERROR");
        spec.SetUpdatedBy(request.UserId);
        spec.RecordStatusChange(request.UserId, previousStatus, spec.Status);

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
                reason = (string?)null
            },
            request.TenantId,
            request.ProjectId);

        await (_analysisTrigger.TriggerAsync(
            request.TenantId, request.ProjectId, "MissingField",
            request.SpecId, "spec", cancellationToken: cancellationToken)).ConfigureAwait(false);
        await (_analysisTrigger.TriggerAsync(
            request.TenantId, request.ProjectId, "Quality",
            request.SpecId, "spec", cancellationToken: cancellationToken)).ConfigureAwait(false);

        return SpecMapping.MapToDto(spec);
    }
}
