using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;

namespace Sddp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected bool TryGetUserId(out GlobalUniqueId userId)
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return GlobalUniqueId.TryParse(userIdClaim, out userId);
    }

    protected bool TryGetTenantId(out GlobalUniqueId tenantId)
    {
        var header = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return GlobalUniqueId.TryParse(header, out tenantId);
    }

    protected bool TryGetProjectId(out GlobalUniqueId projectId)
    {
        var header = Request.Headers["X-Project-Id"].FirstOrDefault();
        return GlobalUniqueId.TryParse(header, out projectId);
    }

    protected IActionResult? RequireTenantId(
        string? tenantIdHeader,
        out GlobalUniqueId tenantId,
        string errorCode = ApiErrorCodes.Header.InvalidTenant,
        string errorMessage = "Invalid or missing X-Tenant-Id header")
    {
        if (!GlobalUniqueId.TryParse(tenantIdHeader, out tenantId))
        {
            return BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
        }

        return null;
    }

    protected IActionResult? RequireTenantId(
        out GlobalUniqueId tenantId,
        string errorCode = ApiErrorCodes.Header.InvalidTenant,
        string errorMessage = "Invalid or missing X-Tenant-Id header")
    {
        var header = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return RequireTenantId(header, out tenantId, errorCode, errorMessage);
    }

    protected IActionResult? RequireProjectId(
        string? projectIdHeader,
        out GlobalUniqueId projectId,
        string errorCode = ApiErrorCodes.Header.InvalidProject,
        string errorMessage = "Invalid or missing X-Project-Id header")
    {
        if (!GlobalUniqueId.TryParse(projectIdHeader, out projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
        }

        return null;
    }

    protected IActionResult? RequireProjectId(
        out GlobalUniqueId projectId,
        string errorCode = ApiErrorCodes.Header.InvalidProject,
        string errorMessage = "Invalid or missing X-Project-Id header")
    {
        var header = Request.Headers["X-Project-Id"].FirstOrDefault();
        return RequireProjectId(header, out projectId, errorCode, errorMessage);
    }

    protected IActionResult? TryGetProjectId(
        string? projectIdHeader,
        out GlobalUniqueId? projectId,
        string errorCode = ApiErrorCodes.Header.InvalidProject,
        string errorMessage = "Invalid X-Project-Id header")
    {
        projectId = null;

        if (string.IsNullOrWhiteSpace(projectIdHeader))
        {
            return null;
        }

        if (!GlobalUniqueId.TryParse(projectIdHeader, out var parsed))
        {
            return BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
        }

        projectId = parsed;
        return null;
    }

    protected IActionResult? RequireTenantAndProject(
        string? tenantIdHeader,
        string? projectIdHeader,
        out GlobalUniqueId tenantId,
        out GlobalUniqueId projectId,
        string errorCode,
        string errorMessage)
    {
        tenantId = default;
        projectId = default;

        if (!GlobalUniqueId.TryParse(tenantIdHeader, out tenantId)
            || !GlobalUniqueId.TryParse(projectIdHeader, out projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
        }

        return null;
    }

    protected IActionResult? RequireUserId(
        out GlobalUniqueId userId,
        bool unauthorizedIfMissing = false,
        string errorCode = ApiErrorCodes.Auth.InvalidUser,
        string errorMessage = "Invalid user ID in token")
    {
        if (TryGetUserId(out userId))
        {
            return null;
        }

        return unauthorizedIfMissing
            ? Unauthorized(ApiResponse<object>.Fail(errorCode, errorMessage))
            : BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
    }

    // ========================================
    // Response Helpers
    // ========================================

    protected IActionResult NotFoundResponse(string entity)
        => NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, $"{entity} not found"));

    protected IActionResult NotFoundResponse(string entity, Guid id)
        => NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, $"{entity} not found: {id}"));

    protected CreatedResult CreatedResponse<T>(string uri, T data) where T : class
        => Created(uri, ApiResponse<T>.Ok(data));

    protected IActionResult ValidationErrorResponse(string message)
        => BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.Error, message));

    protected IActionResult OperationErrorResponse(string errorCode, string message)
        => BadRequest(ApiResponse<object>.Fail(errorCode, message));
}
