using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.DTOs;

public record CreateTaskItemDto(
    string Title,
    string Description,
    TaskItemPriority Priority,
    string? AssigneeId = null,
    decimal EstimatedHours = 0,
    DateOnly? DueDate = null,
    string? CategoryId = null,
    TaskItemStatus? Status = null);

public record UpdateTaskItemDto(
    string? Title = null,
    string? Description = null,
    TaskItemPriority? Priority = null,
    string? AssigneeId = null,
    decimal? EstimatedHours = null,
    DateOnly? DueDate = null,
    string? CategoryId = null);

public record UpdateTaskStatusDto(
    TaskItemStatus NewStatus);

public record CreateTaskTimeLogDto(
    string Date,
    decimal Hours,
    string Description);

public record CreateTaskLinkedItemDto(
    string LinkedType,
    string LinkedEntityId);

public record UpdateTaskPositionDto(
    TaskItemStatus NewStatus,
    int NewPosition);

public record TaskItemDto(
    string Id,
    string TenantId,
    string? ProjectId,
    string Title,
    string Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    UserRefDto? Assignee,
    UserRefDto Creator,
    decimal EstimatedHours,
    decimal ActualHours,
    int LinkedItemCount,
    int SortOrder,
    DateOnly? DueDate,
    string? CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public record TaskItemDetailDto(
    string Id,
    string TenantId,
    string? ProjectId,
    string Title,
    string Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    UserRefDto? Assignee,
    UserRefDto Creator,
    decimal EstimatedHours,
    decimal ActualHours,
    IEnumerable<TaskAcceptanceCriterionDto> AcceptanceCriteria,
    IEnumerable<TaskLinkedItemDto> LinkedItems,
    IEnumerable<TaskTimeLogDto> TimeLogs,
    UserRefDto CreatedBy,
    UserRefDto UpdatedBy,
    DateOnly? DueDate,
    string? CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public record TaskAcceptanceCriterionDto(
    string Id,
    string Description,
    bool Completed);

public record TaskLinkedItemDto(
    string Id,
    string Type,
    string EntityId,
    string EntityTitle,
    string LinkedBy,
    DateTimeOffset LinkedAt);

public record TaskTimeLogDto(
    string Id,
    string TaskId,
    UserRefDto User,
    string Date,
    decimal Hours,
    string Description,
    DateTimeOffset CreatedAt);

public record TaskItemPageDto(
    IEnumerable<TaskItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record MyTaskStatsDto(
    int ToDoCount,
    int InProgressCount,
    int DoneCount,
    int BlockedCount,
    int TotalCount);

public record TaskSearchResultDto(
    string Id,
    string Title,
    string Status);

// ============================================
// Backlog DTOs
// ============================================

public record BacklogProjectSummaryDto(
    string ProjectId,
    string ProjectName,
    bool IsOwner,
    int ActiveTaskCount,
    int TotalTaskCount);

public record BacklogSummaryDto(
    IEnumerable<BacklogProjectSummaryDto> Projects,
    int TotalTasks,
    int TotalProjects);

public record BacklogPriorityDistributionDto(
    TaskItemPriority Priority,
    int Count);

public record BacklogAssigneeStatsDto(
    UserRefDto Assignee,
    int BacklogCount,
    int ToDoCount,
    int InProgressCount,
    int DoneCount,
    int BlockedCount,
    int TotalCount);

public record BacklogStatsDto(
    string ProjectId,
    string ProjectName,
    bool IsOwner,
    int TotalTasks,
    int BacklogCount,
    int ToDoCount,
    int InProgressCount,
    int DoneCount,
    int BlockedCount,
    decimal TotalEstimatedHours,
    decimal TotalActualHours,
    IEnumerable<BacklogPriorityDistributionDto> PriorityDistribution,
    IEnumerable<BacklogAssigneeStatsDto> AssigneeStats);
