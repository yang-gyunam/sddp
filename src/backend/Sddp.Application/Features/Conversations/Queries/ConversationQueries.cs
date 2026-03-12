using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Queries;

/// <summary>
/// conversation get (ID)
/// </summary>
public sealed record GetConversationByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : IQuery<ConversationSummaryDto?>;

public sealed class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto?> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
            || !conversation.IsActive)
        {
            return null;
        }

        // Visibility check
        if (conversation.Visibility == ConversationVisibility.Private)
        {
            var isMember = await (memberRepo.CountAsync(
                m => m.ConversationId == request.ConversationId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)) .ConfigureAwait(false)> 0;
            if (!isMember)
            {
                return null;
            }
        }

        // Member count
        var memberCount = await (memberRepo.CountAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        // Messages for unread count and last message
        var conversationGuid = request.ConversationId.ToGuid();
        var messages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && (Guid)m.ConversationId.Value == conversationGuid && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var messageList = messages.ToList();

        DateTimeOffset? lastMessageAt = messageList.Count > 0
            ? messageList.Max(m => m.CreatedAt.ToDateTimeOffset())
            : null;

        // Unread count
        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId && rs.ConversationId == request.ConversationId,
            cancellationToken)).ConfigureAwait(false);
        var lastReadAt = readStatuses.FirstOrDefault()?.LastReadAt?.ToDateTimeOffset();

        int unreadCount;
        if (lastReadAt.HasValue)
        {
            var lastReadTimestamp = Timestamp.FromDateTimeOffset(lastReadAt.Value);
            unreadCount = messageList.Count(m => m.CreatedAt > lastReadTimestamp);
        }
        else
        {
            unreadCount = messageList.Count;
        }

        return ConversationMapping.MapConversationSummaryDto(
            conversation,
            memberCount,
            unreadCount,
            lastMessageAt);
    }
}

/// <summary>
/// project conversation get (DM)
/// </summary>
public sealed record GetConversationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId UserId) : IQuery<IEnumerable<ConversationSummaryDto>>;

public sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IEnumerable<ConversationSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var conversations = await (conversationRepo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.ProjectId == request.ProjectId
                && c.IsActive
                && c.ConversationType != ConversationType.DirectMessage,
            cancellationToken)).ConfigureAwait(false);

        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId,
            cancellationToken)).ConfigureAwait(false);
        var readStatusMap = readStatuses.ToDictionary(
            rs => rs.ConversationId,
            rs => rs.LastReadAt?.ToDateTimeOffset());

        // Batch load all members for these conversations
        var conversationIds = conversations.Select(c => c.Id).ToList();
        var allMembers = await (memberRepo.FindAsync(
            m => conversationIds.Contains(m.ConversationId) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var membersByConversation = allMembers
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userMemberships = allMembers
            .Where(m => m.UserId == request.UserId)
            .Select(m => m.ConversationId)
            .ToHashSet();

        // Batch load all messages for these conversations (ConversationId is nullable on Message)
        // Convert to Guid list to avoid Npgsql array type mapping failure with GlobalUniqueId?
        var conversationGuids = conversationIds.Select(id => id.ToGuid()).ToList();
        var allMessages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && conversationGuids.Contains((Guid)m.ConversationId.Value) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var messagesByConversation = allMessages
            .Where(m => m.ConversationId.HasValue)
            .GroupBy(m => m.ConversationId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ConversationSummaryDto>();
        foreach (var conversation in conversations.OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
        {
            if (conversation.Visibility == ConversationVisibility.Private && !userMemberships.Contains(conversation.Id))
            {
                continue;
            }

            var memberCount = membersByConversation.TryGetValue(conversation.Id, out var members)
                ? members.Count
                : 0;

            var conversationMessages = messagesByConversation.TryGetValue(conversation.Id, out var msgs)
                ? msgs
                : [];

            DateTimeOffset? lastMessageAt = conversationMessages.Count > 0
                ? conversationMessages.Max(m => m.CreatedAt.ToDateTimeOffset())
                : null;

            var lastReadAt = readStatusMap.TryGetValue(conversation.Id, out var readAt)
                ? readAt
                : null;

            int unreadCount;
            if (lastReadAt.HasValue)
            {
                var lastReadTimestamp = Timestamp.FromDateTimeOffset(lastReadAt.Value);
                unreadCount = conversationMessages.Count(m => m.CreatedAt > lastReadTimestamp);
            }
            else
            {
                unreadCount = conversationMessages.Count;
            }

            results.Add(ConversationMapping.MapConversationSummaryDto(
                conversation,
                memberCount,
                unreadCount,
                lastMessageAt));
        }

        return results;
    }
}

/// <summary>
/// tenant all conversation get (project, DM)
/// </summary>
public sealed record GetGlobalConversationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<IEnumerable<ConversationSummaryDto>>;

public sealed class GetGlobalConversationsQueryHandler : IRequestHandler<GetGlobalConversationsQuery, IEnumerable<ConversationSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGlobalConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConversationSummaryDto>> Handle(GetGlobalConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        var conversations = await (conversationRepo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.ProjectId == null
                && c.IsActive
                && c.ConversationType != ConversationType.DirectMessage,
            cancellationToken)).ConfigureAwait(false);

        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId,
            cancellationToken)).ConfigureAwait(false);
        var readStatusMap = readStatuses.ToDictionary(
            rs => rs.ConversationId,
            rs => rs.LastReadAt?.ToDateTimeOffset());

        // Batch load all members for these conversations
        var conversationIds = conversations.Select(c => c.Id).ToList();
        var allMembers = await (memberRepo.FindAsync(
            m => conversationIds.Contains(m.ConversationId) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var membersByConversation = allMembers
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userMemberships = allMembers
            .Where(m => m.UserId == request.UserId)
            .Select(m => m.ConversationId)
            .ToHashSet();

        // Batch load all messages for these conversations (ConversationId is nullable on Message)
        // Convert to Guid list to avoid Npgsql array type mapping failure with GlobalUniqueId?
        var conversationGuids = conversationIds.Select(id => id.ToGuid()).ToList();
        var allMessages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && conversationGuids.Contains((Guid)m.ConversationId.Value) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var messagesByConversation = allMessages
            .Where(m => m.ConversationId.HasValue)
            .GroupBy(m => m.ConversationId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ConversationSummaryDto>();
        foreach (var conversation in conversations.OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
        {
            // Only show channels the user is a member of (created or invited)
            if (!userMemberships.Contains(conversation.Id))
            {
                continue;
            }

            var memberCount = membersByConversation.TryGetValue(conversation.Id, out var members)
                ? members.Count
                : 0;

            var conversationMessages = messagesByConversation.TryGetValue(conversation.Id, out var msgs)
                ? msgs
                : [];

            DateTimeOffset? lastMessageAt = conversationMessages.Count > 0
                ? conversationMessages.Max(m => m.CreatedAt.ToDateTimeOffset())
                : null;

            var lastReadAt = readStatusMap.TryGetValue(conversation.Id, out var readAt)
                ? readAt
                : null;

            int unreadCount;
            if (lastReadAt.HasValue)
            {
                var lastReadTimestamp = Timestamp.FromDateTimeOffset(lastReadAt.Value);
                unreadCount = conversationMessages.Count(m => m.CreatedAt > lastReadTimestamp);
            }
            else
            {
                unreadCount = conversationMessages.Count;
            }

            results.Add(ConversationMapping.MapConversationSummaryDto(
                conversation,
                memberCount,
                unreadCount,
                lastMessageAt));
        }

        return results;
    }
}

/// <summary>
/// conversation get
/// </summary>
public sealed record GetConversationMembersQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId) : IQuery<IEnumerable<ParticipantDto>?>;

public sealed class GetConversationMembersQueryHandler : IRequestHandler<GetConversationMembersQuery, IEnumerable<ParticipantDto>?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationMembersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ParticipantDto>?> Handle(GetConversationMembersQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var userRepo = _unitOfWork.Repository<User>();

        var conversation = await (conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null
            || conversation.TenantId != request.TenantId
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
                throw new SddpException("ACCESS_DENIED", "User is not a member of this private conversation");
            }
        }

        var members = await (memberRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        return members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt.ToDateTimeOffset())
            .Select(m =>
            {
                var user = userMap.TryGetValue(m.UserId, out var u) ? u : null;
                var userRef = new UserRefDto(m.UserId.ToString(), user?.DisplayName ?? "Unknown", user?.AvatarUrl);
                return ConversationMapping.MapParticipantDto(m, userRef);
            });
    }
}

/// <summary>
/// conversation message get
/// </summary>
public sealed record GetUnreadCountsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId? ProjectId = null) : IQuery<UnreadCountsDto>;

