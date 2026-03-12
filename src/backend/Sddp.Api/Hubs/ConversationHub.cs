using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Api.Hubs;

/// <summary>
/// Real-time Conversation communication hub
/// REQ-03.2: real-time message broadcast
/// </summary>
[Authorize]
public class ConversationHub : Hub
{
    private readonly ILogger<ConversationHub> _logger;
    private readonly IPresenceTracker _presenceTracker;
    private readonly IServiceScopeFactory _scopeFactory;

    public ConversationHub(
        ILogger<ConversationHub> logger,
        IPresenceTracker presenceTracker,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _presenceTracker = presenceTracker;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Handles connection start and begins presence tracking
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            var isFirstConnection = _presenceTracker.UserConnected(userId, Context.ConnectionId);

            // Notify other users only when this is the first active connection
            if (isFirstConnection)
            {
                await Clients.Others.SendAsync("UserOnline", new
                {
                    UserId = userId,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            // Send the full online-user list to the current caller
            await Clients.Caller.SendAsync("PresenceState", new
            {
                OnlineUserIds = _presenceTracker.GetOnlineUsers(),
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation(
                "User {UserId} connected (first={IsFirst})",
                userId, isFirstConnection);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Joins a conversation (group membership)
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            throw new HubException("User not authenticated");
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var convId))
        {
            throw new HubException("Invalid conversation ID");
        }

        var tenantId = GetTenantId();
        var uid = GlobalUniqueId.Parse(userId);

        // Verify that the conversation exists, matches the tenant, and is active
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var conversationRepo = unitOfWork.Repository<Conversation>();
        var memberRepo = unitOfWork.Repository<ConversationMember>();

        var cancellationToken = Context.ConnectionAborted;

        var conversation = await conversationRepo.GetByIdAsync(convId, cancellationToken);
        if (conversation is null || conversation.TenantId != tenantId || !conversation.IsActive)
        {
            throw new HubException("Conversation not found");
        }

        // The caller must be an active member, or the conversation must be public
        var members = await memberRepo.FindAsync(
            m => m.ConversationId == convId && m.UserId == uid && m.IsActive,
            cancellationToken);
        var isMember = members.Any();

        if (!isMember && conversation.Visibility != ConversationVisibility.Public)
        {
            throw new HubException("Access denied: not a member of this conversation");
        }

        var groupName = GetGroupName(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} joined conversation {ConversationId}",
            userId, conversationId);

        // Notify the other participants that the user joined
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", new
        {
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Leaves a conversation (group removal)
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            throw new HubException("User not authenticated");
        }

        var groupName = GetGroupName(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} left conversation {ConversationId}",
            userId, conversationId);

        // Notify the other participants that the user left
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", new
        {
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcasts that typing has started
    /// </summary>
    public async Task StartTyping(string conversationId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var groupName = GetGroupName(conversationId);
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            UserId = userId,
            IsTyping = true,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcasts that typing has stopped
    /// </summary>
    public async Task StopTyping(string conversationId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var groupName = GetGroupName(conversationId);
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            UserId = userId,
            IsTyping = false,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Handles disconnection and stops presence tracking
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            var isLastConnection = _presenceTracker.UserDisconnected(userId, Context.ConnectionId);

            // Notify others when the final connection closes
            if (isLastConnection)
            {
                await Clients.Others.SendAsync("UserOffline", new
                {
                    UserId = userId,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            _logger.LogInformation(
                "User {UserId} disconnected (last={IsLast}). Exception: {Exception}",
                userId, isLastConnection, exception?.Message);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Returns the online-user list (typically called after reconnect)
    /// </summary>
    public async Task GetOnlineUsers()
    {
        await Clients.Caller.SendAsync("PresenceState", new
        {
            OnlineUserIds = _presenceTracker.GetOnlineUsers(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Sends a conversation invitation (Owner/Moderator -> target user)
    /// </summary>
    public async Task SendInvitation(string conversationId, string targetUserId, string conversationName)
    {
        var userId = GetUserId();
        if (userId is null)
            throw new HubException("User not authenticated");

        if (!GlobalUniqueId.TryParse(conversationId, out var convId))
            throw new HubException("Invalid conversation ID");

        if (!GlobalUniqueId.TryParse(targetUserId, out var targetUid))
            throw new HubException("Invalid target user ID");

        var tenantId = GetTenantId();
        var uid = GlobalUniqueId.Parse(userId);
        var cancellationToken = Context.ConnectionAborted;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var conversationRepo = unitOfWork.Repository<Conversation>();
        var memberRepo = unitOfWork.Repository<ConversationMember>();
        var userRepo = unitOfWork.Repository<User>();

        // 1. Verify that the conversation exists, matches the tenant, and is active
        var conversation = await conversationRepo.GetByIdAsync(convId, cancellationToken);
        if (conversation is null || conversation.TenantId != tenantId || !conversation.IsActive)
        {
            throw new HubException("Conversation not found");
        }

        // 2. Verify that the requester is an owner or moderator
        var senderMembers = await memberRepo.FindAsync(
            m => m.ConversationId == convId && m.UserId == uid && m.IsActive,
            cancellationToken);
        var sender = senderMembers.FirstOrDefault();
        if (sender is null || !sender.Role.CanManageMembers())
        {
            throw new HubException("Only owners and moderators can send invitations");
        }

        // 3. Verify that the target user exists
        var targetUser = await userRepo.GetByIdAsync(targetUid, cancellationToken);
        if (targetUser is null || !targetUser.IsActive)
        {
            throw new HubException("Target user not found");
        }

        _logger.LogInformation(
            "User {UserId} sending invitation to {TargetUserId} for conversation {ConversationId}",
            userId, targetUserId, conversationId);

        var projectIdStr = conversation.ProjectId?.ToString();

        // Send invitation to all connections of the target user
        var targetConnections = _presenceTracker.GetConnectionIds(targetUserId);
        if (targetConnections.Count > 0)
        {
            await Clients.Clients(targetConnections).SendAsync("ConversationInvitation", new
            {
                ConversationId = conversationId,
                ConversationName = conversationName,
                InviterUserId = userId,
                InviterName = Context.User?.FindFirst("name")?.Value ?? "Unknown",
                ProjectId = projectIdStr,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Handles an invitation response (accept/reject)
    /// When accepted, the server also adds the member
    /// </summary>
    public async Task RespondToInvitation(string conversationId, string inviterUserId, bool accepted)
    {
        var userId = GetUserId();
        if (userId is null)
            throw new HubException("User not authenticated");

        if (!GlobalUniqueId.TryParse(conversationId, out var convId))
            throw new HubException("Invalid conversation ID");

        if (!GlobalUniqueId.TryParse(inviterUserId, out var inviterUid))
            throw new HubException("Invalid inviter user ID");

        var tenantId = GetTenantId();
        var responderName = Context.User?.FindFirst("name")?.Value ?? "Unknown";
        var uid = GlobalUniqueId.Parse(userId);

        // Verify that the conversation exists, matches the tenant, and is active
        var cancellationToken = Context.ConnectionAborted;
        using var outerScope = _scopeFactory.CreateScope();
        var outerUnitOfWork = outerScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var conversationRepo = outerUnitOfWork.Repository<Conversation>();
        var memberRepo = outerUnitOfWork.Repository<ConversationMember>();

        var conversation = await conversationRepo.GetByIdAsync(convId, cancellationToken);
        if (conversation is null || conversation.TenantId != tenantId || !conversation.IsActive)
        {
            throw new HubException("Conversation not found");
        }

        // Verify that the inviter is still an active member
        var inviterMembers = await memberRepo.FindAsync(
            m => m.ConversationId == convId && m.UserId == inviterUid && m.IsActive,
            cancellationToken);
        if (!inviterMembers.Any())
        {
            throw new HubException("Invitation is no longer valid");
        }

        _logger.LogInformation(
            "User {UserId} responded to invitation for {ConversationId}: accepted={Accepted}",
            userId, conversationId, accepted);

        // Add the member when the invitation is accepted
        if (accepted)
        {
            // Search including inactive members so previously removed members can be reactivated
            var allExisting = await memberRepo.FindIncludingInactiveAsync(
                m => m.ConversationId == convId && m.UserId == uid,
                cancellationToken);

            var existingMember = allExisting.FirstOrDefault();
            if (existingMember is not null && !existingMember.IsActive)
            {
                // Reactivate a previously removed member
                existingMember.Activate();
                await outerUnitOfWork.SaveChangesAsync(cancellationToken);
            }
            else if (existingMember is null)
            {
                // Add a new member
                var member = new ConversationMember(
                    convId, uid, ConversationMemberRole.Member, ParticipantType.Human);
                await memberRepo.AddAsync(member, cancellationToken);
                await outerUnitOfWork.SaveChangesAsync(cancellationToken);
            }
            // else: already an active member, ignore

            // Add all accepter connections to the SignalR group so they receive real-time messages
            var groupName = GetGroupName(conversationId);
            var acceptorConnections = _presenceTracker.GetConnectionIds(userId);
            foreach (var connId in acceptorConnections)
            {
                await Groups.AddToGroupAsync(connId, groupName);
            }

            // Notify the conversation group about MemberJoined
            await Clients.Group(groupName).SendAsync("MemberJoined", new
            {
                ConversationId = conversationId,
                UserId = userId,
                UserName = responderName,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Notify the inviter of the response
        var inviterConnections = _presenceTracker.GetConnectionIds(inviterUserId);
        if (inviterConnections.Count > 0)
        {
            await Clients.Clients(inviterConnections).SendAsync("InvitationResponse", new
            {
                ConversationId = conversationId,
                ResponderUserId = userId,
                ResponderName = responderName,
                Accepted = accepted,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    private GlobalUniqueId GetTenantId()
    {
        var tenantClaim = Context.User?.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !GlobalUniqueId.TryParse(tenantClaim, out var tenantId))
            throw new HubException("Invalid tenant context");
        return tenantId;
    }

    private static string GetGroupName(string conversationId)
    {
        return $"conversation:{conversationId}";
    }

    #endregion
}

/// <summary>
/// ConversationHub client interface (for typed hubs)
/// </summary>
public interface IConversationHubClient
{
    Task NewMessage(MessageDto message);
    Task MessageEdited(MessageDto message);
    Task MessageDeleted(string messageId);
    Task UserJoined(object payload);
    Task UserLeft(object payload);
    Task UserTyping(object payload);
    Task ConversationClosed(object payload);
    Task DMConcluded(object payload);
    Task UserOnline(object payload);
    Task UserOffline(object payload);
    Task PresenceState(object payload);
    Task ProjectChannelCreated(object payload);
    Task ConversationInvitation(object payload);
    Task InvitationResponse(object payload);
}
