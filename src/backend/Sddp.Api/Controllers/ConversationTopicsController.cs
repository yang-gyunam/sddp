using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Conversations.Topics.Commands;
using Sddp.Application.Features.Conversations.Topics.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Topics controller
/// </summary>
[Route("api/conversations")]
[Authorize]
public class ConversationTopicsController : BaseApiController
{
    private readonly ISender _sender;

    public ConversationTopicsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Conversation Topic get
    /// </summary>
    [HttpGet("{conversationId:guid}/topics")]
    public async Task<IActionResult> GetTopics(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TopicStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ConversationId, "Invalid conversation ID format"));
        }

        var result = await _sender.Send(new GetTopicsByForumIdQuery(
            tenantId, parsedConversationId, page, pageSize, status), cancellationToken);

        return Ok(ApiResponse<TopicsPageDto>.Ok(result));
    }

    /// <summary>
    /// Topic create
    /// </summary>
    [HttpPost("{conversationId:guid}/topics")]
    [Authorize(Policy = "CanCreateConversation")]
    [Authorize]
    public async Task<IActionResult> CreateTopic(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateTopicRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ConversationId, "Invalid conversation ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.TitleRequired, "Topic title is required"));
        }

        var result = await _sender.Send(new CreateTopicCommand(
            tenantId, parsedConversationId, userId, dto.Title, dto.InitialMessageContent), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create topic"));
        }

        return Created($"/api/conversations/{conversationId}/topics/{result.Id}",
            ApiResponse<TopicDetailDto>.Ok(result));
    }

    /// <summary>
    /// Topic get
    /// </summary>
    [HttpGet("{conversationId:guid}/topics/{topicId:guid}")]
    public async Task<IActionResult> GetTopicById(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var result = await _sender.Send(new GetTopicByIdQuery(tenantId, parsedTopicId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<TopicDetailDto>.Ok(result));
    }

    /// <summary>
    /// Topic update
    /// </summary>
    [HttpPut("{conversationId:guid}/topics/{topicId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTopic(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateTopicRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.TitleRequired, "Topic title is required"));
        }

        var result = await _sender.Send(new UpdateTopicCommand(
            tenantId, parsedTopicId, dto.Title), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<TopicDto>.Ok(result));
    }

    /// <summary>
    /// Topic
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/close")]
    [Authorize(Policy = "CanCloseConversation")]
    [Authorize]
    public async Task<IActionResult> CloseTopic(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CloseTopicRequestDto? dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        GlobalUniqueId? decisionSpecId = null;
        if (!string.IsNullOrEmpty(dto?.DecisionSpecId))
        {
            if (!GlobalUniqueId.TryParse(dto.DecisionSpecId, out var specId))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.SpecId, "Invalid decision spec ID format"));
            }
            decisionSpecId = specId;
        }

        var result = await _sender.Send(new CloseTopicCommand(
            tenantId, parsedTopicId, decisionSpecId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<TopicDto>.Ok(result));
    }

    /// <summary>
    /// Topic
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/reopen")]
    [Authorize]
    public async Task<IActionResult> ReopenTopic(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var result = await _sender.Send(new ReopenTopicCommand(tenantId, parsedTopicId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<TopicDto>.Ok(result));
    }

    /// <summary>
    /// Topic
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/archive")]
    [Authorize]
    public async Task<IActionResult> ArchiveTopic(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var result = await _sender.Send(new ArchiveTopicCommand(tenantId, parsedTopicId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<TopicDto>.Ok(result));
    }

    /// <summary>
    /// Topic
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/pin")]
    [Authorize]
    public async Task<IActionResult> ToggleTopicPin(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var isPinned = await _sender.Send(new ToggleTopicPinCommand(tenantId, parsedTopicId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { isPinned }));
    }

    /// <summary>
    /// Topic lock
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/lock")]
    [Authorize]
    public async Task<IActionResult> ToggleTopicLock(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var isLocked = await _sender.Send(new ToggleTopicLockCommand(tenantId, parsedTopicId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { isLocked }));
    }

    /// <summary>
    /// Topic message get
    /// </summary>
    [HttpGet("{conversationId:guid}/topics/{topicId:guid}/messages")]
    public async Task<IActionResult> GetTopicMessages(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        DateTimeOffset? cursorTime = null;
        if (!string.IsNullOrEmpty(cursor) && DateTimeOffset.TryParse(cursor, out var parsedCursor))
        {
            cursorTime = parsedCursor;
        }

        var result = await _sender.Send(new GetTopicMessagesQuery(
            tenantId, parsedTopicId, cursorTime, limit), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Topic not found"));
        }

        return Ok(ApiResponse<ForumMessagesPageDto>.Ok(result));
    }

    /// <summary>
    /// Topic message
    /// </summary>
    [HttpPost("{conversationId:guid}/topics/{topicId:guid}/messages")]
    public async Task<IActionResult> PostTopicMessage(
        string conversationId,
        string topicId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(topicId, out var parsedTopicId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TopicId, "Invalid topic ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        GlobalUniqueId? replyToId = null;
        if (!string.IsNullOrEmpty(dto.ReplyToId))
        {
            if (!GlobalUniqueId.TryParse(dto.ReplyToId, out var parsedReplyToId))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ReplyTo, "Invalid reply-to ID format"));
            }
            replyToId = parsedReplyToId;
        }

        var result = await _sender.Send(new PostTopicMessageCommand(
            tenantId, parsedTopicId, userId, dto.Type, dto.Content, dto.References, replyToId), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.PostFailed, "Failed to post message"));
        }

        return Created($"/api/conversations/{conversationId}/topics/{topicId}/messages/{result.Id}",
            ApiResponse<ForumMessageDto>.Ok(result));
    }
}
