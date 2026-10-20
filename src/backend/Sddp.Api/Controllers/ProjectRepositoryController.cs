using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Projects.Commands;
using Sddp.Domain.Entities;

namespace Sddp.Api.Controllers;

/// <summary>Project repository drafts, connection checks and explicit confirmation.</summary>
[Route("api/projects/{id}/repository")]
[Authorize]
public class ProjectRepositoryController(ISender sender) : BaseApiController
{
    /// <summary>Lists the connections this project is allowed to use.</summary>
    [HttpGet("connections")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [ProducesResponseType(typeof(ApiResponse<GitConnectionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Connections(string id, CancellationToken ct)
    {
        var error = ResolveScope(id, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<GitConnectionListDto>.Ok(await sender.Send(new ListGitConnectionsQuery(tenant, project), ct)));
    }

    /// <summary>Reads saved settings and confirmation state.</summary>
    [HttpGet]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ProducesResponseType(typeof(ApiResponse<ProjectRepositoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var error = ResolveScope(id, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectRepositoryDto>.Ok(await sender.Send(new GetProjectRepositoryQuery(tenant, project), ct)));
    }

    /// <summary>Saves a repository draft or updates the synchronization interval.</summary>
    [HttpPut]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProjectRepositoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Save(string id, [FromBody] UpdateProjectRepositoryDto dto, CancellationToken ct)
    {
        var error = ResolveScope(id, out var tenant, out var project);
        if (error is not null) return error;
        var userError = RequireUserId(out var user, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        return Ok(ApiResponse<ProjectRepositoryDto>.Ok(await sender.Send(new SaveProjectRepositoryCommand(tenant, project, user, dto), ct)));
    }

    /// <summary>Checks the saved remote branch, root and existing file relationship paths.</summary>
    [HttpPost("check")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProjectRepositoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Check(string id, [FromBody] RepositoryVersionDto dto, CancellationToken ct)
    {
        var error = ResolveScope(id, out var tenant, out var project);
        if (error is not null) return error;
        return Ok(ApiResponse<ProjectRepositoryDto>.Ok(await sender.Send(new CheckProjectRepositoryCommand(tenant, project, dto.ExpectedVersion), ct)));
    }

    /// <summary>Freezes checked repository coordinates before file relationships can be saved.</summary>
    [HttpPost("confirm")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProjectRepositoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(string id, [FromBody] RepositoryVersionDto dto, CancellationToken ct)
    {
        var error = ResolveScope(id, out var tenant, out var project);
        if (error is not null) return error;
        var userError = RequireUserId(out var user, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        return Ok(ApiResponse<ProjectRepositoryDto>.Ok(await sender.Send(new ConfirmProjectRepositoryCommand(tenant, project, user, dto.ExpectedVersion), ct)));
    }
    private IActionResult? ResolveScope(string id, out GlobalUniqueId tenant, out GlobalUniqueId project)
    {
        project = default;
        var error = RequireTenantId(out tenant);
        if (error is not null) return error;
        return GlobalUniqueId.TryParse(id, out project) ? null
            : BadRequest(ApiResponse<object>.Fail("INVALID_ID", "Invalid project ID format"));
    }
}
