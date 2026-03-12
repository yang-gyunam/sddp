using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Conversation Hub
/// SignalR message.
/// </summary>
public interface IConversationHubService
{
    Task BroadcastNewMessageAsync(string conversationId, MessageDto message);
    Task BroadcastMessageEditedAsync(string conversationId, MessageDto message);
    Task BroadcastMessageDeletedAsync(string conversationId, string messageId);
    Task BroadcastConversationClosedAsync(string conversationId, string? decisionSpecId, string? concludedByUserName = null);
    Task NotifyProjectChannelCreatedAsync(
        IEnumerable<string> memberUserIds,
        string conversationId,
        string projectId,
        string channelName,
        string createdByUserId);
    Task NotifyDMConcludedAsync(IEnumerable<string> memberUserIds, string conversationId, string concludedByUserName);
    Task BroadcastMemberJoinedAsync(string conversationId, string userId, string displayName);
    Task BroadcastMemberRemovedAsync(string conversationId, string removedUserId, string removedUserName);
}
