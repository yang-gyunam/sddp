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

/// <summary>
/// Creates a project
/// </summary>
public sealed record CreateProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId RequestUserId,
    CreateProjectDto Dto) : ICommand<ProjectDto?>, IAuditableRequest<ProjectDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDto? response) => AuditLog;
}

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDto?> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var userRepo = _unitOfWork.Repository<User>();

        var dto = request.Dto;

        // 1. Check uniqueness of (tenantId, code)
        var existing = await (projectRepo.FindAsync(
            p => p.TenantId == request.TenantId && p.Code == dto.Code,
            cancellationToken)).ConfigureAwait(false);
        if (existing.Any())
        {
            throw new ConflictException($"Project code '{dto.Code}' already exists in this tenant");
        }

        // 2. Create the project (starts in Planning)
        var project = new Project(
            request.TenantId,
            dto.Code,
            dto.Name,
            dto.Description,
            request.RequestUserId,
            string.Empty,
            null,
            string.Empty,
            60);

        await (projectRepo.AddAsync(project, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 3. Load owner info and map the DTO
        var owner = await (userRepo.GetByIdAsync(request.RequestUserId, cancellationToken)).ConfigureAwait(false);
        var ownerMap = owner is not null
            ? new Dictionary<GlobalUniqueId, User> { [owner.Id] = owner }
            : new Dictionary<GlobalUniqueId, User>();

        var result = ProjectMapping.MapToProjectDto(project, ownerMap);

        // 4. Audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "create",
            ResourceType: "project",
            ResourceId: project.Id,
            Payload: new { project.Code, project.Name, project.Description },
            TenantId: request.TenantId,
            ProjectId: project.Id);

        return result;
    }
}

/// <summary>
/// Updates a project
/// </summary>
public sealed record UpdateProjectCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RequestUserId,
    UpdateProjectDto Dto) : ICommand<ProjectDetailDto?>, IAuditableRequest<ProjectDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectDetailDto? response) => AuditLog;
}

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public UpdateProjectCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ProjectDetailDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();

        // 1. Load the project and verify tenant ownership
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault();
        if (project is null)
        {
            return null;
        }

        // 2. Update the project (status changes use dedicated lifecycle commands)
        project.UpdateDetails(
            request.Dto.Name,
            request.Dto.Description,
            project.OwnerId);

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 3. Return ProjectDetailDto (reuses GetProjectByIdQuery)
        var detail = await (_sender.Send(
            new Sddp.Application.Features.Projects.Queries.GetProjectByIdQuery(request.TenantId, request.ProjectId),
            cancellationToken)).ConfigureAwait(false);

        // 4. Audit log
        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "update",
            ResourceType: "project",
            ResourceId: request.ProjectId,
            Payload: new { request.Dto.Name, request.Dto.Description },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return detail;
    }
}

/// <summary>
/// Deactivates a project member
/// </summary>
public sealed record DeactivateProjectMemberCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TargetUserId,
    GlobalUniqueId RequestUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class DeactivateProjectMemberCommandHandler : IRequestHandler<DeactivateProjectMemberCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProjectMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var memberRepo = _unitOfWork.Repository<ProjectMember>();
        var projectRepo = _unitOfWork.Repository<Project>();

        // 1. Verify project exists and belongs to tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new NotFoundException("Project", (Guid)request.ProjectId);

        // 2. Prevent deactivating project owner
        if (project.OwnerId.HasValue && request.TargetUserId == project.OwnerId.Value)
        {
            throw new ValidationException("Cannot deactivate the project owner");
        }

        // 3. Find the member by userId + projectId
        var members = await (memberRepo.FindAsync(
            m => m.UserId == request.TargetUserId && m.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);
        var member = members.FirstOrDefault()
            ?? throw new NotFoundException("ProjectMember", (Guid)request.TargetUserId);

        // 4. Deactivate (soft delete)
        member.Deactivate();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 5. Audit log
        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "deactivate_member",
            "project_member",
            member.Id,
            new { ProjectId = request.ProjectId, UserId = request.TargetUserId },
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// Removes a project member permanently
/// </summary>
public sealed record RemoveProjectMemberCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TargetUserId,
    GlobalUniqueId RequestUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveProjectMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var memberRepo = _unitOfWork.Repository<ProjectMember>();
        var projectRepo = _unitOfWork.Repository<Project>();

        // 1. Verify project exists and belongs to tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new NotFoundException("Project", (Guid)request.ProjectId);

        // 2. Prevent removing project owner
        if (project.OwnerId.HasValue && request.TargetUserId == project.OwnerId.Value)
        {
            throw new ValidationException("Cannot remove the project owner");
        }

        // 3. Find the member (including inactive — may have been deactivated first)
        var members = await (memberRepo.FindIncludingInactiveAsync(
            m => m.UserId == request.TargetUserId && m.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);
        var member = members.FirstOrDefault()
            ?? throw new NotFoundException("ProjectMember", (Guid)request.TargetUserId);

        // 4. Hard delete
        await (memberRepo.DeleteAsync(member, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 5. Audit log
        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "remove_member",
            "project_member",
            member.Id,
            new { ProjectId = request.ProjectId, UserId = request.TargetUserId },
            request.TenantId,
            request.ProjectId);

        return true;
    }
}

/// <summary>
/// Adds a project member
/// </summary>
public sealed record AddProjectMemberCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId TargetUserId,
    string RoleName,
    GlobalUniqueId RequestUserId) : ICommand<ProjectMemberDto>, IAuditableRequest<ProjectMemberDto>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ProjectMemberDto response) => AuditLog;
}

