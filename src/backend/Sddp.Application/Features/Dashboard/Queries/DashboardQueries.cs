using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Dashboard.Queries;

/// <summary>
/// Get personal dashboard
/// </summary>
public sealed record GetMyDashboardQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<MyDashboardDto>;

public sealed class GetMyDashboardQueryHandler : IRequestHandler<GetMyDashboardQuery, MyDashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public GetMyDashboardQueryHandler(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<MyDashboardDto> Handle(GetMyDashboardQuery request, CancellationToken cancellationToken)
    {
        var overview = await (DashboardHelpers.BuildOverviewAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken)).ConfigureAwait(false);
        var activities = await (_auditLogService.GetByActorAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        var filtered = activities
            .Where(a => !a.ResourceType.Equals("auth", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        return new MyDashboardDto(
            Tasks: overview.Tasks,
            Conversations: overview.Conversations,
            Specs: overview.Specs,
            Requirements: overview.Requirements,
            Glossary: overview.Glossary,
            Artifacts: overview.Artifacts,
            RecentActivities: filtered);
    }
}

/// <summary>
/// system dashboard get
/// </summary>
public sealed record GetSystemDashboardQuery(
    GlobalUniqueId TenantId) : IQuery<SystemDashboardDto>;

public sealed class GetSystemDashboardQueryHandler : IRequestHandler<GetSystemDashboardQuery, SystemDashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemDashboardDto> Handle(GetSystemDashboardQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var reqRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var userRepo = _unitOfWork.Repository<User>();
        var auditRepo = _unitOfWork.Repository<AuditLog>();

        var weekAgo = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-7));

        var totalSpecs = await (specRepo.CountAsync(
            s => s.TenantId == request.TenantId && s.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var specsThisWeek = await (specRepo.CountAsync(
            s => s.TenantId == request.TenantId && s.ValidTo == null && s.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalReqs = await (reqRepo.CountAsync(
            r => r.TenantId == request.TenantId && r.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var reqsThisWeek = await (reqRepo.CountAsync(
            r => r.TenantId == request.TenantId && r.ValidTo == null && r.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalConversations = await (conversationRepo.CountAsync(
            d => d.TenantId == request.TenantId, cancellationToken)).ConfigureAwait(false);
        var conversationsThisWeek = await (conversationRepo.CountAsync(
            d => d.TenantId == request.TenantId && d.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalUsers = await (userRepo.CountAsync(cancellationToken)).ConfigureAwait(false);

        var (recentLogs, _) = await (auditRepo.FindPagedAsync(
            l => !l.ResourceType.Equals("auth"),
            page: 1, pageSize: 10,
            orderBy: l => l.CreatedAt, descending: true,
            cancellationToken)).ConfigureAwait(false);
        var recentEntries = recentLogs.Select(DashboardHelpers.MapAuditLogEntry).ToList();

        return new SystemDashboardDto(
            Specs: new DashboardStatDto(totalSpecs, specsThisWeek),
            Requirements: new DashboardStatDto(totalReqs, reqsThisWeek),
            Conversations: new DashboardStatDto(totalConversations, conversationsThisWeek),
            Users: new DashboardStatDto(totalUsers, 0),
            RecentActivities: recentEntries);
    }
}

/// <summary>
/// personal overview get (user project aggregated)
/// </summary>
public sealed record GetMyOverviewQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<MyOverviewDto>;

public sealed class GetMyOverviewQueryHandler : IRequestHandler<GetMyOverviewQuery, MyOverviewDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<MyOverviewDto> Handle(GetMyOverviewQuery request, CancellationToken cancellationToken)
    {
        return DashboardHelpers.BuildOverviewAsync(_unitOfWork, request.TenantId, request.UserId, cancellationToken);
    }
}

/// <summary>
/// task get
/// </summary>
public sealed record GetMyTasksQuery : IQuery<MyTasksDto>;

public sealed class GetMyTasksQueryHandler : IRequestHandler<GetMyTasksQuery, MyTasksDto>
{
    public Task<MyTasksDto> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(DashboardHelpers.BuildEmptyTasks());
    }
}

/// <summary>
/// get ()
/// </summary>
public sealed record GetMyActivitiesQuery(
    GlobalUniqueId UserId,
    int Page,
    int PageSize) : IQuery<MyActivitiesDto>;

public sealed class GetMyActivitiesQueryHandler : IRequestHandler<GetMyActivitiesQuery, MyActivitiesDto>
{
    private readonly IAuditLogService _auditLogService;

    public GetMyActivitiesQueryHandler(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<MyActivitiesDto> Handle(GetMyActivitiesQuery request, CancellationToken cancellationToken)
    {
        // IAuditLogService in-memory
        // TODO: IAuditLogService DB
        var activities = await (_auditLogService.GetByActorAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        var filtered = activities
            .Where(a => !a.ResourceType.Equals("auth", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalCount = filtered.Count;
        var pagedActivities = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new MyActivitiesDto(pagedActivities, request.Page, request.PageSize, totalCount);
    }
}

// GetMyNotificationsQuery moved to Features/Notifications/NotificationQueries.cs

/// <summary>
/// system get
/// </summary>
public sealed record GetSystemStatsQuery(
    GlobalUniqueId TenantId) : IQuery<SystemStatsDto>;

public sealed class GetSystemStatsQueryHandler : IRequestHandler<GetSystemStatsQuery, SystemStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemStatsDto> Handle(GetSystemStatsQuery request, CancellationToken cancellationToken)
    {
        var specRepo = _unitOfWork.Repository<Spec>();
        var reqRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var userRepo = _unitOfWork.Repository<User>();

        var weekAgo = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-7));

        var totalSpecs = await (specRepo.CountAsync(
            s => s.TenantId == request.TenantId && s.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var specsThisWeek = await (specRepo.CountAsync(
            s => s.TenantId == request.TenantId && s.ValidTo == null && s.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalReqs = await (reqRepo.CountAsync(
            r => r.TenantId == request.TenantId && r.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var reqsThisWeek = await (reqRepo.CountAsync(
            r => r.TenantId == request.TenantId && r.ValidTo == null && r.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalConversations = await (conversationRepo.CountAsync(
            d => d.TenantId == request.TenantId, cancellationToken)).ConfigureAwait(false);
        var conversationsThisWeek = await (conversationRepo.CountAsync(
            d => d.TenantId == request.TenantId && d.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalUsers = await (userRepo.CountAsync(cancellationToken)).ConfigureAwait(false);

        return new SystemStatsDto(
            TotalProjects: 0,
            TotalUsers: totalUsers,
            TotalSpecs: totalSpecs,
            TotalRequirements: totalReqs,
            TotalConversations: totalConversations,
            SpecsThisWeek: specsThisWeek,
            RequirementsThisWeek: reqsThisWeek,
            ConversationsThisWeek: conversationsThisWeek);
    }
}

/// <summary>
/// system audit log get ()
/// </summary>
public sealed record GetSystemAuditLogsQuery(
    int Page,
    int PageSize) : IQuery<AuditLogsDto>;

public sealed class GetSystemAuditLogsQueryHandler : IRequestHandler<GetSystemAuditLogsQuery, AuditLogsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemAuditLogsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuditLogsDto> Handle(GetSystemAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var auditRepo = _unitOfWork.Repository<AuditLog>();

        var (pagedLogs, totalCount) = await (auditRepo.FindPagedAsync(
            l => l.ResourceType != "auth",
            request.Page, request.PageSize,
            q => q.OrderByDescending(l => l.CreatedAt),
            cancellationToken)).ConfigureAwait(false);

        var items = pagedLogs
            .Select(DashboardHelpers.MapAuditLogEntry)
            .ToList();

        return new AuditLogsDto(items, request.Page, request.PageSize, totalCount);
    }
}

/// <summary>
/// system get
/// </summary>
public sealed record GetSystemHealthQuery : IQuery<HealthCheckDto>;

public sealed class GetSystemHealthQueryHandler : IRequestHandler<GetSystemHealthQuery, HealthCheckDto>
{
    public Task<HealthCheckDto> Handle(GetSystemHealthQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(DashboardHelpers.BuildDefaultHealth());
    }
}

/// <summary>
/// project dashboard get
/// </summary>
public sealed record GetProjectDashboardQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId) : IQuery<ProjectDashboardDto?>;

public sealed class GetProjectDashboardQueryHandler : IRequestHandler<GetProjectDashboardQuery, ProjectDashboardDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectDashboardDto?> Handle(GetProjectDashboardQuery request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var userRepo = _unitOfWork.Repository<User>();

        var project = await (projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)).ConfigureAwait(false);
        if (project is null || project.TenantId != request.TenantId)
        {
            return null;
        }

        // Project info
        var ownerName = string.Empty;
        if (project.OwnerId.HasValue)
        {
            var owner = await (userRepo.GetByIdAsync(project.OwnerId.Value, cancellationToken)).ConfigureAwait(false);
            ownerName = owner?.DisplayName ?? string.Empty;
        }

        // Member count
        var memberRepo = _unitOfWork.Repository<ProjectMember>();
        var members = await (memberRepo.FindAsync(
            pm => pm.ProjectId == request.ProjectId && pm.IsActive, cancellationToken)).ConfigureAwait(false);
        var memberCount = members.Count();

        // Statistics — DB-level COUNT queries
        var specRepo = _unitOfWork.Repository<Spec>();
        var reqRepo = _unitOfWork.Repository<Requirement>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var artifactRepo = _unitOfWork.Repository<ArtifactTracking>();
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var tid = request.TenantId;
        var pid = request.ProjectId;
        var weekAgo = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-7));

        var totalSpecs = await (specRepo.CountAsync(
            s => s.TenantId == tid && s.ProjectId == pid && s.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var specsInReview = await (specRepo.CountAsync(
            s => s.TenantId == tid && s.ProjectId == pid && s.ValidTo == null && s.Status == SpecStatus.InReview, cancellationToken)).ConfigureAwait(false);

        var totalReqs = await (reqRepo.CountAsync(
            r => r.TenantId == tid && r.ProjectId == pid && r.ValidTo == null, cancellationToken)).ConfigureAwait(false);
        var draftReqs = await (reqRepo.CountAsync(
            r => r.TenantId == tid && r.ProjectId == pid && r.ValidTo == null && r.Status == RequirementStatus.Draft, cancellationToken)).ConfigureAwait(false);

        var totalConversations = await (conversationRepo.CountAsync(
            d => d.TenantId == tid && d.ProjectId == pid, cancellationToken)).ConfigureAwait(false);
        var activeConversations = await (conversationRepo.CountAsync(
            d => d.TenantId == tid && d.ProjectId == pid && !d.IsArchived, cancellationToken)).ConfigureAwait(false);

        var totalArtifacts = await (artifactRepo.CountAsync(
            a => a.TenantId == tid && a.ProjectId == pid, cancellationToken)).ConfigureAwait(false);
        var recentArtifacts = await (artifactRepo.CountAsync(
            a => a.TenantId == tid && a.ProjectId == pid && a.CreatedAt >= weekAgo, cancellationToken)).ConfigureAwait(false);

        var totalTasks = await (taskRepo.CountAsync(
            t => t.TenantId == tid && t.ProjectId == pid, cancellationToken)).ConfigureAwait(false);
        var tasksInProgress = await (taskRepo.CountAsync(
            t => t.TenantId == tid && t.ProjectId == pid && t.Status == TaskItemStatus.InProgress, cancellationToken)).ConfigureAwait(false);
        var tasksToDo = await (taskRepo.CountAsync(
            t => t.TenantId == tid && t.ProjectId == pid && t.Status == TaskItemStatus.ToDo, cancellationToken)).ConfigureAwait(false);
        var tasksDone = await (taskRepo.CountAsync(
            t => t.TenantId == tid && t.ProjectId == pid && t.Status == TaskItemStatus.Done, cancellationToken)).ConfigureAwait(false);

        var statistics = new ProjectDashboardStatsDto(
            Conversations: new DashboardStatDto(totalConversations, activeConversations),
            Requirements: new DashboardStatDto(totalReqs, draftReqs),
            Specs: new DashboardStatDto(totalSpecs, specsInReview),
            Tasks: new DashboardStatDto(totalTasks, tasksInProgress),
            Artifacts: new DashboardStatDto(totalArtifacts, recentArtifacts));

        // Task progress
        var taskProgress = new TaskProgressDto(
            ToDo: tasksToDo,
            InProgress: tasksInProgress,
            Done: tasksDone,
            Total: totalTasks);

        // Team members with activity counts
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var roleRepo = _unitOfWork.Repository<Role>();

        var memberUserIds = members.Select(pm => pm.UserId).Distinct().ToList();
        var memberRoleIds = members.Select(pm => pm.UserRoleId).Distinct().ToList();

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

        // Audit logs for activity counts per member
        var auditRepo = _unitOfWork.Repository<AuditLog>();
        var projectLogs = await (auditRepo.FindAsync(
            l => l.ProjectId == request.ProjectId && l.IsActive, cancellationToken)).ConfigureAwait(false);

        var activityByUser = projectLogs
            .Where(l => l.ActorId.HasValue)
            .GroupBy(l => l.ActorId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var teamMembers = members.Select(pm =>
        {
            membersMap.TryGetValue(pm.UserId, out var memberUser);
            var displayName = memberUser?.DisplayName ?? memberUser?.Username ?? "Unknown";

            var roleName = "Developer";
            if (userRoleMap.TryGetValue(pm.UserRoleId, out var userRole) &&
                roleMap.TryGetValue(userRole.RoleId, out var role))
            {
                roleName = role.Type.ToString();
            }

            activityByUser.TryGetValue(pm.UserId, out var activityCount);

            return new TeamMemberActivityDto(
                UserId: pm.UserId.ToString(),
                UserName: displayName,
                Role: roleName,
                ActivityCount: activityCount);
        }).ToList();

        // Recent activities
        var recentActivities = projectLogs
            .Where(l => !l.ResourceType.Equals("auth", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(DashboardHelpers.MapAuditLogEntry)
            .ToList();

        // Last activity time
        var lastActivityLog = projectLogs
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefault();
        var lastActivityAt = lastActivityLog?.CreatedAt.ToIso8601();

        var projectInfo = new ProjectInfoDto(
            Id: project.Id.ToString(),
            Name: project.Name,
            OwnerId: project.OwnerId?.ToString() ?? string.Empty,
            OwnerName: ownerName,
            Status: project.Status.ToString().ToLowerInvariant(),
            MemberCount: memberCount,
            CreatedAt: project.CreatedAt.ToIso8601(),
            LastActivityAt: lastActivityAt);

        return new ProjectDashboardDto(
            Project: projectInfo,
            Statistics: statistics,
            TeamMembers: teamMembers,
            RecentActivities: recentActivities,
            TaskProgress: taskProgress);
    }
}
