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

    public ConversationHubService(
        IHubContext<ConversationHub> hubContext,
        ILogger<ConversationHubService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task BroadcastNewMessageAsync(string conversationId, MessageDto message)
    {
        _logger.LogDebug(
            "Broadcasting new message {MessageId} to conversation {ConversationId}",
            message.Id, conversationId);

        var memberUserIds = await GetActiveMemberUserIdsAsync(conversationId).ConfigureAwait(false);
        if (memberUserIds.Count > 0)
        {
            await _hubContext.Clients.Users(memberUserIds).SendAsync("NewMessage", message).ConfigureAwait(false);
            return;
        }

        // Fallback keeps legacy behavior for conversations without persisted active members.
        var groupName = GetGroupName(conversationId);
        await _hubContext.Clients.Group(groupName).SendAsync("NewMessage", message).ConfigureAwait(false);
    }

    public async Task BroadcastMessageEditedAsync(string conversationId, MessageDto message)
    {
        var groupName = GetGroupName(conversationId);

        _logger.LogDebug(
            "Broadcasting message edit {MessageId} to conversation {ConversationId}",
            message.Id, conversationId);

        await _hubContext.Clients.Group(groupName).SendAsync("MessageEdited", message);
    }

    public async Task BroadcastMessageDeletedAsync(string conversationId, string messageId)
    {
        var groupName = GetGroupName(conversationId);

        _logger.LogDebug(
            "Broadcasting message deletion {MessageId} to conversation {ConversationId}",
            messageId, conversationId);

        await _hubContext.Clients.Group(groupName).SendAsync("MessageDeleted", messageId);
    }

    public async Task BroadcastConversationClosedAsync(string conversationId, string? decisionSpecId, string? concludedByUserName = null)
    {
        var groupName = GetGroupName(conversationId);

        _logger.LogInformation(
            "Broadcasting conversation closed {ConversationId}, DecisionSpecId: {DecisionSpecId}",
            conversationId, decisionSpecId);

        await _hubContext.Clients.Group(groupName).SendAsync("ConversationClosed", new
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
        var userIds = memberUserIds.ToList();
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
        var groupName = GetGroupName(conversationId);

        _logger.LogDebug(
            "Broadcasting member joined {UserId} to conversation {ConversationId}",
            userId, conversationId);

        await _hubContext.Clients.Group(groupName).SendAsync("MemberJoined", new
        {
            ConversationId = conversationId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task BroadcastMemberRemovedAsync(string conversationId, string removedUserId, string removedUserName)
    {
        var groupName = GetGroupName(conversationId);

        _logger.LogDebug(
            "Broadcasting member removed {UserId} from conversation {ConversationId}",
            removedUserId, conversationId);

        await _hubContext.Clients.Group(groupName).SendAsync("MemberRemoved", new
        {
            ConversationId = conversationId,
            RemovedUserId = removedUserId,
            RemovedUserName = removedUserName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static string GetGroupName(string conversationId)
    {
        return $"conversation:{conversationId}";
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
