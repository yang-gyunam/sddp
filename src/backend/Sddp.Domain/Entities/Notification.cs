using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// notification entity
/// user system/domain notification
/// </summary>
public class Notification : EntityBase
{
    /// <summary>
    /// tenant ID ()
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// notification ID
    /// </summary>
    public GlobalUniqueId RecipientId { get; private set; }

    /// <summary>
    /// notification user ID
    /// </summary>
    public GlobalUniqueId? ActorId { get; private set; }

    /// <summary>
    /// notification type ("task_assigned", "new_message")
    /// </summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>
    /// notification
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// notification
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// entity type ("task", "conversation")
    /// </summary>
    public string? EntityType { get; private set; }

    /// <summary>
    /// entity ID
    /// </summary>
    public GlobalUniqueId? EntityId { get; private set; }

    /// <summary>
    /// read
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public Timestamp? ReadAt { get; private set; }

    // EF Core default create
    private Notification() { }

    public Notification(
        GlobalUniqueId tenantId,
        GlobalUniqueId recipientId,
        GlobalUniqueId? actorId,
        string type,
        string title,
        string message,
        string? entityType = null,
        GlobalUniqueId? entityId = null)
    {
        TenantId = tenantId;
        RecipientId = recipientId;
        ActorId = actorId;
        Type = type;
        Title = title;
        Message = message;
        EntityType = entityType;
        EntityId = entityId;
        IsRead = false;
    }

    /// <summary>
    /// mark as read
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = Timestamp.Now;
        MarkAsModified();
    }
}
