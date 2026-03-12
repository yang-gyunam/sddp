using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// user read status entity
/// </summary>
public class UserReadStatus : EntityBase
{
    /// <summary>
    /// tenant ID
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// user ID
    /// </summary>
    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// Conversation ID (Channel, Forum, DirectMessage, Topic)
    /// </summary>
    public GlobalUniqueId ConversationId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public Timestamp? LastReadAt { get; private set; }

    // EF Core default create
    private UserReadStatus() { }

    public UserReadStatus(
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        GlobalUniqueId conversationId,
        DateTimeOffset lastReadAt)
    {
        TenantId = tenantId;
        UserId = userId;
        ConversationId = conversationId;
        LastReadAt = Timestamp.FromDateTimeOffset(lastReadAt);
    }

    /// <summary>
    ///
    /// </summary>
    public void UpdateLastReadAt(DateTimeOffset readAt)
    {
        LastReadAt = Timestamp.FromDateTimeOffset(readAt);
        MarkAsModified();
    }
}
