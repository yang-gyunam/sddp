using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Files.Queries;
using Sddp.Domain.Entities;

namespace Sddp.Api.Controllers;

/// <summary>Read-only committed files and current SDDP relationships.</summary>
[Route("api/projects/{projectId}/files")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class ProjectFilesController(ISender sender) : BaseApiController
{
    /// <summary>Searches file names and paths across the entire committed repository, in pages of one hundred.</summary>
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<Project>("projectId")]
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<ProjectFileSearchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(string projectId, [FromQuery] string q, [FromQuery] string? revision = null,
        [FromQuery] int skip = 0, CancellationToken cancellationToken = default)
    {
        var error = Scope(projectId, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectFileSearchDto>.Ok(await sender.Send(new SearchProjectFilesQuery(tenant, project, q, revision, skip), cancellationToken)));
    }

    /// <summary>Lists one directory of the configured branch; an omitted revision selects its tip.</summary>
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<Project>("projectId")]
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse<ProjectFileTreeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Tree(string projectId, [FromQuery] string path = "", [FromQuery] string? revision = null, CancellationToken cancellationToken = default)
    {
        var error = Scope(projectId, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectFileTreeDto>.Ok(await sender.Send(new GetProjectFileTreeQuery(tenant, project, path, revision), cancellationToken)));
    }

    /// <summary>Reads one complete file version; never returns a diff.</summary>
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<Project>("projectId")]
    [HttpGet("content")]
    [ProducesResponseType(typeof(ApiResponse<ProjectFileContentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Content(string projectId, [FromQuery] string path, [FromQuery] string? revision = null, CancellationToken cancellationToken = default)
    {
        var error = Scope(projectId, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectFileContentDto>.Ok(await sender.Send(new GetProjectFileContentQuery(tenant, project, path, revision), cancellationToken)));
    }

    /// <summary>Lists file changes, following renames, in pages of thirty.</summary>
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<Project>("projectId")]
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<ProjectFileHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(string projectId, [FromQuery] string path, [FromQuery] string? revision = null, [FromQuery] int skip = 0, CancellationToken cancellationToken = default)
    {
        var error = Scope(projectId, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectFileHistoryDto>.Ok(await sender.Send(new GetProjectFileHistoryQuery(tenant, project, path, revision, skip), cancellationToken)));
    }

    /// <summary>Lists current owners and related entities visible to the caller.</summary>
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<Project>("projectId")]
    [HttpGet("relations")]
    [ProducesResponseType(typeof(ApiResponse<ProjectFileRelationsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Relations(string projectId, [FromQuery] string path, CancellationToken cancellationToken)
    {
        var error = Scope(projectId, out var tenant, out var project);
        if (error is not null) return error;
        error = RequireUserId(out var user, unauthorizedIfMissing: true);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectFileRelationsDto>.Ok(await sender.Send(new GetProjectFileRelationsQuery(tenant, project, user, path), cancellationToken)));
    }

    private IActionResult? Scope(string projectId, out GlobalUniqueId tenant, out GlobalUniqueId project)
    {
        project = default;
        var error = RequireTenantId(out tenant);
        if (error is not null) return error;
        if (!GlobalUniqueId.TryParse(projectId, out project))
            return BadRequest(ApiResponse<object>.Fail("INVALID_ID", "Invalid project ID."));
        var header = Request.Headers["X-Project-Id"].FirstOrDefault();
        if (header is not null && (!GlobalUniqueId.TryParse(header, out var supplied) || supplied != project))
            return BadRequest(ApiResponse<object>.Fail("PROJECT_SCOPE_MISMATCH", "The project header must match the route."));
        return null;
    }
}
