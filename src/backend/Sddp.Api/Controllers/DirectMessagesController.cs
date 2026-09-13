using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Application.Features.Conversations.Commands;
using Sddp.Application.Features.Conversations.Queries;
using Sddp.Domain.Entities;

namespace Sddp.Api.Controllers;

/// <summary>
/// Direct Messages controller
/// </summary>
[Route("api/conversations")]
[Authorize]
public class DirectMessagesController : BaseApiController
{
    private readonly ISender _sender;

    public DirectMessagesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get DM list for the current user
    /// </summary>
    [HttpGet("dm")]
    [ResourceScopeExempt("Personal listing keyed by the caller's own user id, with no conversation named in the route.")]
    public async Task<IActionResult> GetDirectMessages(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 20,
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

        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader) && GlobalUniqueId.TryParse(projectIdHeader, out var parsedProjectId))
        {
            projectId = parsedProjectId;
        }

        var dms = await _sender.Send(new GetDirectMessagesQuery(tenantId, userId, projectId), cancellationToken);
        // DM in-memory (user DM)
        var safePageNumber = pageNumber ?? page ?? 1;
        safePageNumber = safePageNumber < 1 ? 1 : safePageNumber;
        var safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var items = dms.ToList();
        var skip = (safePageNumber - 1) * safePageSize;
        var pagedItems = items.Skip(skip).Take(safePageSize).ToList();
        var pagedResult = PagedResult<DMDto>.Create(pagedItems, items.Count, safePageNumber, safePageSize);
        return Ok(ApiResponse<PagedResult<DMDto>>.Ok(pagedResult));
    }

    /// <summary>
    /// Get or create DM with a target user
    /// </summary>
    [HttpPost("dm/{targetUserId}")]
    [ResourceScopeExempt("The route names the other participant, a User, which carries no project; the conversation may not exist yet.")]
    [RequirePermission(PermissionCodes.ConversationCreate)]
    [Authorize]
    public async Task<IActionResult> GetOrCreateDirectMessage(
        string targetUserId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
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

        if (!GlobalUniqueId.TryParse(targetUserId, out var parsedTargetUserId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid target user ID format"));
        }

        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader) && GlobalUniqueId.TryParse(projectIdHeader, out var parsedProjectId))
        {
            projectId = parsedProjectId;
        }

        var result = await _sender.Send(new GetOrCreateDirectMessageCommand(
            tenantId, userId, parsedTargetUserId, projectId), cancellationToken);

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Send DM invitation to a target user (DM is created on accept)
    /// </summary>
    [HttpPost("dm/invitations/{targetUserId}")]
    [ResourceScopeExempt("The route names the invited User, which carries no project, and no conversation exists to resolve from.")]
    [RequirePermission(PermissionCodes.ConversationCreate)]
    [Authorize]
    public async Task<IActionResult> SendDirectMessageInvitation(
        string targetUserId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(targetUserId, out var parsedTargetUserId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid target user ID format"));

        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader) && GlobalUniqueId.TryParse(projectIdHeader, out var parsedProjectId))
        {
            projectId = parsedProjectId;
        }

        var result = await _sender.Send(
            new SendDirectMessageInvitationCommand(tenantId, userId, parsedTargetUserId, projectId),
            cancellationToken);

        return Ok(ApiResponse<DirectMessageInvitationResultDto>.Ok(result));
    }

    /// <summary>
    /// Accept DM invitation and create/get DM conversation
    /// </summary>
    [HttpPost("dm/invitations/{notificationId}/accept")]
    [ResourceScopeExempt("The route names a Notification, which carries no project; the handler accepts it only for its own recipient.")]
    [RequirePermission(PermissionCodes.ConversationCreate)]
    [Authorize]
    public async Task<IActionResult> AcceptDirectMessageInvitation(
        string notificationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(notificationId, out var parsedNotificationId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid notification ID format"));

        var result = await _sender.Send(
            new AcceptDirectMessageInvitationCommand(tenantId, parsedNotificationId, userId),
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Direct message invitation not found"));

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Reject DM invitation
    /// </summary>
    [HttpPost("dm/invitations/{notificationId}/reject")]
    [ResourceScopeExempt("The route names a Notification, which carries no project; the handler accepts it only for its own recipient.")]
    [RequirePermission(PermissionCodes.ConversationCreate)]
    [Authorize]
    public async Task<IActionResult> RejectDirectMessageInvitation(
        string notificationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(notificationId, out var parsedNotificationId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid notification ID format"));

        var rejected = await _sender.Send(
            new RejectDirectMessageInvitationCommand(tenantId, parsedNotificationId, userId),
            cancellationToken);

        if (!rejected)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Direct message invitation not found"));

        return Ok(ApiResponse<object>.Ok(new { message = "Invitation rejected" }));
    }

    /// <summary>
    /// Conclude a DM (Active → Concluded)
    /// </summary>
    [HttpPost("dm/{id}/conclude")]
    [ResourceScope<DirectMessage>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.ConversationClose)]
    [Authorize]
    public async Task<IActionResult> ConcludeDirectMessage(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ConcludeDmRequest? body,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(id, out var dmId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid DM ID format"));

        GlobalUniqueId? decisionSpecId = null;
        if (!string.IsNullOrWhiteSpace(body?.DecisionSpecId)
            && GlobalUniqueId.TryParse(body.DecisionSpecId, out var parsedSpecId))
        {
            decisionSpecId = parsedSpecId;
        }

        var result = await _sender.Send(
            new ConcludeDirectMessageCommand(tenantId, dmId, userId, decisionSpecId), cancellationToken);

        if (result is null) return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Direct message not found"));
        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Reopen a DM (Concluded → Active)
    /// </summary>
    [HttpPost("dm/{id}/reopen")]
    [ResourceScope<DirectMessage>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.ConversationClose)]
    [Authorize]
    public async Task<IActionResult> ReopenDirectMessage(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(id, out var dmId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid DM ID format"));

        var result = await _sender.Send(
            new ReopenDirectMessageCommand(tenantId, dmId, userId), cancellationToken);

        if (result is null) return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Direct message not found"));
        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Archive a DM (Concluded → Archived)
    /// </summary>
    [HttpPost("dm/{id}/archive")]
    [ResourceScope<DirectMessage>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.ConversationClose)]
    [Authorize]
    public async Task<IActionResult> ArchiveDirectMessage(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        if (!GlobalUniqueId.TryParse(id, out var dmId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid DM ID format"));

        var result = await _sender.Send(
            new ArchiveDirectMessageCommand(tenantId, dmId, userId), cancellationToken);

        if (result is null) return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Direct message not found"));
        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }
}

/// <summary>
/// Request body for conclude DM endpoint
/// </summary>
public class ConcludeDmRequest
{
    public string? DecisionSpecId { get; set; }
}
