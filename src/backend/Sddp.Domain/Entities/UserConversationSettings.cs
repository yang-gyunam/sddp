using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// user Conversation settings entity
/// Starred, Muted
/// </summary>
public class UserConversationSettings : EntityBase
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
    /// Conversation ID
    /// </summary>
    public GlobalUniqueId ConversationId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public bool IsStarred { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public bool IsMuted { get; private set; }

    // EF Core default create
    private UserConversationSettings() { }

    public UserConversationSettings(
        GlobalUniqueId tenantId,
        GlobalUniqueId userId,
        GlobalUniqueId conversationId)
    {
        TenantId = tenantId;
        UserId = userId;
        ConversationId = conversationId;
        IsStarred = false;
        IsMuted = false;
    }

    /// <summary>
    ///
    /// </summary>
    public void ToggleStarred()
    {
        IsStarred = !IsStarred;
        MarkAsModified();
    }

    /// <summary>
    ///
    /// </summary>
    public void ToggleMuted()
    {
        IsMuted = !IsMuted;
        MarkAsModified();
    }

    /// <summary>
    /// settings
    /// </summary>
    public void SetStarred(bool starred)
    {
        IsStarred = starred;
        MarkAsModified();
    }

    /// <summary>
    /// settings
    /// </summary>
    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        MarkAsModified();
    }
}
