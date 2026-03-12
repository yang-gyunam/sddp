using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Dashboard;

internal static class DashboardWidgetHelpers
{
    /// <summary>
    /// Widget 1: Spec Health Radar — user Spec status
    /// </summary>
    internal static async Task<SpecHealthRadarDto> BuildSpecHealthAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var specRepo = unitOfWork.Repository<Spec>();
        var allSpecs = await (specRepo.FindAsync(
            s => s.TenantId == tenantId && s.IsActive && s.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        // Filter specs where user is an owner or creator
        var mySpecs = allSpecs
            .Where(s => (s.IsOwnedBy(userId))
                        || s.CreatedBy == userId)
            .ToList();

        return new SpecHealthRadarDto(
            Draft: mySpecs.Count(s => s.Status == SpecStatus.Draft),
            InReview: mySpecs.Count(s => s.Status == SpecStatus.InReview),
            Approved: mySpecs.Count(s => s.Status == SpecStatus.Approved),
            Locked: mySpecs.Count(s => s.Status == SpecStatus.Locked),
            Total: mySpecs.Count);
    }

    /// <summary>
    /// Widget 2: My Sign-off Queue — Sign-off
    /// </summary>
    internal static async Task<MySignOffQueueDto> BuildSignOffQueueAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var signOffRepo = unitOfWork.Repository<SignOff>();
        var specRepo = unitOfWork.Repository<Spec>();
        var projectRepo = unitOfWork.Repository<Project>();

        var pendingSignOffs = await (signOffRepo.FindAsync(
            so => so.TenantId == tenantId
                  && so.StakeholderId == userId
                  && so.Decision == SignOffDecision.Pending
                  && so.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var items = new List<PendingSignOffItemDto>();
        var now = DateTimeOffset.UtcNow;

        foreach (var so in pendingSignOffs.OrderByDescending(so => so.RequestedAt))
        {
            var spec = await (specRepo.GetByIdAsync(so.SpecId, cancellationToken)).ConfigureAwait(false);
            var project = await (projectRepo.GetByIdAsync(so.ProjectId, cancellationToken)).ConfigureAwait(false);

            var waitingDays = (int)(now - so.RequestedAt.ToDateTimeOffset()).TotalDays;
            var urgency = waitingDays > 7 ? "overdue" : waitingDays > 3 ? "urgent" : "normal";

            items.Add(new PendingSignOffItemDto(
                SignOffId: so.Id.ToString(),
                SpecId: so.SpecId.ToString(),
                SpecCode: spec?.Code ?? "",
                SpecTitle: spec?.Title ?? "",
                ProjectId: so.ProjectId.ToString(),
                ProjectName: project?.Name ?? "",
                RequestedAt: so.RequestedAt.ToDateTimeOffset().ToString("o"),
                WaitingDays: waitingDays,
                Urgency: urgency));
        }

        return new MySignOffQueueDto(items.Count, items);
    }

    /// <summary>
    /// Widget 3: Contribution Heatmap — 28
    /// </summary>
    internal static async Task<ContributionHeatmapDto> BuildContributionHeatmapAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var auditRepo = unitOfWork.Repository<AuditLog>();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-28);

        var logs = await (auditRepo.FindAsync(
            l => l.ActorId != null
                 && l.ActorId == userId
                 && l.IsActive
                 && l.ResourceType != "auth",
            cancellationToken)).ConfigureAwait(false);

        var recentLogs = logs
            .Where(l => l.CreatedAt.ToDateTimeOffset() >= cutoff)
            .ToList();

        // Group by date and count by activity type
        var days = new List<DayContributionDto>();
        var totalContributions = 0;

        for (var i = 27; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
            var dayLogs = recentLogs
                .Where(l => DateOnly.FromDateTime(l.CreatedAt.ToDateTimeOffset().UtcDateTime) == date)
                .ToList();

            var count = dayLogs.Count;
            totalContributions += count;

            days.Add(new DayContributionDto(
                Date: date.ToString("yyyy-MM-dd"),
                Count: count,
                SpecsCreated: dayLogs.Count(l => l.ResourceType.Equals("spec", StringComparison.OrdinalIgnoreCase)
                                                  && l.Action.Equals("Created", StringComparison.OrdinalIgnoreCase)),
                Comments: dayLogs.Count(l => l.Action.Equals("Commented", StringComparison.OrdinalIgnoreCase)),
                SignOffs: dayLogs.Count(l => l.ResourceType.Equals("signoff", StringComparison.OrdinalIgnoreCase)
                                             || l.Action.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                                             || l.Action.Equals("Rejected", StringComparison.OrdinalIgnoreCase)),
                TasksCompleted: dayLogs.Count(l => l.ResourceType.Equals("task", StringComparison.OrdinalIgnoreCase)
                                                    && l.Action.Equals("status", StringComparison.OrdinalIgnoreCase))));
        }

        return new ContributionHeatmapDto(days, totalContributions);
    }

