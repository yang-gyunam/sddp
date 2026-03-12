using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Topics;

internal static class TopicHelpers
{
    internal static readonly GlobalUniqueId SystemProjectId =
        GlobalUniqueId.FromGuid(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    internal static async Task<TopicDetailDto?> BuildTopicDetailAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        Topic topic,
        CancellationToken cancellationToken)
    {
        var forumRepo = unitOfWork.Repository<Forum>();
        var messageRepo = unitOfWork.Repository<Message>();
        var userRepo = unitOfWork.Repository<User>();

        var forum = await (forumRepo.GetByIdAsync(topic.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != tenantId || !forum.IsActive)
        {
            return null;
        }

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
            ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
            : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

        var messages = await (messageRepo.FindAsync(
            m => m.TopicId == topic.Id && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        var recentMessages = messages
            .OrderByDescending(m => m.CreatedAt.ToDateTimeOffset())
            .Take(10)
            .Select(m =>
            {
                var hasSender = userMap.TryGetValue(m.SenderId, out var sender);
                var senderRef = new UserRefDto(m.SenderId.ToString(), hasSender && sender is not null ? sender.DisplayName : "Unknown", hasSender && sender is not null ? sender.AvatarUrl : null);
                return TopicMapping.MapMessageDto(m, senderRef);
            })
            .Reverse()
            .ToList();

        var messageCount = await (messageRepo.CountAsync(
            m => m.TopicId == topic.Id && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        return new TopicDetailDto(
            Id: topic.Id.ToString(),
            ForumId: topic.ForumId.ToString(),
            ForumName: forum.Name,
            Title: topic.Title,
            Author: authorRef,
            Status: topic.Status,
            IsPinned: topic.IsPinned,
            IsLocked: topic.IsLocked,
            DecisionSpecId: topic.DecisionSpecId?.ToString(),
            RecentMessages: recentMessages,
            MessageCount: messageCount,
            CreatedAt: topic.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: topic.UpdatedAt.ToDateTimeOffset());
    }
}
