using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
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

    public ProjectsController(ISender sender, IMemoryCache cache)
    {
        _sender = sender;
        _cache = cache;
    }

    /// <summary>
    /// Gets the project list (returns only user projects; Admin gets all)
    /// GET /api/projects
    /// </summary>
    [HttpGet]
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

        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var isAdmin = userRoles.Contains(RoleType.Admin.ToString());

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

    /// <summary>
    /// Gets the project's artifact list
    /// GET /api/projects/{id}/artifacts
    /// </summary>
    [HttpGet("{id}/artifacts")]
    public async Task<IActionResult> GetProjectArtifacts(
        string id,
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

        if (!GlobalUniqueId.TryParse(id, out var projectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid project ID format"));
        }

        var artifacts = await _sender.Send(
            new GetProjectArtifactsQuery(tenantId, projectId),
            cancellationToken);
        var pagedResult = CreatePagedResult(artifacts, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<ArtifactTrackingSummaryDto>>.Ok(pagedResult));
    }

    /// <summary>
    /// Gets per-member ownership data for a project (for treemap visualization)
    /// GET /api/projects/{id}/ownership
    /// </summary>
    [HttpGet("{id}/ownership")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
    [Authorize(Policy = "CanManageProjectMembers")]
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
