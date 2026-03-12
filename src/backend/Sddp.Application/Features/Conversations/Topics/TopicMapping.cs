using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Topics;

internal static class TopicMapping
{
    internal static TopicDto MapTopicDto(Topic topic, UserRefDto author, int messageCount)
    {
        return new TopicDto(
            Id: topic.Id.ToString(),
            ForumId: topic.ForumId.ToString(),
            Title: topic.Title,
            Author: author,
            Status: topic.Status,
            IsPinned: topic.IsPinned,
            IsLocked: topic.IsLocked,
            DecisionSpecId: topic.DecisionSpecId?.ToString(),
            MessageCount: messageCount,
            CreatedAt: topic.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: topic.UpdatedAt.ToDateTimeOffset());
    }

    internal static ForumMessageDto MapMessageDto(Message message, UserRefDto sender)
    {
        return new ForumMessageDto(
            Id: message.Id.ToString(),
            TopicId: message.TopicId?.ToString() ?? string.Empty,
            Sender: sender,
            Type: message.Type,
            Content: message.Content,
            References: message.References,
            ReplyToId: message.ReplyToId?.ToString(),
            IsEdited: message.IsEdited,
            IsPinned: message.IsPinned,
            CreatedAt: message.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: message.UpdatedAt.ToDateTimeOffset());
    }
}
