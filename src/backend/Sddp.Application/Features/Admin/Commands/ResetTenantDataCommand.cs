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
/// tenant all
/// project: → delete → Planning status
/// tenant conversation delete
/// </summary>
public sealed record ResetTenantDataCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId RequestUserId,
    string ConfirmationToken) : ICommand<TenantDataResetResultDto>, IAuditableRequest<TenantDataResetResultDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TenantDataResetResultDto response) => AuditLog;
}

public sealed class ResetTenantDataCommandHandler : IRequestHandler<ResetTenantDataCommand, TenantDataResetResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectSnapshotService _snapshotService;

    public ResetTenantDataCommandHandler(IUnitOfWork unitOfWork, IProjectSnapshotService snapshotService)
    {
        _unitOfWork = unitOfWork;
        _snapshotService = snapshotService;
    }

    public async Task<TenantDataResetResultDto> Handle(ResetTenantDataCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();

        // 1.
        var tenantIdPrefix = request.TenantId.ToString()[..8];
        var expectedToken = $"RESET-TENANT-{tenantIdPrefix}";
        if (!string.Equals(request.ConfirmationToken, expectedToken, StringComparison.Ordinal))
            throw new SddpException("RESET_TENANT_DATA_FAILED", "Invalid confirmation token");

        // 2. tenant project get
        var allProjects = await (projectRepo.FindAsync(
            p => p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);

        var totalDeleted = 0;
        var snapshotsCreated = 0;

        // 3. project
        foreach (var project in allProjects)
        {
            // create
            await (_snapshotService.CreateSnapshotAsync(
                request.TenantId,
                project.Id,
                request.RequestUserId,
                $"Pre-tenant-reset backup ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
                "Automatic snapshot created before tenant data reset",
                "pre_reset",
                cancellationToken)).ConfigureAwait(false);
            snapshotsCreated++;

            // delete
            var deletedCounts = await (_snapshotService.ResetProjectDataAsync(
                request.TenantId,
                project.Id,
                cancellationToken)).ConfigureAwait(false);
            totalDeleted += deletedCounts.Values.Sum();

            // status
            project.ResetStatus();
        }

        // 4. TenantWide conversation delete (project_id IS NULL)
        var tenantWideDeleted = await (_snapshotService.DeleteTenantWideConversationsAsync(
            request.TenantId, cancellationToken)).ConfigureAwait(false);
        totalDeleted += tenantWideDeleted;

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 5. audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "reset_tenant_data",
            ResourceType: "tenant",
            ResourceId: request.TenantId,
            Payload: new { ProjectsReset = allProjects.Count, SnapshotsCreated = snapshotsCreated, TotalRowsDeleted = totalDeleted },
            TenantId: request.TenantId,
            ProjectId: null);

        return new TenantDataResetResultDto(
            TenantId: request.TenantId.ToString(),
            ProjectsReset: allProjects.Count,
            SnapshotsCreated: snapshotsCreated,
            TotalRowsDeleted: totalDeleted);
    }
}
