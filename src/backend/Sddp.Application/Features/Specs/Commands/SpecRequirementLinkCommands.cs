using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Specs.Commands;

/// <summary>
/// spec-requirement
/// </summary>
public sealed record LinkSpecRequirementCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId RequirementId,
    GlobalUniqueId UserId) : ICommand<SpecDto?>, IAuditableRequest<SpecDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SpecDto? response) => AuditLog;
}

public sealed class LinkSpecRequirementCommandHandler : IRequestHandler<LinkSpecRequirementCommand, SpecDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public LinkSpecRequirementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDto?> Handle(LinkSpecRequirementCommand request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var requirementRepo = _unitOfWork.Repository<Requirement>();

        var spec = await (specRepo.GetByIdAsync(request.SpecId, cancellationToken)).ConfigureAwait(false);
        if (spec is null
            || spec.TenantId != request.TenantId
            || spec.ProjectId != request.ProjectId
            || !spec.IsActive)
        {
            return null;
        }

        var requirement = await (requirementRepo.GetByIdAsync(request.RequirementId, cancellationToken)).ConfigureAwait(false);
        if (requirement is null
            || requirement.TenantId != request.TenantId
            || requirement.ProjectId != request.ProjectId
            || !requirement.IsActive)
        {
            throw new SddpException("LINK_ERROR", "Requirement not found");
        }

        var linkResult = spec.LinkRequirement(request.RequirementId);
        linkResult.EnsureSuccess("LINK_ERROR");
        spec.SetUpdatedBy(request.UserId);
        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "link_requirement",
            "spec",
            request.SpecId,
            new { RequirementId = request.RequirementId.ToString() },
            request.TenantId,
            request.ProjectId);

        return SpecMapping.MapToDto(spec);
    }
}

/// <summary>
/// spec-requirement
/// </summary>
public sealed record UnlinkSpecRequirementCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SpecId,
    GlobalUniqueId UserId) : ICommand<SpecDto?>, IAuditableRequest<SpecDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SpecDto? response) => AuditLog;
}

public sealed class UnlinkSpecRequirementCommandHandler : IRequestHandler<UnlinkSpecRequirementCommand, SpecDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnlinkSpecRequirementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpecDto?> Handle(UnlinkSpecRequirementCommand request, CancellationToken cancellationToken)
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

        var unlinkResult = spec.UnlinkRequirement();
        unlinkResult.EnsureSuccess("UNLINK_ERROR");
        spec.SetUpdatedBy(request.UserId);
        await (specRepo.UpdateAsync(spec, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "unlink_requirement",
            "spec",
            request.SpecId,
            null,
            request.TenantId,
            request.ProjectId);

        return SpecMapping.MapToDto(spec);
    }
}
