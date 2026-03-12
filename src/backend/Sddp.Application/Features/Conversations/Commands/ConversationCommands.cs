using MediatR;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.SystemConfig;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Commands;

/// <summary>
/// conversation create (Channel, Forum, DirectMessage)
/// </summary>
public sealed record CreateConversationCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId UserId,
    string Name,
    ConversationType ConversationType,
    ConversationVisibility Visibility,
    ConversationScope Scope,
    string? Description,
    int SortOrder) : ICommand<ConversationSummaryDto?>, IAuditableRequest<ConversationSummaryDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationSummaryDto? response) => AuditLog;
}

public sealed class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationHubService _hubService;

    public CreateConversationCommandHandler(IUnitOfWork unitOfWork, IConversationHubService hubService)
    {
        _unitOfWork = unitOfWork;
        _hubService = hubService;
    }

    public async Task<ConversationSummaryDto?> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new SddpException("CREATE_FAILED", "Conversation name is required");
        }

        if (request.Scope == ConversationScope.ProjectScoped && !request.ProjectId.HasValue)
        {
            throw new SddpException("CREATE_FAILED", "ProjectScoped scope requires a ProjectId");
        }

        if (request.Scope == ConversationScope.TenantWide && request.ProjectId.HasValue)
        {
            throw new SddpException("CREATE_FAILED", "TenantWide scope cannot include a ProjectId");
        }

        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        Conversation conversation;
        try
        {
            conversation = request.ConversationType switch
            {
                ConversationType.Channel => new Channel(request.TenantId, request.ProjectId, request.Name, request.Visibility, request.SortOrder, request.Scope),
                ConversationType.Forum => new Forum(request.TenantId, request.ProjectId, request.Name, request.Visibility, request.SortOrder, request.Scope),
                ConversationType.DirectMessage => new DirectMessage(request.TenantId, request.ProjectId, request.Name, request.Scope),
                _ => throw new SddpException("CREATE_FAILED", $"Unknown conversation type: {request.ConversationType}")
            };
        }
        catch (InvalidOperationException ex)
        {
            // EnsureScopeConsistency — create
            throw new SddpException("CREATE_FAILED", ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var descResult = conversation.UpdateDescription(request.Description);
            descResult.EnsureSuccess("CREATE_FAILED");
        }

        await (conversationRepo.AddAsync(conversation, cancellationToken)).ConfigureAwait(false);

        var ownerMember = new ConversationMember(conversation.Id, request.UserId, ConversationMemberRole.Owner, ParticipantType.Human);
        await (memberRepo.AddAsync(ownerMember, cancellationToken)).ConfigureAwait(false);

        // Channel AI Agent (project/tenant AI)
        var memberCount = 1;
        if (request.ConversationType == ConversationType.Channel)
        {
            var aiEnabled = await (SystemConfigHelpers.IsAiEnabledAsync(
                _unitOfWork, request.TenantId, request.ProjectId, cancellationToken)).ConfigureAwait(false);

            if (aiEnabled)
            {
                var aiUserId = GlobalUniqueId.FromGuid(WellKnownUsers.AiAgentUserId);
                var aiMember = new ConversationMember(
                    conversation.Id, aiUserId,
                    ConversationMemberRole.Member, ParticipantType.AI);
                await (memberRepo.AddAsync(aiMember, cancellationToken)).ConfigureAwait(false);
                memberCount = 2;
            }
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // ProjectScoped Channel create project all notification
        if (request.ConversationType == ConversationType.Channel
            && request.Scope == ConversationScope.ProjectScoped
            && request.ProjectId.HasValue)
        {
            var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();
            var projectMembers = await (projectMemberRepo.FindAsync(
                m => m.ProjectId == request.ProjectId.Value && m.IsActive,
                cancellationToken)) .ConfigureAwait(false)?? [];

            var memberUserIds = projectMembers
                .Select(m => m.UserId.ToString())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            await (_hubService.NotifyProjectChannelCreatedAsync(
                memberUserIds,
                conversation.Id.ToString(),
                request.ProjectId.Value.ToString(),
                conversation.Name,
                request.UserId.ToString())).ConfigureAwait(false);
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "create",
            ResourceType: "conversation",
            ResourceId: conversation.Id,
            Payload: new { Type = request.ConversationType.ToString(), request.Name },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return ConversationMapping.MapConversationSummaryDto(
            conversation,
            memberCount: memberCount,
            unreadCount: 0,
            lastMessageAt: null);
    }
}

/// <summary>
/// conversation message get (, read status)
/// </summary>
// ICommand (not IQuery) — intentional: this handler updates UserReadStatus on first page load (write side-effect).
public sealed record GetConversationMessagesCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId,
    DateTimeOffset? Cursor,
    int Limit) : ICommand<ConversationMessagesPageDto?>;

