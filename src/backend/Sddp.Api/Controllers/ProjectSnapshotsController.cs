using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.ProjectSnapshots.Commands;
using Sddp.Application.Features.ProjectSnapshots.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Project Snapshots (backup / restore)
/// </summary>
[Route("api/projects/{projectId}/snapshots")]
[Authorize]
public class ProjectSnapshotsController : BaseApiController
{
    private readonly ISender _sender;

    public ProjectSnapshotsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// List all snapshots for a project
    /// GET /api/projects/{projectId}/snapshots
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSnapshots(
        string projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        if (!GlobalUniqueId.TryParse(projectId, out var pid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));

        var result = await _sender.Send(
            new GetProjectSnapshotsQuery(tenantId, pid),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ProjectSnapshotDto>>.Ok(result));
    }

    /// <summary>
    /// Create a new snapshot (backup all project data)
    /// POST /api/projects/{projectId}/snapshots
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSnapshot(
        string projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateProjectSnapshotDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        if (!GlobalUniqueId.TryParse(projectId, out var pid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new CreateProjectSnapshotCommand(tenantId, pid, userId, dto),
            cancellationToken);

        return CreatedResponse($"/api/projects/{projectId}/snapshots/{result.Id}", result);
    }

    /// <summary>
    /// Restore project data from a snapshot
    /// POST /api/projects/{projectId}/snapshots/{snapshotId}/restore
    /// </summary>
    [HttpPost("{snapshotId}/restore")]
    public async Task<IActionResult> RestoreSnapshot(
        string projectId,
        string snapshotId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        if (!GlobalUniqueId.TryParse(projectId, out var pid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));

        if (!GlobalUniqueId.TryParse(snapshotId, out var sid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid snapshot ID format"));

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new RestoreProjectSnapshotCommand(tenantId, pid, sid, userId),
            cancellationToken);

        return Ok(ApiResponse<ProjectSnapshotDto>.Ok(result));
    }

    /// <summary>
    /// Delete a snapshot (soft delete)
    /// DELETE /api/projects/{projectId}/snapshots/{snapshotId}
    /// </summary>
    [HttpDelete("{snapshotId}")]
    public async Task<IActionResult> DeleteSnapshot(
        string projectId,
        string snapshotId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        if (!GlobalUniqueId.TryParse(projectId, out var pid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));

        if (!GlobalUniqueId.TryParse(snapshotId, out var sid))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid snapshot ID format"));

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null) return userError;

        await _sender.Send(
            new DeleteProjectSnapshotCommand(tenantId, pid, sid, userId),
            cancellationToken);

        return NoContent();
    }
}
