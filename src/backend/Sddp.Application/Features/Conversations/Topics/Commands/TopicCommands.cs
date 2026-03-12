using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Conversations.Topics.Commands;

/// <summary>
/// create
/// </summary>
public sealed record CreateTopicCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId ForumId,
    GlobalUniqueId AuthorId,
    string Title,
    string? InitialMessageContent) : ICommand<TopicDetailDto?>, IAuditableRequest<TopicDetailDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TopicDetailDto? response) => AuditLog;
}

public sealed class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, TopicDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTopicCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDetailDto?> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var forumRepo = _unitOfWork.Repository<Forum>();
        var messageRepo = _unitOfWork.Repository<Message>();
        var userRepo = _unitOfWork.Repository<User>();

        var forum = await (forumRepo.GetByIdAsync(request.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != request.TenantId || !forum.IsActive)
        {
            return null;
        }

        var author = await (userRepo.GetByIdAsync(request.AuthorId, cancellationToken)).ConfigureAwait(false);
        if (author is null || !author.IsActive)
        {
            return null;
        }

        var topic = new Topic(request.ForumId, request.AuthorId, request.Title);
        await (topicRepo.AddAsync(topic, cancellationToken)).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.InitialMessageContent))
        {
            var message = Message.CreateForTopic(
                request.TenantId,
                forum.ProjectId,
                topic.Id,
                request.AuthorId,
                MessageType.Proposal,
                request.InitialMessageContent);

            await (messageRepo.AddAsync(message, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.AuthorId,
            Action: "create",
            ResourceType: "topic",
            ResourceId: topic.Id,
            Payload: new { request.Title },
            TenantId: request.TenantId,
            ProjectId: null);

        return await (TopicHelpers.BuildTopicDetailAsync(_unitOfWork, request.TenantId, topic, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// update
/// </summary>
public sealed record UpdateTopicCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId,
    string Title) : ICommand<TopicDto?>, IAuditableRequest<TopicDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TopicDto? response) => AuditLog;
}

public sealed class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, TopicDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTopicCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDto?> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
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

        var updateResult = topic.UpdateTitle(request.Title);
        updateResult.EnsureSuccess("UPDATE_FAILED");

        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
            ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
            : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

        var messageCount = await (messageRepo.CountAsync(
            m => m.TopicId == request.TopicId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "update",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { request.Title },
            TenantId: request.TenantId,
            ProjectId: null);

        return TopicMapping.MapTopicDto(topic, authorRef, messageCount);
    }
}

/// <summary>
///
/// </summary>
public sealed record CloseTopicCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId,
    GlobalUniqueId? DecisionSpecId) : ICommand<TopicDto?>, IAuditableRequest<TopicDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TopicDto? response) => AuditLog;
}

public sealed class CloseTopicCommandHandler : IRequestHandler<CloseTopicCommand, TopicDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public CloseTopicCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDto?> Handle(CloseTopicCommand request, CancellationToken cancellationToken)
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

        var closeResult = topic.Close(request.DecisionSpecId);
        closeResult.EnsureSuccess("CLOSE_FAILED");

        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
            ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
            : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

        var messageCount = await (messageRepo.CountAsync(
            m => m.TopicId == request.TopicId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "close",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { topic.Title, ForumId = topic.ForumId.ToString(), DecisionSpecId = request.DecisionSpecId?.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return TopicMapping.MapTopicDto(topic, authorRef, messageCount);
    }
}

/// <summary>
///
/// </summary>
public sealed record ReopenTopicCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId) : ICommand<TopicDto?>, IAuditableRequest<TopicDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TopicDto? response) => AuditLog;
}

public sealed class ReopenTopicCommandHandler : IRequestHandler<ReopenTopicCommand, TopicDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReopenTopicCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDto?> Handle(ReopenTopicCommand request, CancellationToken cancellationToken)
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

        var reopenResult = topic.Reopen();
        reopenResult.EnsureSuccess("REOPEN_FAILED");

        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
            ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
            : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

        var messageCount = await (messageRepo.CountAsync(
            m => m.TopicId == request.TopicId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "reopen",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { topic.Title, ForumId = topic.ForumId.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return TopicMapping.MapTopicDto(topic, authorRef, messageCount);
    }
}

/// <summary>
///
/// </summary>
public sealed record ArchiveTopicCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId) : ICommand<TopicDto?>, IAuditableRequest<TopicDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TopicDto? response) => AuditLog;
}

