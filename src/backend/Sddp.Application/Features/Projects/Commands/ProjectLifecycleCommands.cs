using MediatR;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects.Commands;

// =============================================================================
// InitializeProjectCommand - Planning -> Active
// =============================================================================

/// <summary>
/// Initializes a project (Planning -> Active)
/// Requires at least one member before initialization
/// </summary>
public sealed record InitializeProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId) : ICommand<ProjectDetailDto?>, IAuditableRequest<ProjectDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDetailDto? response) => AuditLog;
}

public sealed class InitializeProjectCommandHandler : IRequestHandler<InitializeProjectCommand, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public InitializeProjectCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ProjectDetailDto?> Handle(InitializeProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var memberRepo = _unitOfWork.Repository<ProjectMember>();

        // 1. Verify that the project exists and belongs to the tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new SddpException("INITIALIZE_PROJECT_FAILED", "Project not found");

        // 2. Verify Planning status
        if (!project.Status.CanInitialize())
            throw new SddpException("INITIALIZE_PROJECT_FAILED", $"Cannot initialize project in {project.Status} status");

        // 2.5 Enforce active-project limit (max 5 per tenant)
        var activeProjects = await (projectRepo.FindAsync(
            p => p.TenantId == request.TenantId && p.Status == ProjectStatus.Active,
            cancellationToken)).ConfigureAwait(false);
        if (activeProjects.Count() >= BusinessLimits.MaxActiveProjectsPerTenant)
            throw new SddpException("ACTIVE_PROJECT_LIMIT_EXCEEDED",
                $"Tenant cannot have more than {BusinessLimits.MaxActiveProjectsPerTenant} active projects");

        // 3. Ensure at least one member besides the owner
        var members = await (memberRepo.FindAsync(
            m => m.ProjectId == request.ProjectId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        if (members.Count(m => m.UserId != project.OwnerId) < 1)
            throw new SddpException("INITIALIZE_PROJECT_FAILED", "At least one member besides the owner is required");

        // 4. Transition state
        project.Initialize();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 5. Audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "initialize_project",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        // 6. Return ProjectDetailDto
        return await (_sender.Send(
            new Queries.GetProjectByIdQuery(request.TenantId, request.ProjectId),
            cancellationToken)).ConfigureAwait(false);
    }
}

// =============================================================================
// ConcludeProjectCommand - Active -> Concluded
// =============================================================================

/// <summary>
/// Concludes a project (Active -> Concluded)
/// Requires all specs to be Approved or Locked
/// </summary>
public sealed record ConcludeProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId,
    string? Reason) : ICommand<ProjectDetailDto?>, IAuditableRequest<ProjectDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDetailDto? response) => AuditLog;
}

public sealed class ConcludeProjectCommandHandler : IRequestHandler<ConcludeProjectCommand, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public ConcludeProjectCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ProjectDetailDto?> Handle(ConcludeProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var specRepo = _unitOfWork.Repository<Spec>();

        // 1. Verify that the project exists and belongs to the tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new SddpException("CONCLUDE_PROJECT_FAILED", "Project not found");

        // 2. Verify Active status
        if (!project.Status.CanConclude())
            throw new SddpException("CONCLUDE_PROJECT_FAILED", $"Cannot conclude project in {project.Status} status");

        // 3. Gate: verify that all specs are Approved/Locked
        var specs = await (specRepo.FindAsync(
            s => s.ProjectId == request.ProjectId && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var pendingSpecs = specs.Where(
            s => s.Status != SpecStatus.Approved && s.Status != SpecStatus.Locked).ToList();
        if (pendingSpecs.Count > 0)
            throw new SddpException("CONCLUDE_PROJECT_FAILED",
                $"All specs must be Approved or Locked before concluding. {pendingSpecs.Count} spec(s) are still pending.");

        // 4. Transition state
        project.Conclude();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 5. Audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "conclude_project",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { Reason = request.Reason },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return await (_sender.Send(
            new Queries.GetProjectByIdQuery(request.TenantId, request.ProjectId),
            cancellationToken)).ConfigureAwait(false);
    }
}

// =============================================================================
// ReopenProjectCommand - Concluded -> Active
// =============================================================================

/// <summary>
/// Reopens a project (Concluded -> Active)
/// </summary>
public sealed record ReopenProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId,
    string? Reason) : ICommand<ProjectDetailDto?>, IAuditableRequest<ProjectDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDetailDto? response) => AuditLog;
}

public sealed class ReopenProjectCommandHandler : IRequestHandler<ReopenProjectCommand, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public ReopenProjectCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ProjectDetailDto?> Handle(ReopenProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();

        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new SddpException("REOPEN_PROJECT_FAILED", "Project not found");

        if (!project.Status.CanReopen())
            throw new SddpException("REOPEN_PROJECT_FAILED", $"Cannot reopen project in {project.Status} status");

        // Enforce active-project limit (max 5 per tenant)
        var activeProjects = await (projectRepo.FindAsync(
            p => p.TenantId == request.TenantId && p.Status == ProjectStatus.Active,
            cancellationToken)).ConfigureAwait(false);
        if (activeProjects.Count() >= BusinessLimits.MaxActiveProjectsPerTenant)
            throw new SddpException("ACTIVE_PROJECT_LIMIT_EXCEEDED",
                $"Tenant cannot have more than {BusinessLimits.MaxActiveProjectsPerTenant} active projects");

        project.Reopen();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "reopen_project",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { Reason = request.Reason },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return await (_sender.Send(
            new Queries.GetProjectByIdQuery(request.TenantId, request.ProjectId),
            cancellationToken)).ConfigureAwait(false);
    }
}

// =============================================================================
// ArchiveProjectCommand - Concluded -> Archived
// =============================================================================

/// <summary>
/// Archives a project (Concluded -> Archived)
/// </summary>
public sealed record ArchiveProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId) : ICommand<ProjectDetailDto?>, IAuditableRequest<ProjectDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDetailDto? response) => AuditLog;
}

public sealed class ArchiveProjectCommandHandler : IRequestHandler<ArchiveProjectCommand, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public ArchiveProjectCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ProjectDetailDto?> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();

        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new SddpException("ARCHIVE_PROJECT_FAILED", "Project not found");

        if (!project.Status.CanArchive())
            throw new SddpException("ARCHIVE_PROJECT_FAILED", $"Cannot archive project in {project.Status} status");

        project.Archive();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "archive_project",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return await (_sender.Send(
            new Queries.GetProjectByIdQuery(request.TenantId, request.ProjectId),
            cancellationToken)).ConfigureAwait(false);
    }
}