public sealed class GetConversationMessagesCommandHandler : IRequestHandler<GetConversationMessagesCommand, ConversationMessagesPageDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationMessagesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationMessagesPageDto?> Handle(GetConversationMessagesCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || (conversation.ProjectId is not null && conversation.ProjectId != request.ProjectId)
            || !conversation.IsActive)
        {
            return null;
        }

        if (conversation.Visibility == ConversationVisibility.Private)
        {
            var isMember = await (memberRepo.CountAsync(
                m => m.ConversationId == request.ConversationId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)) .ConfigureAwait(false)> 0;
            if (!isMember)
            {
                throw new SddpException("NOT_ALLOWED", "User is not a member of this private conversation");
            }
        }

        var messages = await (messageRepo.FindAsync(
            m => m.ConversationId == request.ConversationId
                && m.IsActive
                && (request.Cursor == null || m.CreatedAt.ToDateTimeOffset() < request.Cursor),
            cancellationToken)).ConfigureAwait(false);

        var sortedMessages = messages
            .OrderByDescending(m => m.CreatedAt.ToDateTimeOffset())
            .Take(request.Limit + 1)
            .ToList();

        var hasMore = sortedMessages.Count > request.Limit;
        var resultMessages = sortedMessages.Take(request.Limit).ToList();

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var messageDtos = resultMessages.Select(m =>
        {
            var user = userMap.TryGetValue(m.SenderId, out var u) ? u : null;
            var sender = new UserRefDto(m.SenderId.ToString(), user?.DisplayName ?? "Unknown", user?.AvatarUrl);
            return ConversationMapping.MapConversationMessageDto(m, sender);
        });

        var nextCursor = hasMore && resultMessages.Any()
            ? resultMessages.Last().CreatedAt.ToDateTimeOffset().ToString("O")
            : null;

        if (request.Cursor is null)
        {
            var readStatuses = await (readStatusRepo.FindAsync(
                rs => rs.UserId == request.UserId && rs.ConversationId == request.ConversationId,
                cancellationToken)).ConfigureAwait(false);

            var readStatus = readStatuses.FirstOrDefault();
            var readAt = resultMessages.FirstOrDefault()?.CreatedAt.ToDateTimeOffset()
                ?? DateTimeOffset.UtcNow;

            if (readStatus is null)
            {
                readStatus = new UserReadStatus(request.TenantId, request.UserId, request.ConversationId, readAt);
                await (readStatusRepo.AddAsync(readStatus, cancellationToken)).ConfigureAwait(false);
            }
            else if (!readStatus.LastReadAt.HasValue
                     || readStatus.LastReadAt.Value.ToDateTimeOffset() < readAt)
            {
                readStatus.UpdateLastReadAt(readAt);
                await (readStatusRepo.UpdateAsync(readStatus, cancellationToken)).ConfigureAwait(false);
            }

            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        }

        return new ConversationMessagesPageDto(
            Messages: messageDtos,
            NextCursor: nextCursor,
            HasMore: hasMore);
    }
}

