using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Queries;

/// <summary>
/// user DM get
/// </summary>
public sealed record GetDirectMessagesQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId? ProjectId = null) : IQuery<IEnumerable<DMDto>>;

public sealed class GetDirectMessagesQueryHandler : IRequestHandler<GetDirectMessagesQuery, IEnumerable<DMDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDirectMessagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DMDto>> Handle(GetDirectMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversationRepo = _unitOfWork.Repository<Conversation>();
        var dmRepo = _unitOfWork.Repository<DirectMessage>();
        var memberRepo = _unitOfWork.Repository<ConversationMember>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();
        var readStatusRepo = _unitOfWork.Repository<UserReadStatus>();

        // 1. Find all DM conversations where the user is a member
        var userMemberships = await (memberRepo.FindAsync(
            m => m.UserId == request.UserId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var userConversationIds = userMemberships.Select(m => m.ConversationId).ToList();

        if (userConversationIds.Count == 0)
        {
            return [];
        }

        // 2. Load DM conversations from those IDs (filtered by project scope)
        var userConversationGuids = userConversationIds.Select(id => id.ToGuid()).ToList();
        var dmConversations = await (conversationRepo.FindAsync(
            c => c.TenantId == request.TenantId
                && c.ConversationType == ConversationType.DirectMessage
                && c.IsActive
                && c.ProjectId == request.ProjectId
                && userConversationGuids.Contains((Guid)c.Id),
            cancellationToken)).ConfigureAwait(false);

        var dmList = dmConversations.ToList();
        if (dmList.Count == 0)
        {
            return [];
        }

        // 2b. Load DirectMessage entities for status & filter out Concluded/Archived
        var dmEntityGuids = dmList.Select(d => d.Id.ToGuid()).ToList();
        var dmEntities = await (dmRepo.FindAsync(
            d => dmEntityGuids.Contains((Guid)d.Id),
            cancellationToken)).ConfigureAwait(false);
        var dmEntitiesList = dmEntities.ToList();
        var activeDmIds = dmEntitiesList
            .Where(d => d.Status == DirectMessageStatus.Active)
            .Select(d => d.Id)
            .ToHashSet();
        dmList = dmList.Where(d => activeDmIds.Contains(d.Id)).ToList();
        if (dmList.Count == 0) return [];
        var dmStatusMap = dmEntitiesList.ToDictionary(
            d => d.Id,
            d => (ChannelStatus)(int)d.Status);

        // 3. Load all members for these DM conversations
        var dmIds = dmList.Select(d => d.Id).ToList();
        var dmGuids = dmIds.Select(id => id.ToGuid()).ToList();
        var allMembers = await (memberRepo.FindAsync(
            m => dmGuids.Contains((Guid)m.ConversationId) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var membersByConversation = allMembers
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Load users for display names
        var allUserIds = allMembers.Select(m => m.UserId).Distinct().ToList();
        var allUserGuids = allUserIds.Select(id => id.ToGuid()).ToList();
        var users = await (userRepo.FindAsync(
            u => allUserGuids.Contains((Guid)u.Id),
            cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        // 5. Load last messages per DM
        var allMessages = await (messageRepo.FindAsync(
            m => m.ConversationId.HasValue && dmGuids.Contains((Guid)m.ConversationId.Value) && m.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var lastMessageByConversation = allMessages
            .Where(m => m.ConversationId.HasValue)
            .GroupBy(m => m.ConversationId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt.ToDateTimeOffset()).First());

        // 6. Load read statuses
        var readStatuses = await (readStatusRepo.FindAsync(
            rs => rs.UserId == request.UserId,
            cancellationToken)).ConfigureAwait(false);
        var readStatusMap = readStatuses.ToDictionary(
            rs => rs.ConversationId,
            rs => rs.LastReadAt?.ToDateTimeOffset());

        // 7. Build DMDto list
        var results = new List<DMDto>();
        foreach (var dm in dmList)
        {
            var members = membersByConversation.TryGetValue(dm.Id, out var mList) ? mList : [];
            var otherMember = members.FirstOrDefault(m => m.UserId != request.UserId);

            if (otherMember is null)
            {
                continue;
            }

            var otherUser = userMap.TryGetValue(otherMember.UserId, out var u) ? u : null;
            var lastMessage = lastMessageByConversation.TryGetValue(dm.Id, out var msg) ? msg : null;

            // Unread count
            var lastReadAt = readStatusMap.TryGetValue(dm.Id, out var readAt) ? readAt : null;
            var conversationMessages = allMessages
                .Where(m => m.ConversationId.HasValue && m.ConversationId.Value == dm.Id)
                .ToList();
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

            MessageDto? lastMessageDto = null;
            if (lastMessage is not null)
            {
                var hasSender = userMap.TryGetValue(lastMessage.SenderId, out var sender);
                var senderRef = new UserRefDto(
                    Id: lastMessage.SenderId.ToString(),
                    Name: hasSender ? sender!.DisplayName : "Unknown",
                    AvatarUrl: hasSender ? sender!.AvatarUrl : null);
                lastMessageDto = new MessageDto(
                    Id: lastMessage.Id.ToString(),
                    ConversationId: dm.Id.ToString(),
                    Sender: senderRef,
                    Type: lastMessage.Type,
                    Content: lastMessage.Content,
                    References: lastMessage.References,
                    ReplyToId: lastMessage.ReplyToId?.ToString(),
                    IsEdited: lastMessage.IsEdited,
                    CreatedAt: lastMessage.CreatedAt.ToDateTimeOffset(),
                    UpdatedAt: lastMessage.UpdatedAt.ToDateTimeOffset());
            }

            var otherUserRef = new UserRefDto(
                Id: otherMember.UserId.ToString(),
                Name: otherUser?.DisplayName ?? "Unknown",
                AvatarUrl: otherUser?.AvatarUrl);
            var dmStatus = dmStatusMap.TryGetValue(dm.Id, out var s) ? s : (ChannelStatus?)null;
            results.Add(new DMDto(
                Id: dm.Id.ToString(),
                OtherUser: otherUserRef,
                UnreadCount: unreadCount,
                LastMessage: lastMessageDto,
                CreatedAt: dm.CreatedAt.ToDateTimeOffset(),
                UpdatedAt: dm.UpdatedAt.ToDateTimeOffset(),
                Status: dmStatus));
        }

        return results.OrderByDescending(d => d.LastMessage?.CreatedAt ?? d.CreatedAt);
    }
}
