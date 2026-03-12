using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Admin.Commands;

/// <summary>
/// project
/// create → delete → status Planning
/// </summary>
public sealed record ResetProjectDataCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId,
    string ConfirmationCode) : ICommand<ProjectDataResetResultDto>, IAuditableRequest<ProjectDataResetResultDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDataResetResultDto response) => AuditLog;
}

public sealed class ResetProjectDataCommandHandler : IRequestHandler<ResetProjectDataCommand, ProjectDataResetResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectSnapshotService _snapshotService;

    public ResetProjectDataCommandHandler(IUnitOfWork unitOfWork, IProjectSnapshotService snapshotService)
    {
        _unitOfWork = unitOfWork;
        _snapshotService = snapshotService;
    }

    public async Task<ProjectDataResetResultDto> Handle(ResetProjectDataCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();

        // 1. project + tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new SddpException("RESET_PROJECT_DATA_FAILED", "Project not found");

        // 2.
        var expectedCode = $"RESET-{project.Code}";
        if (!string.Equals(request.ConfirmationCode, expectedCode, StringComparison.Ordinal))
            throw new SddpException("RESET_PROJECT_DATA_FAILED", "Invalid confirmation code");

        // 3. create
        var snapshot = await (_snapshotService.CreateSnapshotAsync(
            request.TenantId,
            request.ProjectId,
            request.RequestUserId,
            $"Pre-reset backup ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
            "Automatic snapshot created before data reset",
            "pre_reset",
            cancellationToken)).ConfigureAwait(false);

        // 4. delete
        var deletedCounts = await (_snapshotService.ResetProjectDataAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        var totalDeleted = deletedCounts.Values.Sum();

        // 5. project status Planning
        project.ResetStatus();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 6. audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "reset_project_data",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { SnapshotId = snapshot.Id, TotalRowsDeleted = totalDeleted },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return new ProjectDataResetResultDto(
            ProjectId: request.ProjectId.ToString(),
            SnapshotId: snapshot.Id,
            TotalRowsDeleted: totalDeleted,
            DeletedTableCounts: deletedCounts);
    }
}
