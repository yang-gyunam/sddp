using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Task entity
/// Supports project work items, state transitions, and effort tracking
/// </summary>
public class TaskItem : AuditableEntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID (null means a personal task)
    /// </summary>
    public GlobalUniqueId? ProjectId { get; private set; }

    /// <summary>
    /// Category ID (null means uncategorized)
    /// </summary>
    public GlobalUniqueId? CategoryId { get; private set; }

    /// <summary>
    /// Task title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Task description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Task status
    /// </summary>
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.ToDo;

    /// <summary>
    /// Task priority
    /// </summary>
    public TaskItemPriority Priority { get; private set; } = TaskItemPriority.Medium;

    /// <summary>
    /// Assignee ID
    /// </summary>
    public GlobalUniqueId? AssigneeId { get; private set; }

    /// <summary>
    /// Creator ID (task owner)
    /// </summary>
    public GlobalUniqueId CreatorId { get; private set; }

    /// <summary>
    /// Estimated effort in hours
    /// </summary>
    public decimal EstimatedHours { get; private set; }

    /// <summary>
    /// Actual effort in hours - aggregated from TimeLog entries
    /// </summary>
    public decimal ActualHours { get; private set; }

    /// <summary>
    /// Completion timestamp
    /// </summary>
    public Timestamp? CompletedAt { get; private set; }

    /// <summary>
    /// Due date (nullable, optional)
    /// </summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>
    /// Sort order (position within the Kanban board)
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    public ICollection<TaskAcceptanceCriterion> AcceptanceCriteria { get; private set; } = [];

    /// <summary>
    /// Linked items
    /// </summary>
    public ICollection<TaskLinkedItem> LinkedItems { get; private set; } = [];

    /// <summary>
    /// Time-log entries
    /// </summary>
    public ICollection<TaskTimeLog> TimeLogs { get; private set; } = [];

    // Default constructor for EF Core
    private TaskItem() { }

    public TaskItem(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string title,
        string description,
        TaskItemPriority priority,
        GlobalUniqueId creatorId,
        GlobalUniqueId? assigneeId = null,
        decimal estimatedHours = 0,
        DateOnly? dueDate = null,
        TaskItemStatus status = TaskItemStatus.ToDo)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        Title = title;
        Description = description;
        Priority = priority;
        CreatorId = creatorId;
        AssigneeId = assigneeId;
        EstimatedHours = estimatedHours;
        DueDate = dueDate;
        Status = status;
        if (status == TaskItemStatus.Done)
        {
            CompletedAt = Timestamp.Now;
        }
    }

    /// <summary>
    /// Updates task information
    /// </summary>
    public void Update(string title, string description, TaskItemPriority priority, decimal estimatedHours)
    {
        Title = title;
        Description = description;
        Priority = priority;
        EstimatedHours = estimatedHours;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the due date
    /// </summary>
    public void SetDueDate(DateOnly? dueDate)
    {
        DueDate = dueDate;
        MarkAsModified();
    }

    /// <summary>
    /// Reassigns the task
    /// </summary>
    public void AssignTo(GlobalUniqueId? assigneeId)
    {
        AssigneeId = assigneeId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the category
    /// </summary>
    public void SetCategory(GlobalUniqueId? categoryId)
    {
        CategoryId = categoryId;
        MarkAsModified();
    }

    /// <summary>
    /// Transitions the task state
    /// </summary>
    public Result TransitionTo(TaskItemStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            return DomainError.InvalidTransition($"transition from {Status} to {newStatus}", Status.ToString());
        }

        Status = newStatus;

        if (newStatus == TaskItemStatus.Done)
        {
            CompletedAt = Timestamp.Now;
        }
        else if (CompletedAt is not null)
        {
            CompletedAt = null;
        }

        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Adds actual effort (called when a TimeLog is added)
    /// </summary>
    public void AddActualHours(decimal hours)
    {
        ActualHours += hours;
        MarkAsModified();
    }

    /// <summary>
    /// Recalculates actual effort (called when a TimeLog is removed)
    /// </summary>
    public void RecalculateActualHours(decimal totalHours)
    {
        ActualHours = totalHours;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the position after Kanban drag-and-drop
    /// </summary>
    public Result UpdatePosition(TaskItemStatus newStatus, int newSortOrder)
    {
        if (Status != newStatus)
        {
            var result = TransitionTo(newStatus);
            if (result.IsFailure) return result;
        }
        SortOrder = newSortOrder;
        MarkAsModified();
        return Result.Success();
    }
}
