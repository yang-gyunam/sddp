using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Sla.Commands;

/// <summary>
/// SLA create
/// </summary>
public sealed record CreateSlaPolicyCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string SlaType,
    int SlaHours,
    int UrgentSlaHours,
    string? ReminderAtPercent,
    string? EscalationRole) : ICommand<SlaPolicyDto>, IAuditableRequest<SlaPolicyDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SlaPolicyDto response) => AuditLog;
}

public sealed class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSlaPolicyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SlaPolicyDto> Handle(
        CreateSlaPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SlaPolicy>();

        // project + SlaType
        var existing = await (repo.FindAsync(
            x => x.TenantId == request.TenantId
                && x.ProjectId == request.ProjectId
                && x.SlaType == request.SlaType
                && x.IsActive,
            cancellationToken)).ConfigureAwait(false);

        if (existing.Any())
        {
            throw new ConflictException(
                $"SLA policy for type '{request.SlaType}' already exists in this project");
        }

        var policy = new SlaPolicy(
            request.TenantId,
            request.ProjectId,
            request.SlaType,
            request.SlaHours,
            request.UrgentSlaHours,
            request.ReminderAtPercent,
            request.EscalationRole);

        await (repo.AddAsync(policy, cancellationToken)).ConfigureAwait(false);
        try
        {
            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex.ToString().Contains("uq_slap_project_type", StringComparison.Ordinal))
        {
            throw new ConflictException(
                $"SLA policy for type '{request.SlaType}' already exists in this project");
        }

        request.AuditLog = new AuditLogRequest(
            null,
            "create",
            "sla_policy",
            policy.Id,
            new { request.SlaType, request.SlaHours },
            request.TenantId,
            request.ProjectId);

        return SlaMapper.MapToDto(policy);
    }
}

/// <summary>
/// SLA update
/// </summary>
public sealed record UpdateSlaPolicyCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PolicyId,
    int SlaHours,
    int UrgentSlaHours,
    string? ReminderAtPercent,
    string? EscalationRole) : ICommand<SlaPolicyDto>, IAuditableRequest<SlaPolicyDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SlaPolicyDto response) => AuditLog;
}

public sealed class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSlaPolicyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SlaPolicyDto> Handle(
        UpdateSlaPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SlaPolicy>();
        var policy = await (repo.GetByIdAsync(request.PolicyId, cancellationToken)).ConfigureAwait(false);

        if (policy == null
            || policy.TenantId != request.TenantId
            || policy.ProjectId != request.ProjectId
            || !policy.IsActive)
        {
            throw new NotFoundException("SlaPolicy", request.PolicyId.ToString());
        }

        policy.Update(
            request.SlaHours,
            request.UrgentSlaHours,
            request.ReminderAtPercent,
            request.EscalationRole);

        await (repo.UpdateAsync(policy, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "update",
            "sla_policy",
            request.PolicyId,
            new { request.SlaHours, request.UrgentSlaHours },
            request.TenantId,
            request.ProjectId);

        return SlaMapper.MapToDto(policy);
    }
}

/// <summary>
/// SLA delete
/// </summary>
public sealed record DeleteSlaPolicyCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId PolicyId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => response ? AuditLog : null;
}

public sealed class DeleteSlaPolicyCommandHandler : IRequestHandler<DeleteSlaPolicyCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSlaPolicyCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteSlaPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<SlaPolicy>();
        var policy = await (repo.GetByIdAsync(request.PolicyId, cancellationToken)).ConfigureAwait(false);

        if (policy == null
            || policy.TenantId != request.TenantId
            || policy.ProjectId != request.ProjectId
            || !policy.IsActive)
        {
            return false;
        }

        await (repo.DeleteAsync(policy, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            null,
            "delete",
            "sla_policy",
            request.PolicyId,
            null,
            request.TenantId,
            request.ProjectId);

        return true;
    }
}
