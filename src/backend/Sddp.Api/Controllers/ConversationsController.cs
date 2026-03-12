using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Conversations.Commands;
using Sddp.Application.Features.Conversations.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Conversations controller — Core CRUD, Search, Settings, Members
/// </summary>
[Route("api/conversations")]
[Authorize]
public class ConversationsController : BaseApiController
{
    private readonly ISender _sender;

    public ConversationsController(ISender sender)
    {
        _sender = sender;
    }

    // ============================================
    // Search
    // ============================================

    /// <summary>
    /// Search conversations by name or description (lightweight, for autocomplete)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchConversations(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string q = "",
        [FromQuery] int limit = 15,
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

        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(ApiResponse<IEnumerable<ConversationSearchResultDto>>.Ok([]));
        }

        var result = await _sender.Send(
            new SearchConversationsQuery(tenantId, projectId, q.Trim(), Math.Min(limit, 50)),
            cancellationToken);
        return Ok(ApiResponse<IEnumerable<ConversationSearchResultDto>>.Ok(result));
    }

    // ============================================
    // Conversations (Channel / Forum / DirectMessage)
    // ============================================

    /// <summary>
    /// Get conversations for a specific project
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string? scope = null,
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

        var (resolvedPageNumber, resolvedPageSize) = NormalizePaging(pageNumber, page, pageSize);

        // If scope=global, return tenant-wide conversations (no project filter)
        if (string.Equals(scope, "global", StringComparison.OrdinalIgnoreCase))
        {
            var globalConversations = await _sender.Send(new GetGlobalConversationsQuery(tenantId, userId), cancellationToken);
            var pagedResult = CreatePagedResult(globalConversations, resolvedPageNumber, resolvedPageSize);
            return Ok(ApiResponse<PagedResult<ConversationSummaryDto>>.Ok(pagedResult));
        }

        // Otherwise, require projectId for project-scoped conversations
        var projectError = RequireProjectId(
            projectIdHeader,
            out var projectId,
            errorMessage: "Invalid X-Project-Id header (required for project-scoped conversations)");
        if (projectError is not null)
        {
            return projectError;
        }

        var conversations = await _sender.Send(new GetConversationsQuery(tenantId, projectId, userId), cancellationToken);
        var paged = CreatePagedResult(conversations, resolvedPageNumber, resolvedPageSize);
        return Ok(ApiResponse<PagedResult<ConversationSummaryDto>>.Ok(paged));
    }

    /// <summary>
    /// Get a single conversation by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetConversationById(
        string id,
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

        if (!GlobalUniqueId.TryParse(id, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var result = await _sender.Send(new GetConversationByIdQuery(tenantId, conversationId, userId), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Get tenant-wide (global) conversations (no project association)
    /// </summary>
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobalConversations(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] int pageNumber = 1,
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

        var conversations = await _sender.Send(new GetGlobalConversationsQuery(tenantId, userId), cancellationToken);
        var pagedResult = CreatePagedResult(conversations, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<ConversationSummaryDto>>.Ok(pagedResult));
    }

    /// <summary>
    /// Get unread message counts for the current user
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadCounts(
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

        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader))
        {
            if (GlobalUniqueId.TryParse(projectIdHeader, out var parsedProjectId))
            {
                projectId = parsedProjectId;
            }
        }

        var result = await _sender.Send(new GetUnreadCountsQuery(tenantId, userId, projectId), cancellationToken);
        return Ok(ApiResponse<UnreadCountsDto>.Ok(result));
    }

    /// <summary>
    /// Create a new conversation
    /// Scope dto.Scope, X-Project-Id.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateConversation")]
    [Authorize]
    public async Task<IActionResult> CreateConversation(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateConversationRequestDto dto,
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

        // ProjectId is header-only in canonical contract.
        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader))
        {
            if (GlobalUniqueId.TryParse(projectIdHeader, out var parsedHeaderProjectId))
            {
                projectId = parsedHeaderProjectId;
            }
            else
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Header.InvalidProject, "Invalid X-Project-Id header"));
            }
        }

        var scope = dto.Scope ?? (projectId.HasValue ? ConversationScope.ProjectScoped : ConversationScope.TenantWide);
        if (scope == ConversationScope.ProjectScoped && !projectId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidScope, "ProjectScoped scope requires X-Project-Id header"));
        }

        if (scope == ConversationScope.TenantWide && projectId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidScope, "TenantWide scope must not include X-Project-Id header"));
        }

        var result = await _sender.Send(new CreateConversationCommand(
            tenantId,
            projectId,
            userId,
            dto.Name,
            dto.ConversationType,
            dto.Visibility,
            scope,
            dto.Description,
            dto.SortOrder), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create conversation"));
        }

        return Created($"/api/conversations/{result.Id}", ApiResponse<ConversationSummaryDto>.Ok(result));
    }

    // ============================================
    // Members
    // ============================================

    /// <summary>
    /// Get members of a conversation
    /// </summary>
    [HttpGet("{conversationId:guid}/members")]
    public async Task<IActionResult> GetConversationMembers(
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

        var result = await _sender.Send(new GetConversationMembersQuery(
            tenantId, parsedConversationId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<IEnumerable<ParticipantDto>>.Ok(result));
    }

    /// <summary>
    /// Add members to a conversation
    /// </summary>
    [HttpPost("{conversationId:guid}/members")]
    [Authorize]
    public async Task<IActionResult> AddConversationMembers(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] AddMembersDto dto,
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

        if (dto.UserIds is null || dto.UserIds.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidInput, "At least one user ID is required"));
        }

        var parsedUserIds = new List<GlobalUniqueId>();
        foreach (var uid in dto.UserIds)
        {
            if (!GlobalUniqueId.TryParse(uid, out var parsedUid))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, $"Invalid user ID format: {uid}"));
            }
            parsedUserIds.Add(parsedUid);
        }

        var result = await _sender.Send(new AddConversationMembersCommand(
            tenantId, parsedConversationId, userId, parsedUserIds), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ParticipantDto>>.Ok(result));
    }

    /// <summary>
    /// Get users that can be invited to a conversation
    /// </summary>
    [HttpGet("{conversationId:guid}/invitable-users")]
    public async Task<IActionResult> GetInvitableUsers(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string? search = null,
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

        var result = await _sender.Send(new GetInvitableUsersQuery(
            tenantId, parsedConversationId, userId, search), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<InvitableUserDto>>.Ok(result));
    }

    /// <summary>
    /// Remove a member from a conversation
    /// </summary>
    [HttpDelete("{conversationId:guid}/members/{targetUserId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveConversationMember(
        string conversationId,
        string targetUserId,
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

        if (!GlobalUniqueId.TryParse(targetUserId, out var parsedTargetUserId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid target user ID format"));
        }

        var result = await _sender.Send(new RemoveConversationMemberCommand(
            tenantId, parsedConversationId, userId, parsedTargetUserId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<object>.Ok(new { removed = true }));
    }

    // ============================================
    // Conversation Settings (Star/Mute)
    // ============================================

    /// <summary>
    /// Get user's starred conversations
    /// </summary>
    [HttpGet("starred")]
    public async Task<IActionResult> GetStarredConversations(
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

        var result = await _sender.Send(new GetStarredConversationsQuery(
            tenantId, userId), cancellationToken);

        return Ok(ApiResponse<IEnumerable<ConversationSummaryDto>>.Ok(result));
    }

    /// <summary>
    /// Toggle starred status for a conversation
    /// </summary>
    [HttpPost("{conversationId:guid}/star")]
    [Authorize]
    public async Task<IActionResult> ToggleStarred(
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

        var result = await _sender.Send(new ToggleStarredCommand(
            tenantId, parsedConversationId, userId), cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { isStarred = result.IsStarred }));
    }

    /// <summary>
    /// Toggle muted status for a conversation
    /// </summary>
    [HttpPost("{conversationId:guid}/mute")]
    [Authorize]
    public async Task<IActionResult> ToggleMuted(
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

        var result = await _sender.Send(new ToggleMutedCommand(
            tenantId, parsedConversationId, userId), cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { isMuted = result.IsMuted }));
    }

    /// <summary>
    /// Mark conversation as read
    /// </summary>
    [HttpPost("{conversationId:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] MarkAsReadRequest? body,
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

        DateTimeOffset? readUntil = null;
        if (!string.IsNullOrWhiteSpace(body?.ReadUntil))
        {
            if (!DateTimeOffset.TryParse(body.ReadUntil, out var parsed))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidDate, "Invalid readUntil format"));
            }

            readUntil = parsed;
        }

        var result = await _sender.Send(new MarkConversationAsReadCommand(
            tenantId, parsedConversationId, userId, readUntil), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Conversation not found"));
        }

        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }

    /// <summary>
    /// Get user conversation settings (starred, muted, lastReadAt)
    /// </summary>
    [HttpGet("{conversationId:guid}/settings")]
    public async Task<IActionResult> GetUserConversationSettings(
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

        var result = await _sender.Send(new GetUserConversationSettingsQuery(
            tenantId, parsedConversationId, userId), cancellationToken);

        return Ok(ApiResponse<UserConversationSettingsDto>.Ok(result));
    }

    // ============================================
    // Linked Requirements
    // ============================================

    /// <summary>
    /// Get requirements linked to a conversation
    /// </summary>
    [HttpGet("{conversationId:guid}/linked-requirements")]
    public async Task<IActionResult> GetLinkedRequirements(
        string conversationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(conversationId, out var parsedConversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid conversation ID format"));
        }

        var result = await _sender.Send(new GetLinkedRequirementsQuery(
            tenantId, parsedConversationId), cancellationToken);

        return Ok(ApiResponse<IEnumerable<LinkedRequirementDto>>.Ok(result));
    }

    private static (int PageNumber, int PageSize) NormalizePaging(int? pageNumber, int? page, int pageSize)
    {
        var safePageNumber = pageNumber ?? page ?? 1;
        safePageNumber = safePageNumber < 1 ? 1 : safePageNumber;
        var safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        return (safePageNumber, safePageSize);
    }

    /// <summary>
    /// / in-memory.
    /// DTO DB change.
    /// tenant conversation.
    /// </summary>
    private static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> source,
        int pageNumber,
        int pageSize)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var items = source.ToList();
        var skip = (safePageNumber - 1) * safePageSize;
        var pagedItems = items.Skip(skip).Take(safePageSize).ToList();
        return PagedResult<T>.Create(pagedItems, items.Count, safePageNumber, safePageSize);
    }
}

public class MarkAsReadRequest
{
    public string? ReadUntil { get; set; }
}
