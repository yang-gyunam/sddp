using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Actual work time record
/// </summary>
public class Worklog : EntityBase
{
    public GlobalUniqueId TenantId { get; private set; }

    public GlobalUniqueId ProjectId { get; private set; }

    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// Work date
    /// </summary>
    public DateOnly WorkDate { get; private set; }

    /// <summary>
    /// Actual hours spent (0-24)
    /// </summary>
    public decimal SpentHours { get; private set; }

    /// <summary>
    /// Work note
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Linked Task (optional)
    /// </summary>
    public GlobalUniqueId? TaskId { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public TaskItem? Task { get; private set; }

    // Default constructor for EF Core
    private Worklog() { }

    public Worklog(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId userId,
        DateOnly workDate,
        decimal spentHours,
        string? note = null,
        GlobalUniqueId? taskId = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        UserId = userId;
        WorkDate = workDate;
        SpentHours = Math.Clamp(spentHours, 0, 24);
        Note = note;
        TaskId = taskId;
    }

    public void Update(decimal spentHours, string? note, GlobalUniqueId? taskId)
    {
        SpentHours = Math.Clamp(spentHours, 0, 24);
        Note = note;
        TaskId = taskId;
        MarkAsModified();
    }
}