/// <summary>
/// conversation message
/// </summary>
public sealed record PostConversationMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId SenderUserId,
    MessageType Type,
    string Content,
    string[]? References,
    GlobalUniqueId? ReplyToId) : ICommand<PostMessageResultDto?>, IAuditableRequest<PostMessageResultDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(PostMessageResultDto? response) => AuditLog;
}

public sealed class PostConversationMessageCommandHandler : IRequestHandler<PostConversationMessageCommand, PostMessageResultDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingTriggerService _embeddingTrigger;
    private readonly INotificationService _notificationService;

    public PostConversationMessageCommandHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingTriggerService embeddingTrigger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _embeddingTrigger = embeddingTrigger;
        _notificationService = notificationService;
    }

    public async Task<PostMessageResultDto?> Handle(PostConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || (conversation.ProjectId is not null && conversation.ProjectId != request.ProjectId)
            || !conversation.IsActive)
        {
            return null;
        }

        if (!conversation.CanAddMessage())
        {
            throw new SddpException("NOT_ALLOWED", "Cannot add message to archived conversation");
        }

        // Channel status: Concluded/Archived channel message
        if (conversation is Channel channel && !channel.CanAddMessage())
        {
            throw new SddpException("NOT_ALLOWED", "Cannot add message to concluded or archived channel");
        }

        // DirectMessage status: Concluded/Archived DM message
        if (conversation is DirectMessage directMessage && !directMessage.CanAddMessage())
        {
            throw new SddpException("NOT_ALLOWED", "Cannot add message to concluded or archived direct message");
        }

        var sender = await (userRepo.GetByIdAsync(request.SenderUserId, cancellationToken)).ConfigureAwait(false);
        if (sender is null || !sender.IsActive)
        {
            throw new SddpException("NOT_ALLOWED", "Sender not found");
        }

        // (soft-delete) get → Public channel auto-join
        var senderMemberships = await (memberRepo.FindIncludingInactiveAsync(
            m => m.ConversationId == request.ConversationId
                && m.UserId == request.SenderUserId,
            cancellationToken)).ConfigureAwait(false);
        var senderMembership = senderMemberships.FirstOrDefault();
        ConversationMember? member = senderMembership is { IsActive: true } ? senderMembership : null;

        if (conversation.Visibility == ConversationVisibility.Private && member is null)
        {
            throw new SddpException("NOT_ALLOWED", "User is not a member of this private conversation");
        }

        // Public channel auto-participation:
        // if sender is not in participants, create/re-activate membership so participants panel stays consistent.
        var isNewParticipant = false;
        if (member is null
            && conversation is Channel channelConversation
            && channelConversation.Visibility == ConversationVisibility.Public)
        {
            isNewParticipant = true;
            if (senderMembership is not null)
            {
                senderMembership.Activate();
                await (memberRepo.UpdateAsync(senderMembership, cancellationToken)).ConfigureAwait(false);
                member = senderMembership;
            }
            else
            {
                member = new ConversationMember(
                    request.ConversationId,
                    request.SenderUserId,
                    ConversationMemberRole.Member,
                    sender.IsAI ? ParticipantType.AI : ParticipantType.Human);
                await (memberRepo.AddAsync(member, cancellationToken)).ConfigureAwait(false);
            }

            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        }

        if (member is not null)
        {
            if (request.Type.IsAiGenerated() && !member.Type.CanUseAiMessageTypes())
            {
                throw new SddpException("NOT_ALLOWED", "Only AI participants can use AI message types");
            }

            if (member.Type.CanUseAiMessageTypes() && !request.Type.IsAiGenerated())
            {
                throw new SddpException("NOT_ALLOWED", "AI participants can only use AI message types");
            }
        }

        if (request.ReplyToId.HasValue)
        {
            var replyToMessage = await (messageRepo.GetByIdAsync(request.ReplyToId.Value, cancellationToken)).ConfigureAwait(false);
            if (replyToMessage is null || replyToMessage.ConversationId != request.ConversationId)
            {
                throw new SddpException("NOT_ALLOWED", "Invalid reply-to message");
            }
        }

        var message = Message.CreateForChannel(
            request.TenantId,
            conversation.ProjectId ?? request.ProjectId,
            request.ConversationId,
            request.SenderUserId,
            request.Type,
            request.Content,
            request.References,
            request.ReplyToId);

        await (messageRepo.AddAsync(message, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.SenderUserId && rs.ConversationId == request.ConversationId,
            cancellationToken)).ConfigureAwait(false);

        var readStatus = readStatuses.FirstOrDefault();
        var readAt = message.CreatedAt.ToDateTimeOffset();

        if (readStatus is null)
        {
            readStatus = new UserReadStatus(request.TenantId, request.SenderUserId, request.ConversationId, readAt);
            await (readStatusRepo.AddAsync(readStatus, cancellationToken)).ConfigureAwait(false);
        }
        else if (!readStatus.LastReadAt.HasValue
                 || readStatus.LastReadAt.Value.ToDateTimeOffset() < readAt)
        {
            readStatus.UpdateLastReadAt(readAt);
            await (readStatusRepo.UpdateAsync(readStatus, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var senderName = sender?.DisplayName ?? "Unknown";
        var senderRef = new UserRefDto(request.SenderUserId.ToString(), senderName, sender?.AvatarUrl);

        // Trigger conversation embedding (fire-and-forget via background job)
        var projectId = conversation.ProjectId ?? request.ProjectId;
        if (projectId.HasValue)
        {
            _embeddingTrigger.TriggerConversationEmbedding(
                request.TenantId, projectId.Value, request.ConversationId);
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: request.SenderUserId,
            Action: "post_message",
            ResourceType: "conversation",
            ResourceId: request.ConversationId,
            Payload: new { MessageId = message.Id.ToString(), MessageType = request.Type.ToString() },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        // conversation new message notification ()
        var allMembers = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var conversationName = conversation.Name;
        foreach (var m in allMembers)
        {
            if (m.UserId == request.SenderUserId) continue;

            await (_notificationService.CreateNotificationAsync(
                request.TenantId,
                m.UserId,
                request.SenderUserId,
                "new_message",
 $"{senderName} #{conversationName} message ",
                string.Empty,
                "conversation",
                request.ConversationId,
                cancellationToken)).ConfigureAwait(false);
        }

        var messageDto = ConversationMapping.MapConversationMessageDto(message, senderRef);
        return new PostMessageResultDto(
            messageDto,
            isNewParticipant,
            isNewParticipant ? request.SenderUserId.ToString() : null,
            isNewParticipant ? senderName : null);
    }
}

// ================================================================
// 3-1. MarkConversationAsReadCommand
// ================================================================

/// <summary>
/// conversation read status
/// </summary>
public sealed record MarkConversationAsReadCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId,
    DateTimeOffset? ReadUntil) : ICommand<bool>;

public sealed class MarkConversationAsReadCommandHandler : IRequestHandler<MarkConversationAsReadCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkConversationAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MarkConversationAsReadCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            return false;
        }

        if (conversation.Visibility == ConversationVisibility.Private)
        {
            var isMember = await (memberRepo.CountAsync(
                m => m.ConversationId == request.ConversationId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)) .ConfigureAwait(false)> 0;
            if (!isMember)
            {
                throw new SddpException("NOT_ALLOWED", "User is not a member of this private conversation");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var requestedReadAt = request.ReadUntil ?? now;
        var readAt = requestedReadAt > now ? now : requestedReadAt;

        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId && rs.ConversationId == request.ConversationId,
            cancellationToken)).ConfigureAwait(false);

        var readStatus = readStatuses.FirstOrDefault();

        if (readStatus is null)
        {
            readStatus = new UserReadStatus(request.TenantId, request.UserId, request.ConversationId, readAt);
            await (readStatusRepo.AddAsync(readStatus, cancellationToken)).ConfigureAwait(false);
        }
        else if (!readStatus.LastReadAt.HasValue
                 || readStatus.LastReadAt.Value.ToDateTimeOffset() < readAt)
        {
            readStatus.UpdateLastReadAt(readAt);
            await (readStatusRepo.UpdateAsync(readStatus, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
        return true;
    }
}

// ================================================================
// 4. EditConversationMessageCommand
// ================================================================

/// <summary>
/// conversation message update
/// </summary>
public sealed record EditConversationMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId MessageId,
    GlobalUniqueId UserId,
    string NewContent) : ICommand<ConversationMessageDto?>, IAuditableRequest<ConversationMessageDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ConversationMessageDto? response) => AuditLog;
}

