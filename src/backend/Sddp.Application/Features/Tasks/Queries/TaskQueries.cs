using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Tasks.Queries;

/// <summary>
/// task get ()
/// </summary>
public sealed record GetTasksQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId? AssigneeId,
    int Page,
    int PageSize,
    TaskItemStatus? Status,
    TaskItemPriority? Priority,
    GlobalUniqueId? CategoryId = null) : IQuery<TaskItemPageDto>;

public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, TaskItemPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTasksQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskItemPageDto> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var (pagedTasks, totalCount) = await (taskRepo.FindPagedAsync(
            t => t.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || t.ProjectId == request.ProjectId.Value)
                && (!request.AssigneeId.HasValue || t.AssigneeId == request.AssigneeId.Value || (t.CreatorId == request.AssigneeId.Value && !t.ProjectId.HasValue))
                && (request.Status == null || t.Status == request.Status)
                && (request.Priority == null || t.Priority == request.Priority)
                && (!request.CategoryId.HasValue || t.CategoryId == request.CategoryId.Value),
            request.Page, request.PageSize,
            orderBy: t => t.Priority,
            descending: true,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var userRepo = _unitOfWork.Repository<User>();
        var items = new List<TaskItemDto>();

        foreach (var task in pagedTasks)
        {
            var assignee = await (UserRefHelper.ToUserRefAsync(userRepo, task.AssigneeId, cancellationToken)).ConfigureAwait(false);
            var linkedItemCount = task.LinkedItems?.Count ?? 0;

            items.Add(TaskMapping.MapToDto(task, assignee, linkedItemCount));
        }

        return new TaskItemPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}

/// <summary>
/// task get (ID)
/// </summary>
public sealed record GetTaskByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId TaskId) : IQuery<TaskItemDetailDto?>;

public sealed class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskItemDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTaskByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskItemDetailDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var task = await (taskRepo.GetByIdAsync(request.TaskId, cancellationToken)).ConfigureAwait(false);
        if (task is null || task.TenantId != request.TenantId || !task.IsActive)
        {
            return null;
        }

        return await (TaskMapping.MapToDetailDtoAsync(task, _unitOfWork, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// task get
/// </summary>
public sealed record GetMyTaskStatsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<MyTaskStatsDto>;

public sealed class GetMyTaskStatsQueryHandler : IRequestHandler<GetMyTaskStatsQuery, MyTaskStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyTaskStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MyTaskStatsDto> Handle(GetMyTaskStatsQuery request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        var toDoCount = await (taskRepo.CountAsync(
            t => t.TenantId == request.TenantId && (t.AssigneeId == request.UserId || (t.CreatorId == request.UserId && !t.ProjectId.HasValue)) && t.IsActive && t.Status == TaskItemStatus.ToDo,
            cancellationToken)).ConfigureAwait(false);

        var inProgressCount = await (taskRepo.CountAsync(
            t => t.TenantId == request.TenantId && (t.AssigneeId == request.UserId || (t.CreatorId == request.UserId && !t.ProjectId.HasValue)) && t.IsActive && t.Status == TaskItemStatus.InProgress,
            cancellationToken)).ConfigureAwait(false);

        var doneCount = await (taskRepo.CountAsync(
            t => t.TenantId == request.TenantId && (t.AssigneeId == request.UserId || (t.CreatorId == request.UserId && !t.ProjectId.HasValue)) && t.IsActive && t.Status == TaskItemStatus.Done,
            cancellationToken)).ConfigureAwait(false);

        var blockedCount = await (taskRepo.CountAsync(
            t => t.TenantId == request.TenantId && (t.AssigneeId == request.UserId || (t.CreatorId == request.UserId && !t.ProjectId.HasValue)) && t.IsActive && t.Status == TaskItemStatus.Blocked,
            cancellationToken)).ConfigureAwait(false);

        return new MyTaskStatsDto(
            ToDoCount: toDoCount,
            InProgressCount: inProgressCount,
            DoneCount: doneCount,
            BlockedCount: blockedCount,
            TotalCount: toDoCount + inProgressCount + doneCount + blockedCount);
    }
}

/// <summary>
/// task search
/// </summary>
public sealed record SearchTasksQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    string Query,
    int Limit = 15) : IQuery<IEnumerable<TaskSearchResultDto>>;

