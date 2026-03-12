namespace Sddp.Abstractions.Enums;

/// <summary>
/// Task priority
/// </summary>
public enum TaskItemPriority
{
    /// <summary>Low</summary>
    Low = 0,

    /// <summary>Medium</summary>
    Medium = 1,

    /// <summary>High</summary>
    High = 2,

    /// <summary>Urgent</summary>
    Urgent = 3
}

/// <summary>
/// TaskItemPriority extension methods
/// </summary>
public static class TaskItemPriorityExtensions
{
    /// <summary>
    /// Returns the priority description
    /// </summary>
    public static string GetDescription(this TaskItemPriority priority)
    {
        return priority switch
        {
            TaskItemPriority.Low => "Low",
            TaskItemPriority.Medium => "Medium",
            TaskItemPriority.High => "High",
            TaskItemPriority.Urgent => "Urgent",
            _ => "Unknown"
        };
    }
}
