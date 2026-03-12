namespace Sddp.Abstractions.Enums;

/// <summary>
/// Conversation role
/// REQ-03: Conversation System
/// </summary>
public enum ConversationMemberRole
{
    /// <summary> - / permission</summary>
    Member = 0,

    /// <summary> - message permission</summary>
    Moderator = 1,

    /// <summary> - Conversation permission</summary>
    Owner = 2
}

/// <summary>
/// ConversationMemberRole
/// </summary>
public static class ConversationMemberRoleExtensions
{
    /// <summary>
    /// message delete permission
    /// </summary>
    public static bool CanDeleteMessages(this ConversationMemberRole role)
    {
        return role >= ConversationMemberRole.Moderator;
    }

    /// <summary>
    /// Conversation settings change permission
    /// </summary>
    public static bool CanManageConversation(this ConversationMemberRole role)
    {
        return role == ConversationMemberRole.Owner;
    }

    /// <summary>
    /// permission
    /// </summary>
    public static bool CanManageMembers(this ConversationMemberRole role)
    {
        return role >= ConversationMemberRole.Moderator;
    }
}