public sealed class ArchiveTopicCommandHandler : IRequestHandler<ArchiveTopicCommand, TopicDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveTopicCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TopicDto?> Handle(ArchiveTopicCommand request, CancellationToken cancellationToken)
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

        var archiveResult = topic.Archive();
        archiveResult.EnsureSuccess("ARCHIVE_FAILED");

        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        var users = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var userMap = users.ToDictionary(u => u.Id, u => u);

        var authorRef = userMap.TryGetValue(topic.AuthorId, out var author)
            ? new UserRefDto(author.Id.ToString(), author.DisplayName, author.AvatarUrl)
            : new UserRefDto(topic.AuthorId.ToString(), "Unknown", null);

        var messageCount = await (messageRepo.CountAsync(
            m => m.TopicId == request.TopicId && m.IsActive,
            cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "archive",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { topic.Title, ForumId = topic.ForumId.ToString() },
            TenantId: request.TenantId,
            ProjectId: null);

        return TopicMapping.MapTopicDto(topic, authorRef, messageCount);
    }
}

/// <summary>
///
/// </summary>
public sealed record ToggleTopicPinCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class ToggleTopicPinCommandHandler : IRequestHandler<ToggleTopicPinCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleTopicPinCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ToggleTopicPinCommand request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var forumRepo = _unitOfWork.Repository<Forum>();

        var topic = await (topicRepo.GetByIdAsync(request.TopicId, cancellationToken)).ConfigureAwait(false);
        if (topic is null || !topic.IsActive)
        {
            throw new SddpException("PIN_FAILED", "Topic not found");
        }

        var forum = await (forumRepo.GetByIdAsync(topic.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != request.TenantId || !forum.IsActive)
        {
            throw new SddpException("PIN_FAILED", "Forum not found");
        }

        var isPinned = topic.TogglePin();
        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "toggle_pin",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { IsPinned = isPinned },
            TenantId: request.TenantId,
            ProjectId: null);

        return isPinned;
    }
}

/// <summary>
/// lock
/// </summary>
public sealed record ToggleTopicLockCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class ToggleTopicLockCommandHandler : IRequestHandler<ToggleTopicLockCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleTopicLockCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ToggleTopicLockCommand request, CancellationToken cancellationToken)
    {
        var topicRepo = _unitOfWork.Repository<Topic>();
        var forumRepo = _unitOfWork.Repository<Forum>();

        var topic = await (topicRepo.GetByIdAsync(request.TopicId, cancellationToken)).ConfigureAwait(false);
        if (topic is null || !topic.IsActive)
        {
            throw new SddpException("LOCK_FAILED", "Topic not found");
        }

        var forum = await (forumRepo.GetByIdAsync(topic.ForumId, cancellationToken)).ConfigureAwait(false);
        if (forum is null || forum.TenantId != request.TenantId || !forum.IsActive)
        {
            throw new SddpException("LOCK_FAILED", "Forum not found");
        }

        var isLocked = topic.ToggleLock();
        await (topicRepo.UpdateAsync(topic, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: null,
            Action: "toggle_lock",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { IsLocked = isLocked },
            TenantId: request.TenantId,
            ProjectId: null);

        return isLocked;
    }
}

/// <summary>
/// message
/// </summary>
public sealed record PostTopicMessageCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId TopicId,
    GlobalUniqueId SenderUserId,
    MessageType Type,
    string Content,
    string[]? References,
    GlobalUniqueId? ReplyToId) : ICommand<ForumMessageDto?>, IAuditableRequest<ForumMessageDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(ForumMessageDto? response) => AuditLog;
}

public sealed class PostTopicMessageCommandHandler : IRequestHandler<PostTopicMessageCommand, ForumMessageDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public PostTopicMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ForumMessageDto?> Handle(PostTopicMessageCommand request, CancellationToken cancellationToken)
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

        if (!topic.CanAddMessage())
        {
            throw new SddpException("POST_FAILED", "Cannot add message to closed, archived, or locked topic");
        }

        var sender = await (userRepo.GetByIdAsync(request.SenderUserId, cancellationToken)).ConfigureAwait(false);
        if (sender is null || !sender.IsActive)
        {
            throw new SddpException("POST_FAILED", "Sender not found");
        }

        if (request.ReplyToId.HasValue)
        {
            var replyToMessage = await (messageRepo.GetByIdAsync(request.ReplyToId.Value, cancellationToken)).ConfigureAwait(false);
            if (replyToMessage is null || replyToMessage.TopicId != request.TopicId)
            {
                throw new SddpException("POST_FAILED", "Invalid reply-to message");
            }
        }

        var message = Message.CreateForTopic(
            request.TenantId,
            forum.ProjectId,
            request.TopicId,
            request.SenderUserId,
            request.Type,
            request.Content,
            request.References,
            request.ReplyToId);

        await (messageRepo.AddAsync(message, cancellationToken)).ConfigureAwait(false);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.SenderUserId,
            Action: "post_message",
            ResourceType: "topic",
            ResourceId: request.TopicId,
            Payload: new { MessageId = message.Id.ToString(), MessageType = request.Type.ToString(), topic.Title },
            TenantId: request.TenantId,
            ProjectId: null);

        return TopicMapping.MapMessageDto(message, new UserRefDto(sender.Id.ToString(), sender.DisplayName, sender.AvatarUrl));
    }
}
