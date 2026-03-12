using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Admin.Commands;

namespace Sddp.Api.Controllers;

/// <summary>
/// controller (Admin permission)
/// </summary>
[Route("api/admin")]
[Authorize(Policy = "RequireAdmin")]
public class AdminController : BaseApiController
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// project (create → delete → Planning status)
    /// POST /api/admin/projects/{id}/reset
    /// </summary>
    [HttpPost("projects/{id}/reset")]
    public async Task<IActionResult> ResetProjectData(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ResetProjectDataDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        if (!GlobalUniqueId.TryParse(id, out var projectId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED", errorMessage: "User identity not found");
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new ResetProjectDataCommand(tenantId, projectId, requestUserId, dto.ConfirmationCode),
            cancellationToken);

        return Ok(ApiResponse<ProjectDataResetResultDto>.Ok(result));
    }

    /// <summary>
    /// tenant all (project → delete → Planning)
    /// POST /api/admin/tenants/reset
    /// </summary>
    [HttpPost("tenants/reset")]
    public async Task<IActionResult> ResetTenantData(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ResetTenantDataDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var requestUserId, unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED", errorMessage: "User identity not found");
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new ResetTenantDataCommand(tenantId, requestUserId, dto.ConfirmationToken),
            cancellationToken);

        return Ok(ApiResponse<TenantDataResetResultDto>.Ok(result));
    }
}
