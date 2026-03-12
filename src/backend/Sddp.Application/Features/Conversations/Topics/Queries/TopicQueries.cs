using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Topics.Queries;

/// <summary>
/// forum get ()
/// </summary>
public sealed record GetTopicsByForumIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ForumId,
    int Page,
    int PageSize,
    TopicStatus? Status) : IQuery<TopicsPageDto>;

public sealed class GetTopicsByForumIdQueryHandler : IRequestHandler<GetTopicsByForumIdQuery, TopicsPageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTopicsByForumIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicsPageDto> Handle(GetTopicsByForumIdQuery request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();
        var forumRepo = _unitOfWork.Repository<Forum>();

        var forum = await (forumRepo.GetByIdAsync(request.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != request.TenantId || !forum.IsActive)
        {
            return new TopicsPageDto([], 0, request.Page, request.PageSize, false);
        }

        var (sortedTopics, totalCount) = await (topicRepo.FindPagedAsync(
            t => t.ForumId == request.ForumId
                && (request.Status == null || t.Status == request.Status),
            request.Page, request.PageSize,
            q => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CreatedAt),
            cancellationToken)).ConfigureAwait(false);

        //
        var authorIds = sortedTopics.Select(t => t.AuthorId).Distinct().ToList();
        var authors = authorIds.Count == 0
            ? Array.Empty<User>()
            : await (userRepo.FindAsync(u => authorIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false);
        var userMap = authors.ToDictionary(u => u.Id, u => u);

        // Batch load message counts for paged topics only
        var topicGuids = sortedTopics.Select(t => t.Id.ToGuid()).ToList();
        var allTopicMessages = topicGuids.Count == 0
            ? Array.Empty<Message>()
            : await (messageRepo.FindAsync(
                m => m.TopicId.HasValue && topicGuids.Contains((Guid)m.TopicId.Value) && m.IsActive,
                cancellationToken)).ConfigureAwait(false);
        var messageCountByTopic = allTopicMessages
            .GroupBy(m => m.TopicId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<TopicDto>();
        foreach (var topic in sortedTopics)
        {
            var messageCount = messageCountByTopic.TryGetValue(topic.Id, out var count) ? count : 0;

            var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
                ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
                : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

            result.Add(TopicMapping.MapTopicDto(topic, authorRef, messageCount));
        }

        var hasMore = (request.Page * request.PageSize) < totalCount;

        return new TopicsPageDto(result, totalCount, request.Page, request.PageSize, hasMore);
    }
}

/// <summary>
/// get (ID)
/// </summary>
public sealed record GetTopicByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId) : IQuery<TopicDetailDto?>;

public sealed class GetTopicByIdQueryHandler : IRequestHandler<GetTopicByIdQuery, TopicDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTopicByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDetailDto?> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var topic = await (topicRepo.GetByIdAsync(request.TopicId, cancellationToken)).ConfigureAwait(false);
        if (topic is null || !topic.IsActive)
        {
            return null;
        }

        return await (TopicHelpers.BuildTopicDetailAsync(_unitOfWork, request.TenantId, topic, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// message get ()
/// </summary>
public sealed record GetTopicMessagesQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId,
    DateTimeOffset? Cursor,
    int Limit) : IQuery<ForumMessagesPageDto?>;

public sealed class GetTopicMessagesQueryHandler : IRequestHandler<GetTopicMessagesQuery, ForumMessagesPageDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTopicMessagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ForumMessagesPageDto?> Handle(GetTopicMessagesQuery request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var forumRepo = _unitOfWork.Repository<Forum>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();

        var topic = await (topicRepo.GetByIdAsync(request.TopicId, cancellationToken)).ConfigureAwait(false);
        if (topic is null || !topic.IsActive)
        {
            return null;
        }

        var forum = await (forumRepo.GetByIdAsync(topic.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != request.TenantId || !forum.IsActive)
        {
            return null;
        }

        var messages = await (messageRepo.FindAsync(
            m => m.TopicId == request.TopicId
                && m.IsActive
                && (request.Cursor == null || m.CreatedAt.ToDateTimeOffset() < request.Cursor),
            cancellationToken)).ConfigureAwait(false);

        var sortedMessages = messages
            .OrderBy(m => m.CreatedAt.ToDateTimeOffset())
            .Take(request.Limit + 1)
            .ToList();

        var hasMore = sortedMessages.Count > request.Limit;
        var resultMessages = sortedMessages.Take(request.Limit).ToList();

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var messageDtos = resultMessages.Select(m =>
        {
            var hasSender = userMap.TryGetValue(m.SenderId, out var user);
            var senderRef = new UserRefDto(m.SenderId.ToString(), hasSender && user is not null ? user.DisplayName : "Unknown", hasSender && user is not null ? user.AvatarUrl : null);
            return TopicMapping.MapMessageDto(m, senderRef);
        });

        var nextCursor = hasMore && resultMessages.Any()
            ? resultMessages.Last().CreatedAt.ToDateTimeOffset().ToString("O")
            : null;

        return new ForumMessagesPageDto(
            Messages: messageDtos,
            NextCursor: nextCursor,
            HasMore: hasMore);
    }
}
