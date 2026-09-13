using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.Artifacts.Queries;
using Sddp.Application.Features.Projects.Commands;
using Sddp.Application.Features.Projects.Queries;
using Sddp.Application.Features.Admin.Commands;

namespace Sddp.Api.Controllers;

/// <summary>
/// Projects controller
/// </summary>
[Route("api/projects")]
[Authorize]
public class ProjectsController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IMemoryCache _cache;
    private readonly IScopedAuthorizationService _authorizationService;

    public ProjectsController(
        ISender sender,
        IMemoryCache cache,
        IScopedAuthorizationService authorizationService)
    {
        _sender = sender;
        _cache = cache;
        _authorizationService = authorizationService;
    }

    /// <summary>Gets current user's capabilities for this project.</summary>
    [HttpGet("{id}/capabilities")]
    [ResourceScope<Project>("id")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ApiResponse<ProjectCapabilitiesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapabilities(string id, CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(out var tenantId);
        if (tenantError is not null) return tenantError;
        var userError = RequireUserId(out var userId, unauthorizedIfMissing: true);
        if (userError is not null) return userError;
        if (!GlobalUniqueId.TryParse(id, out var projectId))
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        var permissions = await _authorizationService.GetPermissionsAsync(userId, tenantId, projectId, cancellationToken);
        var canWrite = !await _authorizationService.HasAnyRoleAsync(
            userId, tenantId, projectId, Array.Empty<RoleType>(), cancellationToken);
        return Ok(ApiResponse<ProjectCapabilitiesDto>.Ok(new(
            tenantId.ToString(), projectId.ToString(), permissions, canWrite)));
    }

    /// <summary>
    /// Gets the project list (returns only user projects; Admin gets all)
    /// GET /api/projects
    /// </summary>
    [HttpGet]
    [ResourceScopeExempt("Listing endpoint whose result is itself the set of projects the caller belongs to.")]
    public async Task<IActionResult> GetProjects(
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

        var userError = RequireUserId(
            out var userId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        var isAdmin = await _authorizationService.HasAnyRoleAsync(
            userId,
            tenantId,
            projectId: null,
            [RoleType.Admin],
            cancellationToken);

        var projectDtos = await _sender.Send(
            new GetProjectsByUserQuery(tenantId, userId, isAdmin),
            cancellationToken);
        var pagedResult = CreatePagedResult(projectDtos, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(pagedResult));
    }

    /// <summary>
    /// Creates a project
    /// POST /api/projects
    /// </summary>
    [HttpPost]
    [ResourceScopeExempt("Creation endpoint: the project does not exist yet, so there is nothing to resolve.")]
    [RequirePermission(PermissionCodes.ProjectCreate)]
    [Authorize]
    public async Task<IActionResult> CreateProject(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateProjectDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(
            out var requestUserId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(
            new CreateProjectCommand(tenantId, requestUserId, dto),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));
        }

        return CreatedResponse($"/api/projects/{result.Id}", result);
    }

    /// <summary>
    /// Gets project details (including summary statistics)
    /// GET /api/projects/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ResourceScope<Project>("id")]
    public async Task<IActionResult> GetProjectById(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var detail = await _sender.Send(
            new GetProjectByIdQuery(tenantId, projectId),
            cancellationToken);
        if (detail is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));
        }

        return Ok(ApiResponse<ProjectDetailDto>.Ok(detail));
    }

    [HttpGet("{id}/artifacts")]
    [ResourceScope<Project>("id")]
    public async Task<IActionResult> GetProjectArtifacts(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var artifacts = await _sender.Send(
            new GetProjectArtifactsQuery(tenantId, projectId),
            cancellationToken);

        // Counted before filtering: the dashboard and the filter labels are asking what the
        // project holds, not what the current filter matched.
        var counts = new ArtifactStatusCounts(
            Valid: artifacts.Count(a => IsStatus(a, "Valid")),
            Modified: artifacts.Count(a => IsStatus(a, "Modified")),
            Missing: artifacts.Count(a => IsStatus(a, "Missing")),
            Unverified: artifacts.Count(a => IsStatus(a, "Unverified")));

        var matched = FilterArtifacts(artifacts, status, q);
        var page = CreatePagedResult(matched, pageNumber, pageSize);

        return Ok(ApiResponse<ProjectArtifactsPage>.Ok(new ProjectArtifactsPage
        {
            Items = page.Items,
            TotalCount = page.TotalCount,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            StatusCounts = counts,
        }));
    }

    /// <summary>
    /// Gets per-member ownership data for a project (for treemap visualization)
    /// GET /api/projects/{id}/ownership
    /// </summary>
    [HttpGet("{id}/ownership")]
    [ResourceScope<Project>("id")]
    public async Task<IActionResult> GetProjectOwnership(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var result = await _sender.Send(
            new GetProjectOwnershipQuery(tenantId, projectId),
            cancellationToken);

        return Ok(ApiResponse<ProjectOwnershipDto>.Ok(result));
    }

    /// <summary>
    /// Updates a project
    /// PUT /api/projects/{id}
    /// </summary>
    [HttpPut("{id}")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateProject(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateProjectDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var userError = RequireUserId(
            out var requestUserId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(
            new UpdateProjectCommand(tenantId, projectId, requestUserId, dto),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));
        }

        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    /// <summary>
    /// Gets the project's traceability map (Tangled Tree)
    /// GET /api/projects/{id}/traceability-map
    /// </summary>
    [HttpGet("{id}/traceability-map")]
    [ResourceScope<Project>("id")]
    public async Task<IActionResult> GetTraceabilityMap(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var result = await _sender.Send(
            new GetTraceabilityMapQuery(tenantId, projectId),
            cancellationToken);

        return Ok(ApiResponse<TraceabilityMapDto>.Ok(result));
    }

    /// <summary>
    /// Adds a project member
    /// POST /api/projects/{id}/members
    /// </summary>
    [HttpPost("{id}/members")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.RoleAssign)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> AddProjectMember(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] AddProjectMemberDto dto,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        if (!GlobalUniqueId.TryParse(dto.UserId, out var targetUserId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.TargetUser, "Invalid user ID format"));
        }

        var userError = RequireUserId(
            out var requestUserId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(
            new AddProjectMemberCommand(tenantId, projectId, targetUserId, dto.Role, requestUserId),
            cancellationToken);

        // Invalidate membership cache
        _cache.Remove($"project-membership:{(Guid)targetUserId}:{(Guid)projectId}");

        return CreatedResponse($"/api/projects/{id}/members/{dto.UserId}", result);
    }

    /// <summary>
    /// Deactivates a project member (soft block)
    /// PUT /api/projects/{id}/members/{userId}/deactivate
    /// </summary>
    [HttpPut("{id}/members/{userId}/deactivate")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.RoleAssign)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> DeactivateProjectMember(
        string id,
        string userId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        if (!GlobalUniqueId.TryParse(userId, out var targetUserId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var userError = RequireUserId(
            out var requestUserId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        await _sender.Send(
            new DeactivateProjectMemberCommand(tenantId, projectId, targetUserId, requestUserId),
            cancellationToken);

        // Invalidate membership cache
        _cache.Remove($"project-membership:{(Guid)targetUserId}:{(Guid)projectId}");

        return Ok(ApiResponse<object>.Ok(null!));
    }

    /// <summary>
    /// Removes a project member (hard delete)
    /// PUT /api/projects/{id}/members/{userId}/remove
    /// </summary>
    [HttpPut("{id}/members/{userId}/remove")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.RoleAssign)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> RemoveProjectMember(
        string id,
        string userId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        if (!GlobalUniqueId.TryParse(userId, out var targetUserId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid user ID format"));
        }

        var userError = RequireUserId(
            out var requestUserId,
            unauthorizedIfMissing: true,
            errorCode: "UNAUTHORIZED",
            errorMessage: "User identity not found");
        if (userError is not null)
        {
            return userError;
        }

        await _sender.Send(
            new RemoveProjectMemberCommand(tenantId, projectId, targetUserId, requestUserId),
            cancellationToken);

        // Invalidate membership cache
        _cache.Remove($"project-membership:{(Guid)targetUserId}:{(Guid)projectId}");

        return Ok(ApiResponse<object>.Ok(null!));
    }

    // ========================================================================
    // Lifecycle
    // ========================================================================

    /// <summary>
    /// Initializes a project (Planning -> Active)
    /// POST /api/projects/{id}/initialize
    /// </summary>
    [HttpPost("{id}/initialize")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> InitializeProject(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
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
            new InitializeProjectCommand(tenantId, projectId, requestUserId),
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));

        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    /// <summary>
    /// Concludes a project (Active -> Concluded)
    /// POST /api/projects/{id}/conclude
    /// </summary>
    [HttpPost("{id}/conclude")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> ConcludeProject(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ProjectLifecycleReasonDto? dto,
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
            new ConcludeProjectCommand(tenantId, projectId, requestUserId, dto?.Reason),
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));

        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    /// <summary>
    /// Reopens a project (Concluded -> Active)
    /// POST /api/projects/{id}/reopen
    /// </summary>
    [HttpPost("{id}/reopen")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> ReopenProject(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ProjectLifecycleReasonDto? dto,
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
            new ReopenProjectCommand(tenantId, projectId, requestUserId, dto?.Reason),
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));

        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    /// <summary>
    /// Archives a project (Concluded -> Archived)
    /// POST /api/projects/{id}/archive
    /// </summary>
    [HttpPost("{id}/archive")]
    [ResourceScope<Project>("id")]
    [RequirePermission(PermissionCodes.ProjectUpdate)]
    [Authorize(Policy = "CanManageProjectMembers")]
    [Authorize]
    public async Task<IActionResult> ArchiveProject(
        string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
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
            new ArchiveProjectCommand(tenantId, projectId, requestUserId),
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));

        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    /// <summary>
    /// In-memory paging for membership-based multi-table filtering results.
    /// Because the query handler returns DTOs, DB-level paging would require changing the handler contract.
    /// The current structure is acceptable because the number of projects per user is limited.
    /// </summary>
    private static bool IsStatus(ArtifactTrackingSummaryDto artifact, string status) =>
        string.Equals(artifact.Status, status, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Applies the optional status and text filters across every artifact in the project.
    ///
    /// Both are omitted by default, in which case this returns the input untouched and the
    /// endpoint behaves exactly as it did.
    /// </summary>
    private static IReadOnlyCollection<ArtifactTrackingSummaryDto> FilterArtifacts(
        IReadOnlyList<ArtifactTrackingSummaryDto> artifacts,
        string? status,
        string? q)
    {
        IEnumerable<ArtifactTrackingSummaryDto> matched = artifacts;

        if (!string.IsNullOrWhiteSpace(status))
        {
            matched = matched.Where(a => IsStatus(a, status.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            matched = matched.Where(a =>
                Contains(a.ArtifactPath, term)
                || Contains(a.ArtifactType, term)
                || Contains(a.EntityName, term));
        }

        return ReferenceEquals(matched, artifacts) ? artifacts : matched.ToList();

        static bool Contains(string? value, string term) =>
            value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static PagedResult<T> CreatePagedResult<T>(
        IReadOnlyCollection<T> source,
        int pageNumber,
        int pageSize)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var skip = (safePageNumber - 1) * safePageSize;
        var items = source.Skip(skip).Take(safePageSize).ToList();
        return PagedResult<T>.Create(items, source.Count, safePageNumber, safePageSize);
    }
}