    /// <summary>
    /// Widget 4: Project Spotlight — project
    /// </summary>
    internal static async Task<ProjectSpotlightDto?> BuildProjectSpotlightAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var auditRepo = unitOfWork.Repository<AuditLog>();
        var projectRepo = unitOfWork.Repository<Project>();
        var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);

        var logs = await (auditRepo.FindAsync(
            l => l.ActorId != null
                 && l.ActorId == userId
                 && l.IsActive
                 && l.ProjectId != null
                 && l.ResourceType != "auth",
            cancellationToken)).ConfigureAwait(false);

        var recentLogs = logs
            .Where(l => l.CreatedAt.ToDateTimeOffset() >= weekAgo)
            .ToList();

        if (recentLogs.Count == 0)
            return null;

        // Find the most active project
        var topProjectGroup = recentLogs
            .GroupBy(l => l.ProjectId)
            .OrderByDescending(g => g.Count())
            .First();

        var topProjectId = topProjectGroup.Key!.Value;
        var project = await (projectRepo.GetByIdAsync(topProjectId, cancellationToken)).ConfigureAwait(false);

        // Count team activity for this project
        var teamLogs = await (auditRepo.FindAsync(
            l => l.ProjectId == topProjectId && l.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var teamRecentLogs = teamLogs
            .Where(l => l.CreatedAt.ToDateTimeOffset() >= weekAgo)
            .ToList();

        var teamMemberCount = teamRecentLogs
            .Where(l => l.ActorId != null)
            .Select(l => l.ActorId)
            .Distinct()
            .Count();

        var recentChanges = topProjectGroup
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new SpotlightActivityDto(
                l.Action,
                l.ResourceType,
                l.CreatedAt.ToDateTimeOffset().ToString("o")))
            .ToList();

        return new ProjectSpotlightDto(
            ProjectId: topProjectId.ToString(),
            ProjectName: project?.Name,
            ChangeCount: topProjectGroup.Count(),
            TeamActivityCount: teamRecentLogs.Count,
            TeamMemberCount: teamMemberCount,
            RecentChanges: recentChanges);
    }

    /// <summary>
    /// Widget 5: Due Date Timeline — Task
    /// </summary>
    internal static async Task<DueDateTimelineDto> BuildDueDateTimelineAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var taskRepo = unitOfWork.Repository<TaskItem>();
        var projectRepo = unitOfWork.Repository<Project>();

        var tasks = await (taskRepo.FindAsync(
            t => t.TenantId == tenantId
                 && t.IsActive
                 && t.DueDate != null
                 && t.Status != TaskItemStatus.Done
                 && (t.AssigneeId == userId || t.CreatorId == userId),
            cancellationToken)).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var timelineTasks = new List<TimelineTaskDto>();

        foreach (var task in tasks.OrderBy(t => t.DueDate))
        {
            var daysFromToday = task.DueDate!.Value.DayNumber - today.DayNumber;
            string? projectName = null;
            if (task.ProjectId.HasValue)
            {
                var project = await (projectRepo.GetByIdAsync(task.ProjectId.Value, cancellationToken)).ConfigureAwait(false);
                projectName = project?.Name;
            }

            timelineTasks.Add(new TimelineTaskDto(
                TaskId: task.Id.ToString(),
                Title: task.Title,
                Status: task.Status.ToString(),
                Priority: task.Priority.ToString(),
                DueDate: task.DueDate.Value.ToString("yyyy-MM-dd"),
                ProjectName: projectName,
                DaysFromToday: daysFromToday,
                IsOverdue: daysFromToday < 0));
        }

        return new DueDateTimelineDto(
            Tasks: timelineTasks,
            OverdueCount: timelineTasks.Count(t => t.IsOverdue),
            UpcomingCount: timelineTasks.Count(t => !t.IsOverdue));
    }

    /// <summary>
    /// Widget 6: Effort Tracker —
    /// </summary>
    internal static async Task<MyEffortTrackerDto> BuildEffortTrackerAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var timeLogRepo = unitOfWork.Repository<TaskTimeLog>();
        var taskRepo = unitOfWork.Repository<TaskItem>();

        // Calculate this week's Monday
        var today = DateTime.UtcNow;
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = DateOnly.FromDateTime(today.AddDays(-daysFromMonday));
        var sunday = monday.AddDays(6);

        var timeLogs = await (timeLogRepo.FindIncludingInactiveAsync(
            tl => tl.UserId == userId
                  && tl.LogDate >= monday
                  && tl.LogDate <= sunday,
            cancellationToken)).ConfigureAwait(false);

        var timeLogList = timeLogs.ToList();
        var totalHours = timeLogList.Sum(tl => tl.Hours);

        // Daily breakdown
        var dayLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var dailyBreakdown = new List<DailyEffortSummaryDto>();

        for (var i = 0; i < 7; i++)
        {
            var date = monday.AddDays(i);
            var dayHours = timeLogList
                .Where(tl => tl.LogDate == date)
                .Sum(tl => tl.Hours);

            dailyBreakdown.Add(new DailyEffortSummaryDto(
                Date: date.ToString("yyyy-MM-dd"),
                DayLabel: dayLabels[i],
                Hours: dayHours));
        }

        // Task distribution
        var taskDistribution = new List<TaskEffortDto>();
        var taskGroups = timeLogList
            .GroupBy(tl => tl.TaskId)
            .OrderByDescending(g => g.Sum(tl => tl.Hours));

        foreach (var group in taskGroups)
        {
            var task = await (taskRepo.GetByIdAsync(group.Key, cancellationToken)).ConfigureAwait(false);
            var hours = group.Sum(tl => tl.Hours);
            var percentage = totalHours > 0 ? Math.Round(hours / totalHours * 100, 1) : 0;

            taskDistribution.Add(new TaskEffortDto(
                TaskId: group.Key.ToString(),
                TaskTitle: task?.Title ?? "Unknown",
                Hours: hours,
                Percentage: percentage));
        }

        return new MyEffortTrackerDto(
            TotalHoursThisWeek: totalHours,
            TargetHoursPerWeek: 40m,
            DailyBreakdown: dailyBreakdown,
            TaskDistribution: taskDistribution);
    }
}
