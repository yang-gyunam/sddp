using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Configuration;
using Sddp.Api.Security;
using Sddp.Application.Features.ProjectSearch;
using Sddp.Domain.Entities;

namespace Sddp.Api.Controllers;

/// <summary>Full-text search within the currently authorized project.</summary>
[Authorize]
[SensitiveRead("project-search")]
[Route("api/projects/{projectId:guid}/search")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProjectSearchController(ISender sender, IScopedAuthorizationService authorization,
    IProjectSearchFileIndex files, DistributionInfo distribution, ILogger<ProjectSearchController> logger) : BaseApiController
{
    [HttpGet]
    [ResourceScope<Project>("projectId")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ProducesResponseType(typeof(ApiResponse<ProjectSearchPage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(Guid projectId, [FromQuery] string q, [FromQuery] string mode = "AND",
        [FromQuery] string field = "all", [FromQuery] string menu = "all", [FromQuery] string sort = "relevance",
        [FromQuery] int offset = 0, [FromQuery] string? cursor = null, CancellationToken ct = default)
    {
        var error = RequireTenantAndProject(Request.Headers["X-Tenant-Id"].FirstOrDefault(), Request.Headers["X-Project-Id"].FirstOrDefault(),
            out var tenant, out var project, "INVALID_SCOPE", "Valid tenant and project headers are required.");
        if (error is not null) return error;
        if (!Guid.TryParse(Request.Headers["X-Project-Id"].FirstOrDefault(), out var supplied) ||
            supplied != projectId || project.ToGuid() != projectId || offset is < 0 or > int.MaxValue - 10 || sort is not ("date" or "relevance") ||
            (menu != "all" && !ProjectSearchQueryHandler.Menus.Contains(menu)))
            return BadRequest(ApiResponse<object>.Fail("INVALID_QUERY", "Invalid project scope or search options."));
        error = RequireUserId(out var user, unauthorizedIfMissing: true);
        if (error is not null) return error;
        var expression = new ProjectSearchExpression(q, mode, field);
        var fingerprint = ProjectSearchCursor.Identify(tenant.ToString(), project.ToString(), user.ToString(), q, mode, field, menu, sort);
        var position = ProjectSearchCursor.Decode(cursor, fingerprint);
        var permissions = await authorization.GetPermissionsAsync(user, tenant, project, ct);
        string? commit = null;
        try { commit = await files.PrepareAsync(tenant, project, ct); }
        catch (Exception e) when (e is SddpException or IOException ||
            e is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogInformation("Project {Project} repository could not be included in search ({ErrorType})", project, e.GetType().Name);
        }
        var result = await sender.Send(new ProjectSearchQuery(tenant, project, user, permissions, expression, menu, sort, offset, commit, commit is null, fingerprint, position,
            IncludeAiConversationMessages: distribution.Edition == "ai"), ct);
        return Ok(ApiResponse<ProjectSearchPage>.Ok(result));
    }
}
