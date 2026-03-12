using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations;

internal static class ConversationMapping
{
    internal static ConversationSummaryDto MapConversationSummaryDto(
        Conversation conversation,
        int memberCount,
        int unreadCount,
        DateTimeOffset? lastMessageAt)
    {
        var channelStatus = conversation switch
        {
            Channel ch => ch.Status,
            DirectMessage dm => (ChannelStatus)(int)dm.Status,
            _ => (ChannelStatus?)null
        };
        var decisionSpecId = conversation switch
        {
            Channel ch2 => ch2.DecisionSpecId?.ToString(),
            DirectMessage dm2 => dm2.DecisionSpecId?.ToString(),
            _ => null
        };

        return new ConversationSummaryDto(
            Id: conversation.Id.ToString(),
            ProjectId: conversation.ProjectId?.ToString(),
            Name: conversation.Name,
            Description: conversation.Description,
            ConversationType: conversation.ConversationType,
            Visibility: conversation.Visibility,
            Scope: conversation.Scope,
            IsArchived: conversation.IsArchived,
            SortOrder: conversation.SortOrder,
            MemberCount: memberCount,
            UnreadCount: unreadCount,
            LastMessageAt: lastMessageAt,
            ChannelStatus: channelStatus,
            DecisionSpecId: decisionSpecId);
    }

    internal static ParticipantDto MapParticipantDto(ConversationMember member, UserRefDto user)
    {
        return new ParticipantDto(
            Id: member.Id.ToString(),
            User: user,
            Type: member.Type,
            Role: member.Role.ToString(),
            JoinedAt: member.JoinedAt.ToDateTimeOffset(),
            LeftAt: member.IsActive ? null : member.UpdatedAt.ToDateTimeOffset(),
            IsActive: member.IsActive);
    }

    internal static ConversationMessageDto MapConversationMessageDto(Message message, UserRefDto sender)
    {
        return new ConversationMessageDto(
            Id: message.Id.ToString(),
            ConversationId: message.ConversationId?.ToString() ?? string.Empty,
            Sender: sender,
            Type: message.Type,
            Content: message.Content,
            References: message.References,
            ReplyToId: message.ReplyToId?.ToString(),
            IsEdited: message.IsEdited,
            CreatedAt: message.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: message.UpdatedAt.ToDateTimeOffset());
    }
}