public sealed class EditConversationMessageCommandHandler : IRequestHandler<EditConversationMessageCommand, ConversationMessageDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public EditConversationMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationMessageDto?> Handle(EditConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || (conversation.ProjectId is not null && conversation.ProjectId != request.ProjectId)
            || !conversation.IsActive)
        {
            return null;
        }

        var message = await (messageRepo.GetByIdAsync(request.MessageId, cancellationToken)).ConfigureAwait(false);
        if (message is null
            || message.ConversationId != request.ConversationId
            || !message.IsActive)
        {
            throw new SddpException("EDIT_FAILED", "Message not found");
        }

        if (message.SenderId != request.UserId)
        {
            throw new SddpException("EDIT_FAILED", "Only the message sender can edit this message");
        }

        if (string.IsNullOrWhiteSpace(request.NewContent))
        {
            throw new SddpException("EDIT_FAILED", "Message content is required");
        }

        message.Edit(request.NewContent);
        await (messageRepo.UpdateAsync(message, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var sender = await (userRepo.GetByIdAsync(message.SenderId, cancellationToken)).ConfigureAwait(false);
        var senderName = sender?.DisplayName ?? "Unknown";

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "edit_message",
            ResourceType: "message",
            ResourceId: request.MessageId,
            Payload: new { ConversationId = request.ConversationId.ToString(), message.Type },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return ConversationMapping.MapConversationMessageDto(message, new UserRefDto(message.SenderId.ToString(), senderName, sender?.AvatarUrl));
    }
}

