namespace Sddp.Abstractions.DTOs;

public record DashboardStatDto(int Total, int Secondary);

// My Dashboard DTOs
public record MyDashboardDto(
    DashboardStatDto Tasks,
    DashboardStatDto Conversations,
    DashboardStatDto Specs,
    DashboardStatDto Requirements,
    DashboardStatDto Glossary,
    DashboardStatDto Artifacts,
    IReadOnlyList<AuditLogEntry> RecentActivities);

public record MyOverviewDto(
    DashboardStatDto Tasks,
    DashboardStatDto Conversations,
    DashboardStatDto Specs,
    DashboardStatDto Requirements,
    DashboardStatDto Glossary,
    DashboardStatDto Artifacts);

public record MyTasksDto(
    IReadOnlyList<TaskSummaryDto> Tasks,
    int Total,
    int ToDo,
    int InProgress,
    int Done);

public record TaskSummaryDto(
    string Id,
    string Title,
    string Status,
    string? ProjectName,
    DateTimeOffset? DueDate);

public record MyActivitiesDto(
    IReadOnlyList<AuditLogEntry> Activities,
    int Page,
    int PageSize,
    int TotalCount);

public record MyNotificationsDto(
    IReadOnlyList<NotificationDto> Notifications,
    int UnreadCount);

public record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    string? EntityType,
    string? EntityId,
    string? ActorName,
    DateTimeOffset CreatedAt);

// System Dashboard DTOs
public record SystemDashboardDto(
    DashboardStatDto Specs,
    DashboardStatDto Requirements,
    DashboardStatDto Conversations,
    DashboardStatDto Users,
    IReadOnlyList<AuditLogEntry> RecentActivities);

public record SystemStatsDto(
    int TotalProjects,
    int TotalUsers,
    int TotalSpecs,
    int TotalRequirements,
    int TotalConversations,
    int SpecsThisWeek,
    int RequirementsThisWeek,
    int ConversationsThisWeek);

public record AuditLogsDto(
    IReadOnlyList<AuditLogEntry> Logs,
    int Page,
    int PageSize,
    int TotalCount);

public record HealthCheckDto(
    string Status,
    IReadOnlyList<ServiceHealthDto> Services);

public record ServiceHealthDto(
    string Name,
    string Status,
    string? Message,
    long? ResponseTimeMs);

// ============================================
// Dashboard Phase 1 Widget DTOs
// ============================================

// Widget 1: Spec Health Radar
public record SpecHealthRadarDto(
    int Draft,
    int InReview,
    int Approved,
    int Locked,
    int Total);

// Widget 2: My Sign-off Queue
public record MySignOffQueueDto(
    int PendingCount,
    IReadOnlyList<PendingSignOffItemDto> Items);

public record PendingSignOffItemDto(
    string SignOffId,
    string SpecId,
    string SpecCode,
    string SpecTitle,
    string ProjectId,
    string ProjectName,
    string RequestedAt,
    int WaitingDays,
    string Urgency);

// Widget 3: Contribution Heatmap
public record ContributionHeatmapDto(
    IReadOnlyList<DayContributionDto> Days,
    int TotalContributions);

public record DayContributionDto(
    string Date,
    int Count,
    int SpecsCreated,
    int Comments,
    int SignOffs,
    int TasksCompleted);

// Widget 4: Project Spotlight
public record ProjectSpotlightDto(
    string? ProjectId,
    string? ProjectName,
    int ChangeCount,
    int TeamActivityCount,
    int TeamMemberCount,
    IReadOnlyList<SpotlightActivityDto> RecentChanges);

public record SpotlightActivityDto(
    string Action,
    string ResourceType,
    string Timestamp);

// Widget 5: Due Date Timeline
public record DueDateTimelineDto(
    IReadOnlyList<TimelineTaskDto> Tasks,
    int OverdueCount,
    int UpcomingCount);

public record TimelineTaskDto(
    string TaskId,
    string Title,
    string Status,
    string Priority,
    string? DueDate,
    string? ProjectName,
    int DaysFromToday,
    bool IsOverdue);

// Widget 6: Effort Tracker
public record MyEffortTrackerDto(
    decimal TotalHoursThisWeek,
    decimal TargetHoursPerWeek,
    IReadOnlyList<DailyEffortSummaryDto> DailyBreakdown,
    IReadOnlyList<TaskEffortDto> TaskDistribution);

public record DailyEffortSummaryDto(
    string Date,
    string DayLabel,
    decimal Hours);

public record TaskEffortDto(
    string TaskId,
    string TaskTitle,
    decimal Hours,
    decimal Percentage);

// Batch response
public record MyDashboardWidgetsDto(
    SpecHealthRadarDto SpecHealth,
    MySignOffQueueDto SignOffQueue,
    ContributionHeatmapDto ContributionHeatmap,
    ProjectSpotlightDto? ProjectSpotlight,
    DueDateTimelineDto DueDateTimeline,
    MyEffortTrackerDto EffortTracker);

// ============================================
// Project Dashboard DTOs
// ============================================

public record ProjectDashboardDto(
    ProjectInfoDto Project,
    ProjectDashboardStatsDto Statistics,
    IReadOnlyList<TeamMemberActivityDto> TeamMembers,
    IReadOnlyList<AuditLogEntry> RecentActivities,
    TaskProgressDto TaskProgress);

public record ProjectInfoDto(
    string Id,
    string Name,
    string OwnerId,
    string OwnerName,
    string Status,
    int MemberCount,
    string CreatedAt,
    string? LastActivityAt);

public record ProjectDashboardStatsDto(
    DashboardStatDto Conversations,
    DashboardStatDto Requirements,
    DashboardStatDto Specs,
    DashboardStatDto Tasks,
    DashboardStatDto Artifacts);

public record TeamMemberActivityDto(
    string UserId,
    string UserName,
    string Role,
    int ActivityCount);

public record TaskProgressDto(
    int ToDo,
    int InProgress,
    int Done,
    int Total);