public sealed class GetUnreadCountsQueryHandler : IRequestHandler<GetUnreadCountsQuery, UnreadCountsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUnreadCountsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnreadCountsDto> Handle(GetUnreadCountsQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        // Get conversations the user can access
        IEnumerable<Conversation> conversations;
        if (request.ProjectId.HasValue)
        {
            conversations = await (conversationRepo.FindAsync(
                c => c.TenantId == request.TenantId
                    && c.ProjectId == request.ProjectId.Value
                    && c.IsActive,
                cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            conversations = await (conversationRepo.FindAsync(
                c => c.TenantId == request.TenantId && c.IsActive,
                cancellationToken)).ConfigureAwait(false);
        }

        var conversationIds = conversations.Select(c => c.Id).ToList();

        // Filter private conversations by membership
        var allMembers = await (memberRepo.FindAsync(
            m => conversationIds.Contains(m.ConversationId) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var userMemberships = allMembers
            .Where(m => m.UserId == request.UserId)
            .Select(m => m.ConversationId)
            .ToHashSet();

        var accessibleConversations = conversations
            .Where(c => c.Visibility != ConversationVisibility.Private || userMemberships.Contains(c.Id))
            .ToList();

        var accessibleIds = accessibleConversations.Select(c => c.Id).ToList();

        // Read statuses
        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId,
            cancellationToken)).ConfigureAwait(false);
        var readStatusMap = readStatuses.ToDictionary(
            rs => rs.ConversationId,
            rs => rs.LastReadAt?.ToDateTimeOffset());

        // Messages
        var accessibleGuids = accessibleIds.Select(id => id.ToGuid()).ToList();
        var allMessages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && accessibleGuids.Contains((Guid)m.ConversationId.Value) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var messagesByConversation = allMessages
            .Where(m => m.ConversationId.HasValue)
            .GroupBy(m => m.ConversationId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var byConversation = new List<ConversationUnreadDto>();
        var totalUnread = 0;

        foreach (var conversation in accessibleConversations)
        {
            var conversationMessages = messagesByConversation.TryGetValue(conversation.Id, out var msgs)
                ? msgs : [];

            var lastReadAt = readStatusMap.TryGetValue(conversation.Id, out var readAt)
                ? readAt : null;

            int unreadCount;
            if (lastReadAt.HasValue)
            {
                var lastReadTimestamp = Timestamp.FromDateTimeOffset(lastReadAt.Value);
                unreadCount = conversationMessages.Count(m => m.CreatedAt > lastReadTimestamp);
            }
            else
            {
                unreadCount = conversationMessages.Count;
            }

            if (unreadCount > 0)
            {
                var lastMessageAt = conversationMessages.Count > 0
                    ? conversationMessages.Max(m => m.CreatedAt.ToDateTimeOffset())
                    : (DateTimeOffset?)null;

                byConversation.Add(new ConversationUnreadDto(
                    conversation.Id.ToString(),
                    conversation.Name,
                    unreadCount,
                    lastMessageAt));
                totalUnread += unreadCount;
            }
        }

        return new UnreadCountsDto(totalUnread, byConversation);
    }
}

/// <summary>
/// conversation message get
/// </summary>
public sealed record GetPinnedConversationMessagesQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    GlobalUniqueId ConversationId,
    GlobalUniqueId UserId,
    int Limit) : IQuery<IEnumerable<ConversationMessageDto>?>;

public sealed class GetPinnedConversationMessagesQueryHandler : IRequestHandler<GetPinnedConversationMessagesQuery, IEnumerable<ConversationMessageDto>?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPinnedConversationMessagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConversationMessageDto>?> Handle(GetPinnedConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
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

        var pinnedMessages = await (messageRepo.FindAsync(
            m => m.ConversationId == request.ConversationId && m.IsActive && m.IsPinned,
            cancellationToken)).ConfigureAwait(false);

        if (request.Limit <= 0)
        {
            return Enumerable.Empty<ConversationMessageDto>();
        }

        var ordered = pinnedMessages
            .OrderByDescending(m => m.CreatedAt.ToDateTimeOffset())
            .Take(request.Limit)
            .ToList();

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        return ordered.Select(m =>
        {
            var user = userMap.TryGetValue(m.SenderId, out var u) ? u : null;
            var sender = new UserRefDto(m.SenderId.ToString(), user?.DisplayName ?? "Unknown", user?.AvatarUrl);
            return ConversationMapping.MapConversationMessageDto(m, sender);
        });
    }
}

/// <summary>
/// Conversation search (/description, autocomplete)
/// </summary>
public sealed record SearchConversationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId? ProjectId,
    string Query,
    int Limit = 15) : IQuery<IEnumerable<ConversationSearchResultDto>>;

