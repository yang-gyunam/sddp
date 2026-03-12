using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Api.Hubs;
using Sddp.Application.Features.Conversations.Commands;
using Sddp.Application.Features.Conversations.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Conversation Messages controller (Message CRUD + SignalR broadcast)
/// </summary>
[Route("api/conversations")]
[Authorize]
public class ConversationMessagesController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IConversationHubService _hubService;

    public ConversationMessagesController(
        ISender sender,
        IConversationHubService hubService)
    {
        _sender = sender;
        _hubService = hubService;
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetConversationMessages(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        DateTimeOffset? cursorTime = null;
        if (!string.IsNullOrEmpty(cursor) && DateTimeOffset.TryParse(cursor, out var parsedCursor))
        {
            cursorTime = parsedCursor;
        }

        var messagesPage = await _sender.Send(new GetConversationMessagesCommand(
            tenantId, projectId, conversationId, userId, cursorTime, limit), cancellationToken);

        if (messagesPage is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<ConversationMessagesPageDto>.Ok(messagesPage));
    }

    [HttpGet("{id:guid}/pinned")]
    public async Task<IActionResult> GetPinnedConversationMessages(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var messages = await _sender.Send(new GetPinnedConversationMessagesQuery(
            tenantId, projectId, conversationId, userId, limit), cancellationToken);

        if (messages is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<IEnumerable<ConversationMessageDto>>.Ok(messages));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> PostConversationMessage(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
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

        var result = await _sender.Send(new PostConversationMessageCommand(
            tenantId,
            projectId,
            conversationId,
            userId,
            dto.Type,
            dto.Content,
            dto.References,
            replyToId), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.PostFailed, "Failed to post message"));
        }

        var messageDto = result.Message;

        // Broadcast via SignalR to all conversation members
        await _hubService.BroadcastNewMessageAsync(
            id,
            new MessageDto(
                messageDto.Id, messageDto.ConversationId, messageDto.Sender,
                messageDto.Type, messageDto.Content, messageDto.References ?? Array.Empty<string>(),
                messageDto.ReplyToId, messageDto.IsEdited, messageDto.CreatedAt, messageDto.UpdatedAt));

        // Broadcast MemberJoined if auto-participation occurred
        if (result.IsNewParticipant && result.NewParticipantUserId is not null)
        {
            await _hubService.BroadcastMemberJoinedAsync(
                id, result.NewParticipantUserId, result.NewParticipantDisplayName ?? "Unknown");
        }

        return Created($"/api/conversations/{id}/messages/{messageDto.Id}", ApiResponse<ConversationMessageDto>.Ok(messageDto));
    }

    /// <summary>
    /// Edit a message in a conversation
    /// </summary>
    [HttpPut("{id:guid}/messages/{messageId:guid}")]
    [Authorize]
    public async Task<IActionResult> EditConversationMessage(
        string id,
        string messageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] EditMessageRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        if (!GlobalUniqueId.TryParse(messageId, out var parsedMessageId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid message ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new EditConversationMessageCommand(
            tenantId, projectId, conversationId, parsedMessageId, userId, dto.Content), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        // Broadcast via SignalR
        await _hubService.BroadcastMessageEditedAsync(
            id,
            new MessageDto(
                result.Id, result.ConversationId, result.Sender,
                result.Type, result.Content, result.References ?? Array.Empty<string>(),
                result.ReplyToId, result.IsEdited, result.CreatedAt, result.UpdatedAt));

        return Ok(ApiResponse<ConversationMessageDto>.Ok(result));
    }

    /// <summary>
    /// Delete a message from a conversation (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}/messages/{messageId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteConversationMessage(
        string id,
        string messageId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        if (!GlobalUniqueId.TryParse(messageId, out var parsedMessageId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid message ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new DeleteConversationMessageCommand(
            tenantId, projectId, conversationId, parsedMessageId, userId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        // Broadcast via SignalR
        await _hubService.BroadcastMessageDeletedAsync(id, messageId);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }
}