public sealed class SearchTasksQueryHandler : IRequestHandler<SearchTasksQuery, IEnumerable<TaskSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchTasksQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TaskSearchResultDto>> Handle(SearchTasksQuery request, CancellationToken cancellationToken)
    {
        var taskRepo = _unitOfWork.Repository<TaskItem>();
        var queryLower = request.Query.ToLower();

        var matches = await (taskRepo.FindAsync(
            t => t.TenantId == request.TenantId
                && (!request.ProjectId.HasValue || t.ProjectId == request.ProjectId.Value)
                && t.IsActive
                && t.Title.ToLower().Contains(queryLower),
            cancellationToken)).ConfigureAwait(false);

        return matches
            .OrderByDescending(t => t.CreatedAt.ToDateTimeOffset())
            .Take(request.Limit)
            .Select(t => new TaskSearchResultDto(
                Id: t.Id.ToString(),
                Title: t.Title,
                Status: t.Status.ToString()));
    }
}

/// <summary>
/// log get
/// </summary>
public sealed record GetBacklogSummaryQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<BacklogSummaryDto>;

public sealed class GetBacklogSummaryQueryHandler : IRequestHandler<GetBacklogSummaryQuery, BacklogSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBacklogSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BacklogSummaryDto> Handle(GetBacklogSummaryQuery request, CancellationToken cancellationToken)
    {
        var memberRepo = _unitOfWork.Repository<ProjectMember>();
        var projectRepo = _unitOfWork.Repository<Project>();
        var taskRepo = _unitOfWork.Repository<TaskItem>();

        // Find all projects where user is a member
        var memberships = await (memberRepo.FindAsync(
            m => m.UserId == request.UserId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var projectSummaries = new List<BacklogProjectSummaryDto>();
        var totalTasks = 0;

        foreach (var membership in memberships)
        {
            var project = await (projectRepo.GetByIdAsync(membership.ProjectId, cancellationToken)).ConfigureAwait(false);
            if (project is null || project.TenantId != request.TenantId) continue;

            var isOwner = project.OwnerId.HasValue && project.OwnerId.Value == request.UserId;

            // Count tasks: owner sees all, member sees own
            int activeCount;
            int totalCount;

            if (isOwner)
            {
                activeCount = await (taskRepo.CountAsync(
                    t => t.TenantId == request.TenantId && t.ProjectId == project.Id && t.IsActive
                        && (t.Status == TaskItemStatus.Backlog || t.Status == TaskItemStatus.ToDo || t.Status == TaskItemStatus.InProgress),
                    cancellationToken)).ConfigureAwait(false);
                totalCount = await (taskRepo.CountAsync(
                    t => t.TenantId == request.TenantId && t.ProjectId == project.Id && t.IsActive,
                    cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                activeCount = await (taskRepo.CountAsync(
                    t => t.TenantId == request.TenantId && t.ProjectId == project.Id && t.IsActive
                        && (t.AssigneeId == request.UserId || t.CreatorId == request.UserId)
                        && (t.Status == TaskItemStatus.Backlog || t.Status == TaskItemStatus.ToDo || t.Status == TaskItemStatus.InProgress),
                    cancellationToken)).ConfigureAwait(false);
                totalCount = await (taskRepo.CountAsync(
                    t => t.TenantId == request.TenantId && t.ProjectId == project.Id && t.IsActive
                        && (t.AssigneeId == request.UserId || t.CreatorId == request.UserId),
                    cancellationToken)).ConfigureAwait(false);
            }

            projectSummaries.Add(new BacklogProjectSummaryDto(
                ProjectId: project.Id.ToString(),
                ProjectName: project.Name,
                IsOwner: isOwner,
                ActiveTaskCount: activeCount,
                TotalTaskCount: totalCount));

            totalTasks += totalCount;
        }

        return new BacklogSummaryDto(
            Projects: projectSummaries.OrderBy(p => p.ProjectName),
            TotalTasks: totalTasks,
            TotalProjects: projectSummaries.Count);
    }
}

/// <summary>
/// log get (project)
/// </summary>
public sealed record GetBacklogStatsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId) : IQuery<BacklogStatsDto?>;

public sealed class GetBacklogStatsQueryHandler : IRequestHandler<GetBacklogStatsQuery, BacklogStatsDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBacklogStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BacklogStatsDto?> Handle(GetBacklogStatsQuery request, CancellationToken cancellationToken)
    {
        var projectRepo = _unitOfWork.Repository<Project>();
        var taskRepo = _unitOfWork.Repository<TaskItem>();
        var userRepo = _unitOfWork.Repository<User>();

        var project = await (projectRepo.GetByIdAsync(request.ProjectId, cancellationToken)).ConfigureAwait(false);
        if (project is null || project.TenantId != request.TenantId) return null;

        var isOwner = project.OwnerId.HasValue && project.OwnerId.Value == request.UserId;

        // Fetch tasks based on role
        IReadOnlyList<TaskItem> tasks;
        if (isOwner)
        {
            tasks = await (taskRepo.FindAsync(
                t => t.TenantId == request.TenantId && t.ProjectId == request.ProjectId && t.IsActive,
                cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            tasks = await (taskRepo.FindAsync(
                t => t.TenantId == request.TenantId && t.ProjectId == request.ProjectId && t.IsActive
                    && (t.AssigneeId == request.UserId || t.CreatorId == request.UserId),
                cancellationToken)).ConfigureAwait(false);
        }

        // Status counts
        var backlogCount = tasks.Count(t => t.Status == TaskItemStatus.Backlog);
        var toDoCount = tasks.Count(t => t.Status == TaskItemStatus.ToDo);
        var inProgressCount = tasks.Count(t => t.Status == TaskItemStatus.InProgress);
        var doneCount = tasks.Count(t => t.Status == TaskItemStatus.Done);
        var blockedCount = tasks.Count(t => t.Status == TaskItemStatus.Blocked);

        // Priority distribution
        var priorityDistribution = Enum.GetValues<TaskItemPriority>()
            .Select(p => new BacklogPriorityDistributionDto(p, tasks.Count(t => t.Priority == p)))
            .Where(p => p.Count > 0)
            .ToList();

        // Effort totals
        var totalEstimatedHours = tasks.Sum(t => t.EstimatedHours);
        var totalActualHours = tasks.Sum(t => t.ActualHours);

        // Assignee stats (only for owner)
        var assigneeStats = new List<BacklogAssigneeStatsDto>();
        if (isOwner)
        {
            var assigneeGroups = tasks
                .Where(t => t.AssigneeId.HasValue)
                .GroupBy(t => t.AssigneeId!.Value);

            foreach (var group in assigneeGroups)
            {
                var assignee = await (UserRefHelper.ToUserRefAsync(userRepo, group.Key, cancellationToken)).ConfigureAwait(false);
                assigneeStats.Add(new BacklogAssigneeStatsDto(
                    Assignee: assignee,
                    BacklogCount: group.Count(t => t.Status == TaskItemStatus.Backlog),
                    ToDoCount: group.Count(t => t.Status == TaskItemStatus.ToDo),
                    InProgressCount: group.Count(t => t.Status == TaskItemStatus.InProgress),
                    DoneCount: group.Count(t => t.Status == TaskItemStatus.Done),
                    BlockedCount: group.Count(t => t.Status == TaskItemStatus.Blocked),
                    TotalCount: group.Count()));
            }
        }

        return new BacklogStatsDto(
            ProjectId: project.Id.ToString(),
            ProjectName: project.Name,
            IsOwner: isOwner,
            TotalTasks: tasks.Count,
            BacklogCount: backlogCount,
            ToDoCount: toDoCount,
            InProgressCount: inProgressCount,
            DoneCount: doneCount,
            BlockedCount: blockedCount,
            TotalEstimatedHours: totalEstimatedHours,
            TotalActualHours: totalActualHours,
            PriorityDistribution: priorityDistribution,
            AssigneeStats: assigneeStats.OrderByDescending(a => a.TotalCount));
    }
}
