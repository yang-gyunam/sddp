using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects.Queries;

/// <summary>
/// user project get
/// </summary>
public sealed record GetProjectsByUserQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    bool IsAdmin) : IQuery<IReadOnlyList<ProjectDto>>;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectsByUserQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var userRepo = _unitOfWork.Repository<User>();

        IEnumerable<Project> projects;
        if (request.IsAdmin)
        {
            projects = await (projectRepo.FindAsync(p => p.TenantId == request.TenantId, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var memberRepo = _unitOfWork.Repository<ProjectMember>();
            var memberships = await (memberRepo.FindAsync(
                m => m.UserId == request.UserId && m.IsActive, cancellationToken)).ConfigureAwait(false);
            var memberProjectIds = memberships.Select(m => m.ProjectId).ToHashSet();

            projects = await (projectRepo.FindAsync(
                p => p.TenantId == request.TenantId && memberProjectIds.Contains(p.Id), cancellationToken)).ConfigureAwait(false);
        }

        var ownerIds = projects
            .Where(p => p.OwnerId.HasValue)
            .Select(p => p.OwnerId!.Value)
            .Distinct()
            .ToList();

        var ownerMap = ownerIds.Count == 0
            ? new Dictionary<GlobalUniqueId, User>()
            : (await (userRepo.FindAsync(u => ownerIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false))
                .ToDictionary(u => u.Id, u => u);

        return projects
            .OrderBy(p => p.Code)
            .Select(p => ProjectMapping.MapToProjectDto(p, ownerMap))
            .ToList();
    }
}

/// <summary>
/// project get (ID)
/// </summary>
public sealed record GetProjectByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<ProjectDetailDto?>;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresenceTracker _presenceTracker;

    public GetProjectByIdQueryHandler(IUnitOfWork unitOfWork, IPresenceTracker presenceTracker)
    {
        _unitOfWork = unitOfWork;
        _presenceTracker = presenceTracker;
    }

    public async Task<ProjectDetailDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var userRepo = _unitOfWork.Repository<User>();

        var project = await (projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)).ConfigureAwait(false);
        if (project is null || project.TenantId != request.TenantId)
        {
            return null;
        }

        UserRefDto owner;
        if (project.OwnerId.HasValue)
        {
            var ownerUser = await (userRepo.GetByIdAsync(project.OwnerId.Value, cancellationToken)).ConfigureAwait(false);
            owner = UserRefHelper.ToUserRef(
                project.OwnerId.Value.ToString(),
                ownerUser?.DisplayName,
                ownerUser?.AvatarUrl);
        }
        else
        {
            owner = UserRefHelper.ToUserRef(string.Empty, null, null);
        }

        var statistics = await (BuildStatisticsAsync(request.TenantId, request.ProjectId, cancellationToken)).ConfigureAwait(false);
        var onlineUserIds = new HashSet<string>(_presenceTracker.GetOnlineUsers());
        var members = await (BuildMembersAsync(request.ProjectId, onlineUserIds, cancellationToken)).ConfigureAwait(false);

        return new ProjectDetailDto(
            Id: project.Id.ToString(),
            TenantId: project.TenantId.ToString(),
            Code: project.Code,
            Name: project.Name,
            Description: project.Description,
            Owner: owner,
            Status: project.Status.ToString().ToLowerInvariant(),
            CreatedAt: project.CreatedAt.ToIso8601(),
            UpdatedAt: project.UpdatedAt.ToIso8601(),
            Statistics: statistics,
            Members: members);
    }

    private async Task<ProjectStatisticsDto> BuildStatisticsAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var reqRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();
        var taskRepo = _unitOfWork.Repository<TaskItem>();
        var glossaryRepo = _unitOfWork.Repository<GlossaryTerm>();
        var worklogRepo = _unitOfWork.Repository<Worklog>();

        var allSpecs = await (specRepo.FindAsync(
            s => s.TenantId == tenantId && s.ProjectId == projectId && s.IsActive && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var allReqs = await (reqRepo.FindAsync(
            r => r.TenantId == tenantId && r.ProjectId == projectId && r.IsActive && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);
        var allConversations = await (conversationRepo.FindAsync(
            d => d.TenantId == tenantId && d.ProjectId == projectId && d.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var allArtifacts = await (artifactRepo.FindAsync(
            a => a.TenantId == tenantId && a.ProjectId == projectId && a.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var allTasks = await (taskRepo.FindAsync(
            t => t.TenantId == tenantId && t.ProjectId == projectId && t.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var allGlossary = await (glossaryRepo.FindAsync(
            g => g.TenantId == tenantId && g.ProjectId == projectId && g.IsActive && g.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var recentArtifacts = allArtifacts.Count(a => a.CreatedAt.ToDateTimeOffset() >= weekAgo);

        // Effort: total hours logged this week + active contributors
        var allWorklogs = await (worklogRepo.FindAsync(
            w => w.TenantId == tenantId && w.ProjectId == projectId && w.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var weekWorklogs = allWorklogs.Where(w => w.CreatedAt.ToDateTimeOffset() >= weekAgo).ToList();
        var totalHoursThisWeek = (int)weekWorklogs.Sum(w => w.SpentHours);
        var activeContributors = weekWorklogs.Select(w => w.UserId).Distinct().Count();

        return new ProjectStatisticsDto(
            Conversations: new StatPairDto(
                allConversations.Count(),
                allConversations.Count(d => !d.IsArchived)),
            Requirements: new StatPairDto(
                allReqs.Count(),
                allReqs.Count(r => r.Status == Abstractions.Enums.RequirementStatus.Draft)),
            Specs: new StatPairDto(
                allSpecs.Count(),
                allSpecs.Count(s => s.Status == Abstractions.Enums.SpecStatus.InReview)),
            Artifacts: new StatPairDto(allArtifacts.Count(), recentArtifacts),
            Tasks: new StatPairDto(
                allTasks.Count(),
                allTasks.Count(t => t.Status == Abstractions.Enums.TaskItemStatus.InProgress)),
            Glossary: new StatPairDto(
                allGlossary.Count(),
                allGlossary.Count(g => g.Status == Abstractions.Enums.GlossaryTermStatus.Draft)),
            Effort: new StatPairDto(totalHoursThisWeek, activeContributors));
    }

    private async Task<IReadOnlyList<ProjectMemberDto>> BuildMembersAsync(
        GlobalUniqueId projectId,
        HashSet<string> onlineUserIds,
        CancellationToken cancellationToken)
    {
        var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();
        var userRepo = _unitOfWork.Repository<User>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var roleRepo = _unitOfWork.Repository<Role>();

        var projectMembers = (await (projectMemberRepo.FindAsync(
                pm => pm.ProjectId == projectId && pm.IsActive,
                cancellationToken)).ConfigureAwait(false))
            // Defensive dedupe: keep the latest active row per user when legacy duplicate data exists.
            .Where(pm => pm.IsActive)
            .OrderByDescending(pm => pm.UpdatedAt.ToDateTimeOffset())
            .ThenByDescending(pm => pm.Id)
            .GroupBy(pm => pm.UserId)
            .Select(group => group.First())
            .ToList();

        var memberUserIds = projectMembers.Select(pm => pm.UserId).Distinct().ToList();
        var memberRoleIds = projectMembers.Select(pm => pm.UserRoleId).Distinct().ToList();

        var membersMap = memberUserIds.Count == 0
            ? new Dictionary<GlobalUniqueId, User>()
            : (await (userRepo.FindAsync(u => memberUserIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false))
                .ToDictionary(u => u.Id, u => u);

        var userRoles = memberRoleIds.Count == 0
            ? new List<UserRole>()
            : (await (userRoleRepo.FindAsync(ur => memberRoleIds.Contains(ur.Id), cancellationToken)).ConfigureAwait(false)).ToList();

        var userRoleMap = userRoles.ToDictionary(ur => ur.Id, ur => ur);

        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var roleMap = roles.ToDictionary(r => r.Id, r => r);

        return projectMembers.Select(pm =>
        {
            membersMap.TryGetValue(pm.UserId, out var memberUser);
            var displayName = memberUser?.DisplayName ?? memberUser?.Username ?? "Unknown";
            var avatarUrl = memberUser?.AvatarUrl;
            var lastActivity = memberUser?.LastLoginAt?.ToIso8601();

            var roleName = "Developer";
            if (userRoleMap.TryGetValue(pm.UserRoleId, out var userRole) &&
                roleMap.TryGetValue(userRole.RoleId, out var role))
            {
                roleName = role.Type.ToString();
            }

            return new ProjectMemberDto(
                UserId: pm.UserId.ToString(),
                PersonId: pm.UserId.ToString(),
                DisplayName: displayName,
                Role: roleName,
                AvatarUrl: avatarUrl,
                LastActivityAt: lastActivity,
                IsOnline: onlineUserIds.Contains(pm.UserId.ToString()));
        }).ToList();
    }
}