public sealed class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, ProjectMemberDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddProjectMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectMemberDto> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var memberRepo = _unitOfWork.Repository<ProjectMember>();
        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();

        // 1. Verify that the project exists and belongs to the tenant
        var projects = await (projectRepo.FindAsync(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)).ConfigureAwait(false);
        var project = projects.FirstOrDefault()
            ?? throw new NotFoundException("Project", (Guid)request.ProjectId);

        // 2. Verify that the project is editable (Planning/Active)
        if (!project.Status.IsEditable())
            throw new SddpException("ADD_MEMBER_FAILED",
                $"Cannot add members to a project in {project.Status} status");

        // 3. Load the current member state (both active and inactive)
        var existingMembers = await (memberRepo.FindIncludingInactiveAsync(
            m => m.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);

        // Treat the owner as an implicit participant.
        if (project.OwnerId.HasValue && request.TargetUserId == project.OwnerId.Value)
            throw new ConflictException("Project owner is already a participant");

        var activeMembers = existingMembers.Where(m => m.IsActive).ToList();
        if (activeMembers.Any(m => m.UserId == request.TargetUserId))
            throw new ConflictException("User is already a member of this project");

        // 4. Enforce the member limit (max 10 including the owner)
        var ownerIncluded = project.OwnerId.HasValue;
        var ownerId = project.OwnerId;
        var ownerAlreadyInMemberRows = ownerId.HasValue && activeMembers.Any(m => m.UserId == ownerId.Value);
        var currentParticipantCount = activeMembers.Count + (ownerIncluded && !ownerAlreadyInMemberRows ? 1 : 0);

        if (currentParticipantCount >= BusinessLimits.MaxProjectMembers)
            throw new SddpException("PROJECT_MEMBER_LIMIT_EXCEEDED",
                $"Project cannot have more than {BusinessLimits.MaxProjectMembers} members");

        var existingInactiveMember = existingMembers
            .FirstOrDefault(m => m.UserId == request.TargetUserId && !m.IsActive);

        // 5. Verify that the target user exists
        var targetUser = await (userRepo.GetByIdAsync(request.TargetUserId, cancellationToken))
.ConfigureAwait(false)            ?? throw new NotFoundException("User", (Guid)request.TargetUserId);

        // 6. Parse the role and block system roles
        if (!Enum.TryParse<RoleType>(request.RoleName, out var roleType))
            throw new SddpException("ADD_MEMBER_FAILED",
                $"Invalid role: {request.RoleName}");

        if (roleType is RoleType.Admin)
            throw new SddpException("ADD_MEMBER_FAILED",
                "Cannot assign the Admin system role as a project member role");

        var roles = await (roleRepo.FindAsync(r => r.Type == roleType, cancellationToken)).ConfigureAwait(false);
        var role = roles.FirstOrDefault()
            ?? throw new SddpException("ADD_MEMBER_FAILED", $"Role '{request.RoleName}' not found");

        // 7. Find or create the UserRole (including inactive entries)
        var existingUserRoles = await (userRoleRepo.FindIncludingInactiveAsync(
            ur => ur.UserId == request.TargetUserId
                && ur.RoleId == role.Id
                && ur.ProjectId == request.ProjectId,
            cancellationToken)).ConfigureAwait(false);
        var userRole = existingUserRoles.FirstOrDefault();

        if (userRole is null)
        {
            userRole = new UserRole(
                request.TargetUserId,
                role.Id,
                request.RequestUserId,
                request.TenantId,
                request.ProjectId);
            await (userRoleRepo.AddAsync(userRole, cancellationToken)).ConfigureAwait(false);
        }
        else if (!userRole.IsActive)
        {
            userRole.Activate();
        }

        // 8. Create or reactivate the ProjectMember
        var wasReactivated = false;
        ProjectMember member;
        if (existingInactiveMember is not null)
        {
            existingInactiveMember.Activate();
            existingInactiveMember.ChangeRole(userRole.Id);
            member = existingInactiveMember;
            wasReactivated = true;
        }
        else
        {
            member = new ProjectMember(request.ProjectId, request.TargetUserId, userRole.Id);
            await (memberRepo.AddAsync(member, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 9. Audit log
        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "add_member",
            "project_member",
            member.Id,
            new { ProjectId = request.ProjectId, UserId = request.TargetUserId, Role = request.RoleName, Reactivated = wasReactivated },
            request.TenantId,
            request.ProjectId);

        // 10. Return ProjectMemberDto
        return new ProjectMemberDto(
            UserId: targetUser.Id.ToString(),
            PersonId: targetUser.PersonId.ToString(),
            DisplayName: targetUser.DisplayName,
            Role: roleType.ToString(),
            AvatarUrl: targetUser.AvatarUrl,
            LastActivityAt: targetUser.LastLoginAt?.ToIso8601(),
            IsOnline: false);
    }
}
