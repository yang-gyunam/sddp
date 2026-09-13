using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Api.Hubs;

/// <summary>
/// Conversation Hub
/// </summary>
public class ConversationHubService : IConversationHubService
{
    private readonly IHubContext<ConversationHub> _hubContext;
    private readonly ILogger<ConversationHubService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPresenceTracker _presenceTracker;

    public ConversationHubService(
        IHubContext<ConversationHub> hubContext,
        ILogger<ConversationHubService> logger,
        IServiceScopeFactory scopeFactory,
        IPresenceTracker presenceTracker)
    {
        _hubContext = hubContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _presenceTracker = presenceTracker;
    }

    public async Task BroadcastNewMessageAsync(string conversationId, MessageDto message)
    {
        _logger.LogDebug(
            "Broadcasting new message {MessageId} to conversation {ConversationId}",
            message.Id, conversationId);

        await SendToActiveMembersAsync(conversationId, "NewMessage", message).ConfigureAwait(false);
    }

    public async Task BroadcastMessageEditedAsync(string conversationId, MessageDto message)
    {
        _logger.LogDebug(
            "Broadcasting message edit {MessageId} to conversation {ConversationId}",
            message.Id, conversationId);

        await SendToActiveMembersAsync(conversationId, "MessageEdited", message);
    }

    public async Task BroadcastMessageDeletedAsync(string conversationId, string messageId)
    {
        _logger.LogDebug(
            "Broadcasting message deletion {MessageId} to conversation {ConversationId}",
            messageId, conversationId);

        await SendToActiveMembersAsync(conversationId, "MessageDeleted", messageId);
    }

    public async Task BroadcastConversationClosedAsync(string conversationId, string? decisionSpecId, string? concludedByUserName = null)
    {
        _logger.LogInformation(
            "Broadcasting conversation closed {ConversationId}, DecisionSpecId: {DecisionSpecId}",
            conversationId, decisionSpecId);

        await SendToActiveMembersAsync(conversationId, "ConversationClosed", new
        {
            ConversationId = conversationId,
            DecisionSpecId = decisionSpecId,
            ConcludedBy = concludedByUserName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task NotifyProjectChannelCreatedAsync(
        IEnumerable<string> memberUserIds,
        string conversationId,
        string projectId,
        string channelName,
        string createdByUserId)
    {
        var userIds = memberUserIds.Distinct(StringComparer.Ordinal).ToList();
        if (userIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Notifying project channel created {ConversationId} to {UserCount} users in project {ProjectId}",
            conversationId, userIds.Count, projectId);

        await _hubContext.Clients.Users(userIds).SendAsync("ProjectChannelCreated", new
        {
            ConversationId = conversationId,
            ProjectId = projectId,
            ChannelName = channelName,
            CreatedByUserId = createdByUserId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task NotifyDMConcludedAsync(IEnumerable<string> memberUserIds, string conversationId, string concludedByUserName)
    {
        var activeUserIds = await GetActiveMemberUserIdsAsync(conversationId).ConfigureAwait(false);
        var userIds = memberUserIds.Intersect(activeUserIds, StringComparer.Ordinal).ToList();
        if (userIds.Count == 0) return;
        _logger.LogInformation(
            "Notifying DM concluded {ConversationId} to {UserCount} users",
            conversationId, userIds.Count);

        await _hubContext.Clients.Users(userIds).SendAsync("DMConcluded", new
        {
            ConversationId = conversationId,
            ConcludedBy = concludedByUserName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task BroadcastMemberJoinedAsync(string conversationId, string userId, string displayName)
    {
        _logger.LogDebug(
            "Broadcasting member joined {UserId} to conversation {ConversationId}",
            userId, conversationId);

        await SendToActiveMembersAsync(conversationId, "MemberJoined", new
        {
            ConversationId = conversationId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task BroadcastMemberRemovedAsync(string conversationId, string removedUserId, string removedUserName)
    {
        if (!GlobalUniqueId.TryParse(conversationId, out _) || !GlobalUniqueId.TryParse(removedUserId, out _)) return;

        _logger.LogDebug(
            "Broadcasting member removed {UserId} from conversation {ConversationId}",
            removedUserId, conversationId);

        var activeUserIds = await GetActiveMemberUserIdsAsync(conversationId).ConfigureAwait(false);
        // An older outbox event must not evict a member who has since rejoined.
        if (activeUserIds.Contains(removedUserId, StringComparer.Ordinal)) return;

        var groupName = GetGroupName(conversationId);
        foreach (var connectionId in _presenceTracker.GetConnectionIds(removedUserId))
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName).ConfigureAwait(false);
        }

        // The removed user still needs the existing notification to clean up their UI.
        var recipients = activeUserIds.Append(removedUserId).Distinct(StringComparer.Ordinal).ToList();
        await _hubContext.Clients.Users(recipients).SendAsync("MemberRemoved", new
        {
            ConversationId = conversationId,
            RemovedUserId = removedUserId,
            RemovedUserName = removedUserName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private async Task SendToActiveMembersAsync(string conversationId, string eventName, object payload)
    {
        var recipients = await GetActiveMemberUserIdsAsync(conversationId).ConfigureAwait(false);
        if (recipients.Count == 0) return;
        await _hubContext.Clients.Users(recipients).SendAsync(eventName, payload).ConfigureAwait(false);
    }

    private static string GetGroupName(string conversationId)
    {
        // GUID spelling must not create a second group that server-side removal cannot find.
        var canonicalId = GlobalUniqueId.TryParse(conversationId, out var id) ? id.ToString() : conversationId;
        return $"conversation:{canonicalId}";
    }

    private async Task<IReadOnlyList<string>> GetActiveMemberUserIdsAsync(string conversationId)
    {
        if (!GlobalUniqueId.TryParse(conversationId, out var conversationGid))
        {
            return Array.Empty<string>();
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var memberRepo = unitOfWork.Repository<ConversationMember>();
        var members = await memberRepo.FindAsync(
            m => m.ConversationId == conversationGid && m.IsActive,
            CancellationToken.None).ConfigureAwait(false);

        return members
            .Select(m => m.UserId.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
