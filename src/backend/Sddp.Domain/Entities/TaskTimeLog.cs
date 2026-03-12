using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Task effort log entity.
/// </summary>
public class TaskTimeLog : EntityBase
{
    public GlobalUniqueId TaskId { get; private set; }

    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// Work date.
    /// </summary>
    public DateOnly LogDate { get; private set; }

    /// <summary>
    /// Hours worked.
    /// </summary>
    public decimal Hours { get; private set; }

    /// <summary>
    /// Work description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    public TaskItem? Task { get; private set; }

    // Parameterless constructor for EF Core.
    private TaskTimeLog() { }

    public TaskTimeLog(
        GlobalUniqueId taskId,
        GlobalUniqueId userId,
        DateOnly logDate,
        decimal hours,
        string description)
    {
        TaskId = taskId;
        UserId = userId;
        LogDate = logDate;
        Hours = hours;
        Description = description;
    }
}
