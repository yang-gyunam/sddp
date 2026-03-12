using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Conversation member entity
/// DB table: conversation_members (conversation_id FK -> conversations.id)
/// </summary>
public class ConversationMember : EntityBase
{
    /// <summary>
    /// Conversation ID (FK → conversations.id)
    /// </summary>
    public GlobalUniqueId ConversationId { get; private set; }

    /// <summary>
    /// User ID (FK)
    /// </summary>
    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// Member role
    /// </summary>
    public ConversationMemberRole Role { get; private set; }

    /// <summary>
    /// Member type (Human/AI)
    /// </summary>
    public ParticipantType Type { get; private set; }

    /// <summary>
    /// Join timestamp
    /// </summary>
    public Timestamp JoinedAt { get; private set; }

    /// <summary>
    /// Mute-until timestamp (optional)
    /// </summary>
    public Timestamp? MutedUntil { get; private set; }

    /// <summary>
    /// Whether notifications are enabled
    /// </summary>
    public bool NotificationsEnabled { get; private set; }

    /// <summary>
    /// Conversation navigation property
    /// </summary>
    public Conversation Conversation { get; private set; } = null!;

    /// <summary>
    /// User navigation property
    /// </summary>
    public User User { get; private set; } = null!;

    // Default constructor for EF Core
    private ConversationMember() { }

    public ConversationMember(
        GlobalUniqueId conversationId,
        GlobalUniqueId userId,
        ConversationMemberRole role = ConversationMemberRole.Member,
        ParticipantType type = ParticipantType.Human)
    {
        ConversationId = conversationId;
        UserId = userId;
        Role = role;
        Type = type;
        JoinedAt = Timestamp.Now;
        NotificationsEnabled = true;
    }

    /// <summary>
    /// Changes the role
    /// </summary>
    public void ChangeRole(ConversationMemberRole role)
    {
        Role = role;
        MarkAsModified();
    }

    /// <summary>
    /// Mutes the member
    /// </summary>
    public void Mute(Timestamp until)
    {
        MutedUntil = until;
        MarkAsModified();
    }

    /// <summary>
    /// Unmutes the member
    /// </summary>
    public void Unmute()
    {
        MutedUntil = null;
        MarkAsModified();
    }

    /// <summary>
    /// Updates notification settings
    /// </summary>
    public void SetNotifications(bool enabled)
    {
        NotificationsEnabled = enabled;
        MarkAsModified();
    }

    /// <summary>
    /// Returns whether the member is currently muted
    /// </summary>
    public bool IsMuted() => MutedUntil.HasValue && MutedUntil.Value > Timestamp.Now;
}
