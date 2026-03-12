using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Task entity
/// Conversation, Requirement, Spec, Artifact
/// </summary>
public class TaskLinkedItem : EntityBase
{
    public GlobalUniqueId TaskId { get; private set; }

    /// <summary>
    /// (conversation, requirement, spec, artifact)
    /// </summary>
    public string LinkedType { get; private set; } = string.Empty;

    /// <summary>
    /// entity ID
    /// </summary>
    public GlobalUniqueId LinkedEntityId { get; private set; }

    /// <summary>
    /// user ID
    /// </summary>
    public GlobalUniqueId LinkedBy { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public Timestamp LinkedAt { get; private set; } = Timestamp.Now;

    public TaskItem? Task { get; private set; }

    // EF Core default create
    private TaskLinkedItem() { }

    public TaskLinkedItem(
        GlobalUniqueId taskId,
        string linkedType,
        GlobalUniqueId linkedEntityId,
        GlobalUniqueId linkedBy)
    {
        TaskId = taskId;
        LinkedType = linkedType;
        LinkedEntityId = linkedEntityId;
        LinkedBy = linkedBy;
    }
}
