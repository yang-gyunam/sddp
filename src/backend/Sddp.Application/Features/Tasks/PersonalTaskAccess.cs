using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Tasks;

public static class PersonalTaskAccess
{
    /// <summary>
    /// True when <paramref name="userId"/> may act on <paramref name="task"/>.
    ///
    /// A personal task belongs to the person who created it and the person it is assigned to —
    /// assignment is how a personal task is handed over, so the assignee has to be included or the
    /// hand-off would leave them unable to open their own work.
    /// </summary>
    public static bool IsPermitted(TaskItem task, GlobalUniqueId userId)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.ProjectId.HasValue)
        {
            return true;
        }

        return task.CreatorId == userId
            || (task.AssigneeId.HasValue && task.AssigneeId.Value == userId);
    }
}
