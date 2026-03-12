namespace Sddp.Abstractions.Enums;

/// <summary>
/// Task status.
/// </summary>
public enum TaskItemStatus
{
    /// <summary>To do.</summary>
    ToDo = 0,

    /// <summary>In progress.</summary>
    InProgress = 1,

    /// <summary>Done.</summary>
    Done = 2,

    /// <summary>Blocked.</summary>
    Blocked = 3,

    /// <summary>Backlog.</summary>
    Backlog = 4
}

/// <summary>
/// TaskItemStatus extension methods.
/// </summary>
public static class TaskItemStatusExtensions
{
    /// <summary>
    /// Checks whether a transition to the target status is allowed.
    /// </summary>
    public static bool CanTransitionTo(this TaskItemStatus current, TaskItemStatus target)
    {
        return (current, target) switch
        {
            // Backlog -> ToDo
            (TaskItemStatus.Backlog, TaskItemStatus.ToDo) => true,

            // ToDo -> InProgress or Blocked
            (TaskItemStatus.ToDo, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.ToDo, TaskItemStatus.Blocked) => true,
            (TaskItemStatus.ToDo, TaskItemStatus.Backlog) => true,

            // InProgress -> Done, ToDo (revert), or Blocked
            (TaskItemStatus.InProgress, TaskItemStatus.Done) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.ToDo) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.Blocked) => true,

            // Done -> InProgress (reopen) or ToDo
            (TaskItemStatus.Done, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.Done, TaskItemStatus.ToDo) => true,

            // Blocked -> ToDo or InProgress (unblock)
            (TaskItemStatus.Blocked, TaskItemStatus.ToDo) => true,
            (TaskItemStatus.Blocked, TaskItemStatus.InProgress) => true,

            _ => false
        };
    }

    /// <summary>
    /// Returns a human-readable description for the status.
    /// </summary>
    public static string GetDescription(this TaskItemStatus status)
    {
        return status switch
        {
            TaskItemStatus.ToDo => "To Do",
            TaskItemStatus.InProgress => "In Progress",
            TaskItemStatus.Done => "Done",
            TaskItemStatus.Blocked => "Blocked",
            TaskItemStatus.Backlog => "Backlog",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Checks whether the status is editable (anything except Done).
    /// </summary>
    public static bool IsEditable(this TaskItemStatus status)
    {
        return status != TaskItemStatus.Done;
    }
}
