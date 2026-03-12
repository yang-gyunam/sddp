using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Users.Commands;
using Sddp.Application.Features.Users.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// user controller
/// </summary>
[Route("api/users")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _sender.Send(new GetUsersQuery(pageNumber, pageSize, search));
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetUserById(string id)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var user = await _sender.Send(new GetUserByIdQuery(userId));
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// tenant get (- permission)
    /// GET /api/users/tenant-members
    /// DM user tenant get
    /// </summary>
    [HttpGet("tenant-members")]
    public async Task<IActionResult> GetTenantMembers(
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(out var tenantId);
        if (tenantError is not null) return tenantError;

        var result = await _sender.Send(new GetTenantMembersQuery(tenantId, search), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenantMemberDto>>.Ok(result));
    }

    /// <summary>
    /// system user get (role/project)
    /// GET /api/users/system
    /// </summary>
    [HttpGet("system")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetSystemUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(role) && !Enum.TryParse<RoleType>(role, true, out _))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidQuery, "Invalid role filter"));
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = status.Trim().ToLowerInvariant();
            if (normalizedStatus is not ("active" or "inactive"))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    ApiErrorCodes.Validation.InvalidQuery,
                    "Invalid status filter. Use 'active' or 'inactive'."));
            }
        }

        var result = await _sender.Send(
            new GetSystemUsersQuery(pageNumber, pageSize, search, role, normalizedStatus),
            cancellationToken);
        return Ok(ApiResponse<PagedResult<SystemUserDto>>.Ok(result));
    }

    /// <summary>
    /// system user get (role/project)
    /// GET /api/users/system/{id}
    /// </summary>
    [HttpGet("system/{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetSystemUserById(string id, CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var dto = await _sender.Send(new GetSystemUserByIdQuery(userId), cancellationToken);
        if (dto is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }
        return Ok(ApiResponse<SystemUserDto>.Ok(dto));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _sender.Send(new GetCurrentUserQuery(userId));
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// user create
    /// POST /api/users
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new CreateUserCommand(tenantId, requestUserId, dto), cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User creation returned no result"));

        return CreatedResponse($"/api/users/{result.Id}", result);
    }

    /// <summary>
    /// user update ()
    /// PUT /api/users/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> UpdateUser(
        string id,
        [FromBody] UpdateSystemUserDto dto,
        CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var tenantError = RequireTenantId(out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new UpdateSystemUserCommand(tenantId, userId, requestUserId, dto), cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));

        return Ok(ApiResponse<SystemUserDto>.Ok(result));
    }

    /// <summary>
    /// user
    /// PUT /api/users/{id}/deactivate
    /// </summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> DeactivateUser(
        string id,
        CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        await _sender.Send(new DeactivateUserCommand(userId, requestUserId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { deactivated = true }));
    }

    /// <summary>
    /// user
    /// PUT /api/users/{id}/activate
    /// </summary>
    [HttpPut("{id}/activate")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> ActivateUser(
        string id,
        CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        await _sender.Send(new ActivateUserCommand(userId, requestUserId), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { activated = true }));
    }

    /// <summary>
    ///
    /// POST /api/users/{id}/reset-password
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> ResetPassword(
        string id,
        CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var userId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new AdminResetPasswordCommand(userId, requestUserId), cancellationToken);
        return Ok(ApiResponse<AdminResetPasswordResponse>.Ok(result));
    }

    /// <summary>
    /// user settings get
    /// GET /api/users/me/preferences
    /// </summary>
    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetCurrentUserPreferences(CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetCurrentUserPreferencesQuery(userId), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<UserPreferencesDto>.Ok(result));
    }

    /// <summary>
    /// user settings update
    /// PUT /api/users/me/preferences
    /// </summary>
    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdateCurrentUserPreferences(
        [FromBody] UserPreferencesDto dto,
        CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var updated = await _sender.Send(new UpdateCurrentUserPreferencesCommand(userId, dto), cancellationToken);
        if (updated is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<UserPreferencesDto>.Ok(updated));
    }

    /// <summary>
    /// user notification settings get
    /// GET /api/users/me/notifications
    /// </summary>
    [HttpGet("me/notifications")]
    public async Task<IActionResult> GetNotificationSettings(CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetNotificationSettingsQuery(userId), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<NotificationSettingsDto>.Ok(result));
    }

    /// <summary>
    /// user notification settings update
    /// PUT /api/users/me/notifications
    /// </summary>
    [HttpPut("me/notifications")]
    public async Task<IActionResult> UpdateNotificationSettings(
        [FromBody] NotificationSettingsDto dto,
        CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var updated = await _sender.Send(new UpdateNotificationSettingsCommand(userId, dto), cancellationToken);
        if (updated is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<NotificationSettingsDto>.Ok(updated));
    }

    /// <summary>
    /// user update
    /// PUT /api/users/me/profile
    /// </summary>
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateCurrentUserProfile(
        [FromBody] UpdateProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var updated = await _sender.Send(new UpdateCurrentUserProfileCommand(userId, dto), cancellationToken);
        if (updated is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "User not found"));
        }

        return Ok(ApiResponse<UserDto>.Ok(updated));
    }
}