// ================================================================
// 5. DeleteConversationMessageCommand
// ================================================================

/// <summary>
/// conversation message delete
/// </summary>
public sealed record DeleteConversationMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId MessageId,
    GlobalUniqueId UserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class DeleteConversationMessageCommandHandler : IRequestHandler<DeleteConversationMessageCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || (conversation.ProjectId is not null && conversation.ProjectId != request.ProjectId)
            || !conversation.IsActive)
        {
            return false;
        }

        var message = await (messageRepo.GetByIdAsync(request.MessageId, cancellationToken)).ConfigureAwait(false);
        if (message is null
            || message.ConversationId != request.ConversationId
            || !message.IsActive)
        {
            throw new SddpException("DELETE_FAILED", "Message not found");
        }

        // Owner of the message can always delete
        if (message.SenderId != request.UserId)
        {
            // Check if requester is Moderator+ (can delete others' messages)
            var members = await (memberRepo.FindAsync(
                m => m.ConversationId == request.ConversationId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)).ConfigureAwait(false);
            var requester = members.FirstOrDefault();
            if (requester is null || !requester.Role.CanDeleteMessages())
            {
                throw new SddpException("DELETE_FAILED", "Only the message sender or a moderator can delete this message");
            }
        }

        message.Deactivate();
        await (messageRepo.UpdateAsync(message, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "delete_message",
            ResourceType: "message",
            ResourceId: request.MessageId,
            Payload: new { ConversationId = request.ConversationId.ToString(), message.Type, message.SenderId },
            TenantId: request.TenantId,
            ProjectId: request.ProjectId);

        return true;
    }
}
