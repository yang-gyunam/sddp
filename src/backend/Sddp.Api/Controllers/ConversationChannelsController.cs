using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Conversations.Commands;

namespace Sddp.Api.Controllers;

/// <summary>
/// Channel lifecycle controller (Conclude/Reopen/Archive)
/// </summary>
[Route("api/conversations")]
[Authorize]
public class ConversationChannelsController : BaseApiController
{
    private readonly ISender _sender;

    public ConversationChannelsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Conclude a channel (Active → Concluded)
    /// </summary>
    [HttpPost("{conversationId:guid}/conclude")]
    [Authorize]
    public async Task<IActionResult> ConcludeChannel(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ConcludeChannelRequestDto? dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
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

        var result = await _sender.Send(new ConcludeChannelCommand(
            tenantId, parsedConversationId, userId, decisionSpecId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Channel not found"));
        }

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Reopen a channel (Concluded → Active)
    /// </summary>
    [HttpPost("{conversationId:guid}/reopen")]
    [Authorize]
    public async Task<IActionResult> ReopenChannel(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var result = await _sender.Send(new ReopenChannelCommand(
            tenantId, parsedConversationId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Channel not found"));
        }

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Archive a channel (Concluded → Archived)
    /// </summary>
    [HttpPost("{conversationId:guid}/archive")]
    [Authorize]
    public async Task<IActionResult> ArchiveChannel(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var result = await _sender.Send(new ArchiveChannelCommand(
            tenantId, parsedConversationId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Channel not found"));
        }

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }
}
