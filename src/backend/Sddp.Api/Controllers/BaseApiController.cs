using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Api.Middleware;
using Sddp.Api.Services;

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

    /// <summary>
    /// The project this request is scoped to, preferring the one
    /// <see cref="ResourceScopeMiddleware"/> resolved from the routed resource over the header the
    /// caller sent.
    ///
    /// Without this the middleware would fix membership and permission — both read
    /// <see cref="ITenantContext"/> — while handlers kept receiving the caller's own header, so an
    /// ownership comparison inside a handler would still be checking the wrong project.
    ///
    /// The marker matters as much as the value: on a resource with no project the resolved answer
    /// is null, and falling back to the header there would hand the handler a project the resource
    /// does not belong to.
    /// </summary>
    private bool TryGetResolvedProjectId(out GlobalUniqueId projectId)
    {
        projectId = default;

        if (!HttpContext.Items.ContainsKey(ResourceScopeMiddleware.ResolvedItemKey))
        {
            return false;
        }

        var resolved = HttpContext.RequestServices.GetRequiredService<ITenantContext>().ProjectId;
        if (!resolved.HasValue)
        {
            return false;
        }

        projectId = resolved.Value;
        return true;
    }

    /// <summary>True when this request has a project, resolved or from the header.</summary>
    protected bool TryGetProjectId(out GlobalUniqueId projectId)
    {
        if (TryGetResolvedProjectId(out projectId))
        {
            return true;
        }

        // Endpoints the middleware does not scope — listings, creation, exempt actions — still take
        // the project from the header, which is where it has always come from.
        if (HttpContext.Items.ContainsKey(ResourceScopeMiddleware.ResolvedItemKey))
        {
            return false;
        }

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

    /// <summary>
    /// Overload for actions that bind the header themselves with [FromHeader]. The resolved scope
    /// still wins — binding the header does not make it authoritative.
    /// </summary>
    protected IActionResult? RequireProjectId(
        string? projectIdHeader,
        out GlobalUniqueId projectId,
        string errorCode = ApiErrorCodes.Header.InvalidProject,
        string errorMessage = "Invalid or missing X-Project-Id header")
    {
        if (TryGetResolvedProjectId(out projectId))
        {
            return null;
        }

        if (HttpContext.Items.ContainsKey(ResourceScopeMiddleware.ResolvedItemKey)
            || !GlobalUniqueId.TryParse(projectIdHeader, out projectId))
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
        if (TryGetProjectId(out projectId))
        {
            return null;
        }

        return BadRequest(ApiResponse<object>.Fail(errorCode, errorMessage));
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
