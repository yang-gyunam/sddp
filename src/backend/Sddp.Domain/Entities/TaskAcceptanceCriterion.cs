using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Task acceptance criterion entity.
/// </summary>
public class TaskAcceptanceCriterion : EntityBase
{
    public GlobalUniqueId TaskId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public bool Completed { get; private set; }

    public int SortOrder { get; private set; }

    public TaskItem? Task { get; private set; }

    // Parameterless constructor for EF Core.
    private TaskAcceptanceCriterion() { }

    public TaskAcceptanceCriterion(GlobalUniqueId taskId, string description, int sortOrder = 0)
    {
        TaskId = taskId;
        Description = description;
        SortOrder = sortOrder;
    }

    public void ToggleCompleted()
    {
        Completed = !Completed;
        MarkAsModified();
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        MarkAsModified();
    }
}