public sealed class SearchConversationsQueryHandler : IRequestHandler<SearchConversationsQuery, IEnumerable<ConversationSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConversationSearchResultDto>> Handle(SearchConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var queryLower = request.Query.ToLower();

        var matches = await (conversationRepo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.IsActive
                && !c.IsArchived
                && (!request.ProjectId.HasValue || c.ProjectId == request.ProjectId.Value)
                && (c.Name.ToLower().Contains(queryLower)
                    || (c.Description != null && c.Description.ToLower().Contains(queryLower))),
            cancellationToken)).ConfigureAwait(false);

        return matches
            .OrderBy(c => c.Name)
            .Take(request.Limit)
            .Select(c => new ConversationSearchResultDto(
                Id: c.Id.ToString(),
                Name: c.Name,
                Description: c.Description,
                ConversationType: c.ConversationType));
    }
}

/// <summary>
/// (starred) conversation get
/// </summary>
public sealed record GetStarredConversationsQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId) : IQuery<IEnumerable<ConversationSummaryDto>>;

public sealed class GetStarredConversationsQueryHandler : IRequestHandler<GetStarredConversationsQuery, IEnumerable<ConversationSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStarredConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConversationSummaryDto>> Handle(GetStarredConversationsQuery request, CancellationToken cancellationToken)
    {
        var settingsRepo = _unitOfWork.Repository<UserConversationSettings>();
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        // Find starred conversation IDs for this user
        // Note: RepositoryBase.FindAsync already applies IsActive filter
        var starredSettings = await (settingsRepo.FindAsync(
            s => s.TenantId == request.TenantId
                && s.UserId == request.UserId
                && s.IsStarred,
            cancellationToken)).ConfigureAwait(false);

        var starredConversationIds = starredSettings.Select(s => s.ConversationId).ToList();
        if (starredConversationIds.Count == 0)
        {
            return [];
        }

        // Fetch the conversations (RepositoryBase applies IsActive filter automatically)
        var conversations = await (conversationRepo.FindAsync(
            c => starredConversationIds.Contains(c.Id),
            cancellationToken)).ConfigureAwait(false);

        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId,
            cancellationToken)).ConfigureAwait(false);
        var readStatusMap = readStatuses.ToDictionary(
            rs => rs.ConversationId,
            rs => rs.LastReadAt?.ToDateTimeOffset());

        // Batch load members and messages (RepositoryBase applies IsActive filter automatically)
        var conversationIds = conversations.Select(c => c.Id).ToList();
        var allMembers = await (memberRepo.FindAsync(
            m => conversationIds.Contains(m.ConversationId),
            cancellationToken)).ConfigureAwait(false);
        var userMemberships = allMembers
            .Where(m => m.UserId == request.UserId)
            .Select(m => m.ConversationId)
            .ToHashSet();
        var membersByConversation = allMembers
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.Count());

        var conversationGuids = conversationIds.Select(id => id.ToGuid()).ToList();
        var allMessages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && conversationGuids.Contains((Guid)m.ConversationId.Value),
            cancellationToken)).ConfigureAwait(false);
        var messagesByConversation = allMessages
            .Where(m => m.ConversationId.HasValue)
            .GroupBy(m => m.ConversationId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ConversationSummaryDto>();
        foreach (var conversation in conversations.OrderBy(c => c.Name))
        {
            // Skip private conversations the user is no longer a member of
            if (conversation.Visibility == ConversationVisibility.Private && !userMemberships.Contains(conversation.Id))
            {
                continue;
            }
            var memberCount = membersByConversation.TryGetValue(conversation.Id, out var count)
                ? count
                : 0;

            var conversationMessages = messagesByConversation.TryGetValue(conversation.Id, out var msgs)
                ? msgs
                : [];

            DateTimeOffset? lastMessageAt = conversationMessages.Count > 0
                ? conversationMessages.Max(m => m.CreatedAt.ToDateTimeOffset())
                : null;

            var lastReadAt = readStatusMap.TryGetValue(conversation.Id, out var readAt)
                ? readAt
                : null;

            int unreadCount;
            if (lastReadAt.HasValue)
            {
                var lastReadTimestamp = Timestamp.FromDateTimeOffset(lastReadAt.Value);
                unreadCount = conversationMessages.Count(m => m.CreatedAt > lastReadTimestamp);
            }
            else
            {
                unreadCount = conversationMessages.Count;
            }

            results.Add(ConversationMapping.MapConversationSummaryDto(
                conversation,
                memberCount,
                unreadCount,
                lastMessageAt));
        }

        return results;
    }
}
