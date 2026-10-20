using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Projects.Commands;
namespace Sddp.Api.Controllers;

/// <summary>Administrator-managed Git connections, restricted to explicit tenant/project ownership.</summary>
[Route("api/admin/git-connections")]
[Authorize(Policy = "RequireAdmin")]
public sealed class GitConnectionsController(ISender sender) : BaseApiController
{
    /// <summary>Lists metadata without credentials.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.UserManage)]
    [ResourceScopeExempt("Tenant administrator operation; the service validates connection and allowed project ownership within the authenticated tenant.")]
    [ProducesResponseType(typeof(ApiResponse<GitConnectionListDto>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var error = RequireTenantId(out var tenant); if (error is not null) return error;
        return Ok(ApiResponse<GitConnectionListDto>.Ok(await sender.Send(new ListGitConnectionsQuery(tenant, null), ct)));
    }
    /// <summary>Creates a project-bound connection after validating provided credentials.</summary>
    [ResourceScopeExempt("Tenant administrator operation; the service validates connection and allowed project ownership within the authenticated tenant.")]
    [HttpPost]
    [RequirePermission(PermissionCodes.UserManage)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GitConnectionDto>), 201)]
    [ProducesResponseType(400), ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateGitConnectionDto input, CancellationToken ct)
    {
        var error = RequireTenantId(out var tenant); if (error is not null) return error;
        var result = await sender.Send(new CreateGitConnectionCommand(tenant, input), ct);
        return Created($"/api/admin/git-connections/{result.Id}", ApiResponse<GitConnectionDto>.Ok(result));
    }
    /// <summary>Replaces credentials only after checking the new connection.</summary>
    [ResourceScopeExempt("Tenant administrator operation; the service validates connection and allowed project ownership within the authenticated tenant.")]
    [HttpPut("{id}")]
    [RequirePermission(PermissionCodes.UserManage)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GitConnectionDto>), 200)]
    [ProducesResponseType(400), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGitConnectionDto input, CancellationToken ct)
    {
        var error = RequireTenantId(out var tenant); if (error is not null) return error;
        if (!GlobalUniqueId.TryParse(id, out var key)) return BadRequest(ApiResponse<object>.Fail("INVALID_ID", "Invalid connection ID."));
        return Ok(ApiResponse<GitConnectionDto>.Ok(await sender.Send(new UpdateGitConnectionCommand(tenant, key, input), ct)));
    }
    /// <summary>Deactivates an unused connection.</summary>
    [ResourceScopeExempt("Tenant administrator operation; the service validates connection and allowed project ownership within the authenticated tenant.")]
    [HttpDelete("{id}")]
    [RequirePermission(PermissionCodes.UserManage)]
    [Authorize]
    [ProducesResponseType(204), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Delete(string id, [FromQuery] int expectedVersion, CancellationToken ct)
    {
        var error = RequireTenantId(out var tenant); if (error is not null) return error;
        if (!GlobalUniqueId.TryParse(id, out var key)) return BadRequest(ApiResponse<object>.Fail("INVALID_ID", "Invalid connection ID."));
        await sender.Send(new DeleteGitConnectionCommand(tenant, key, expectedVersion), ct);
        return NoContent();
    }
}
