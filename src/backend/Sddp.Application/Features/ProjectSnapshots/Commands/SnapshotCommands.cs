using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;

namespace Sddp.Application.Features.ProjectSnapshots.Commands;

/// <summary>
/// project create
/// </summary>
public sealed record CreateProjectSnapshotCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId,
    CreateProjectSnapshotDto Dto) : ICommand<ProjectSnapshotDto>, IAuditableRequest<ProjectSnapshotDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectSnapshotDto response) => AuditLog;
}

public sealed class CreateProjectSnapshotCommandHandler
    : IRequestHandler<CreateProjectSnapshotCommand, ProjectSnapshotDto>
{
    private readonly IProjectSnapshotService _snapshotService;

    public CreateProjectSnapshotCommandHandler(IProjectSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public async Task<ProjectSnapshotDto> Handle(
        CreateProjectSnapshotCommand request, CancellationToken cancellationToken)
    {
        var result = await (_snapshotService.CreateSnapshotAsync(
            request.TenantId,
            request.ProjectId,
            request.RequestUserId,
            request.Dto.Name,
            request.Dto.Description,
            "manual",
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "create_snapshot",
            ResourceType: "project_snapshot",
            ResourceId: GlobalUniqueId.Parse(result.Id),
            Payload: new { result.Name, result.DataSizeBytes, result.TableCounts },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return result;
    }
}

/// <summary>
/// project
/// </summary>
public sealed record RestoreProjectSnapshotCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SnapshotId,
    GlobalUniqueId RequestUserId) : ICommand<ProjectSnapshotDto>, IAuditableRequest<ProjectSnapshotDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectSnapshotDto response) => AuditLog;
}

public sealed class RestoreProjectSnapshotCommandHandler
    : IRequestHandler<RestoreProjectSnapshotCommand, ProjectSnapshotDto>
{
    private readonly IProjectSnapshotService _snapshotService;

    public RestoreProjectSnapshotCommandHandler(IProjectSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public async Task<ProjectSnapshotDto> Handle(
        RestoreProjectSnapshotCommand request, CancellationToken cancellationToken)
    {
        var result = await (_snapshotService.RestoreSnapshotAsync(
            request.TenantId,
            request.ProjectId,
            request.SnapshotId,
            request.RequestUserId,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "restore_snapshot",
            ResourceType: "project_snapshot",
            ResourceId: request.SnapshotId,
            Payload: new { result.Name, result.DataSizeBytes },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return result;
    }
}

/// <summary>
/// project delete
/// </summary>
public sealed record DeleteProjectSnapshotCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId SnapshotId,
    GlobalUniqueId RequestUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class DeleteProjectSnapshotCommandHandler
    : IRequestHandler<DeleteProjectSnapshotCommand, bool>
{
    private readonly IProjectSnapshotService _snapshotService;

    public DeleteProjectSnapshotCommandHandler(IProjectSnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public async Task<bool> Handle(
        DeleteProjectSnapshotCommand request, CancellationToken cancellationToken)
    {
        await (_snapshotService.DeleteSnapshotAsync(
            request.TenantId,
            request.ProjectId,
            request.SnapshotId,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "delete_snapshot",
            ResourceType: "project_snapshot",
            ResourceId: request.SnapshotId,
            Payload: null,
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}
