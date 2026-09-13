using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.AuditLogs.Queries;
using Sddp.Application.Features.Specs.Commands;
using Sddp.Application.Features.Specs.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Spec controller
/// </summary>
[Route("api/specs")]
[Authorize]
public class SpecsController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IVersionDiffService _diffService;

    public SpecsController(
        ISender sender,
        IVersionDiffService diffService)
    {
        _sender = sender;
        _diffService = diffService;
    }

    [HttpGet]
    [ResourceScopeExempt("Listing endpoint: no resource id in the route, and results are already filtered to the tenant and the requested project.")]
    public async Task<IActionResult> GetSpecs(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SpecStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var result = await _sender.Send(new GetSpecsQuery(tenantId, projectId, page, pageSize, status), cancellationToken);
        return Ok(ApiResponse<SpecPageDto>.Ok(result));
    }

    [HttpGet("{id}")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetSpecById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetSpecByIdQuery(tenantId, projectId, specId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDetailDto>.Ok(result));
    }

    [HttpGet("code/{code}")]
    [ResourceScopeExempt("Lookup by human code, not a database id: the query resolves the code inside the requested project, so it cannot reach another one.")]
    public async Task<IActionResult> GetSpecByCode(
        string code,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var result = await _sender.Send(new GetSpecByCodeQuery(tenantId, projectId, code), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDetailDto>.Ok(result));
    }

    [HttpGet("search")]
    [ResourceScopeExempt("Search endpoint: no resource id in the route, and the query filters to the tenant and the requested project.")]
    public async Task<IActionResult> SearchSpecs(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string q = "",
        [FromQuery] int limit = 15,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var result = await _sender.Send(new SearchSpecsQuery(tenantId, projectId, q, limit), cancellationToken);
        return Ok(ApiResponse<IEnumerable<SpecSummaryDto>>.Ok(result));
    }

    [HttpPost]
    [ResourceScopeExempt("Creation endpoint: the spec does not exist yet, so there is no owning project to resolve from.")]
    [RequirePermission(PermissionCodes.SpecCreate)]
    [Authorize]
    public async Task<IActionResult> CreateSpec(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateSpecDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new CreateSpecCommand(tenantId, projectId, userId, dto), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create spec"));
        }

        return Created($"/api/specs/{result.Id}", ApiResponse<SpecDetailDto>.Ok(result));
    }

    [HttpPut("{id}")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateSpecDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateSpecCommand(tenantId, projectId, specId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDetailDto>.Ok(result));
    }

    [HttpPatch("{id}/status")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecLock)]
    [Authorize]
    public async Task<IActionResult> TransitionStatus(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] SpecTransitionStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new TransitionSpecStatusCommand(
            tenantId, projectId, specId, userId, dto.NewStatus), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpPost("{id}/submit-review")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecUpdate)]
    [Authorize]
    public async Task<IActionResult> SubmitForReview(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new SubmitSpecForReviewCommand(tenantId, projectId, specId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpPost("{id}/approve")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecApprove)]
    [Authorize]
    public async Task<IActionResult> ApproveSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new ApproveSpecCommand(tenantId, projectId, specId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpPost("{id}/reject")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecApprove)]
    [Authorize]
    public async Task<IActionResult> RejectSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] SpecRejectDto? dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new RejectSpecCommand(tenantId, projectId, specId, userId, dto?.Reason), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpPost("{id}/lock")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecLock)]
    [Authorize]
    public async Task<IActionResult> LockSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new LockSpecCommand(tenantId, projectId, specId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpDelete("{id}")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecDelete)]
    [Authorize]
    public async Task<IActionResult> DeleteSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new DeleteSpecCommand(tenantId, projectId, specId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    [HttpPut("{id}/activate")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecUpdate)]
    [Authorize]
    public async Task<IActionResult> ActivateSpec(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new ActivateSpecCommand(tenantId, projectId, specId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    [HttpGet("{id}/sign-offs")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetSignOffs(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetSpecSignOffsQuery(tenantId, projectId, specId), cancellationToken);

        return Ok(ApiResponse<List<SignOffDto>>.Ok(result));
    }

    [HttpPost("{id}/sign-off")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecApprove)]
    [Authorize]
    public async Task<IActionResult> SubmitSignOff(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] SubmitSignOffDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new SubmitSpecSignOffCommand(tenantId, projectId, specId, userId, dto), cancellationToken);
        return Ok(ApiResponse<SignOffDto>.Ok(result));
    }

    [HttpGet("{id}/sign-off-summary")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetSignOffSummary(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetSpecSignOffSummaryQuery(tenantId, projectId, specId), cancellationToken);

        return Ok(ApiResponse<SignOffSummaryDto>.Ok(result));
    }

    [HttpPost("{id}/link-requirement")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecUpdate)]
    [Authorize]
    public async Task<IActionResult> LinkRequirement(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] SpecLinkRequirementDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        if (!GlobalUniqueId.TryParse(dto.RequirementId, out var requirementId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.RequirementId, "Invalid requirement ID format"));
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new LinkSpecRequirementCommand(tenantId, projectId, specId, requirementId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpDelete("{id}/link-requirement")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecUpdate)]
    [Authorize]
    public async Task<IActionResult> UnlinkRequirement(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UnlinkSpecRequirementCommand(tenantId, projectId, specId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<SpecDto>.Ok(result));
    }

    [HttpPost("{id}/new-version")]
    [ResourceScope<Spec>("id")]
    [RequirePermission(PermissionCodes.SpecCreate)]
    [Authorize]
    public async Task<IActionResult> CreateNewVersion(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new CreateSpecNewVersionCommand(tenantId, projectId, specId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Created($"/api/specs/{result.Id}", ApiResponse<SpecDetailDto>.Ok(result));
    }

    [HttpGet("{id}/versions")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetVersionHistory(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetSpecVersionHistoryQuery(tenantId, projectId, specId), cancellationToken);

        return Ok(ApiResponse<List<SpecDto>>.Ok(result));
    }

    [HttpGet("{id}/diff/{compareToId}")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> CompareSpecVersions(
        Guid id,
        Guid compareToId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId1 = GlobalUniqueId.FromGuid(id);
        var specId2 = GlobalUniqueId.FromGuid(compareToId);

        var result = await _diffService.CompareSpecVersionsAsync(tenantId, projectId, specId1, specId2, cancellationToken);
        return Ok(ApiResponse<SpecDiffResultDto>.Ok(result));
    }

    /// <summary>
    /// spec update get
    /// </summary>
    [HttpGet("{id}/field-authors")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetFieldAuthors(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        // Verify spec exists
        var spec = await _sender.Send(new GetSpecByIdQuery(tenantId, projectId, specId), cancellationToken);
        if (spec is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        var result = await _sender.Send(
            new GetFieldAuthorsQuery("spec", specId), cancellationToken);
        return Ok(ApiResponse<List<FieldAuthorDto>>.Ok(result));
    }

    /// <summary>
    /// spec change get ()
    /// </summary>
    [HttpGet("{id}/timeline")]
    [ResourceScope<Spec>("id")]
    public async Task<IActionResult> GetTimeline(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var specId = GlobalUniqueId.FromGuid(id);

        // Verify spec exists
        var spec = await _sender.Send(new GetSpecByIdQuery(tenantId, projectId, specId), cancellationToken);
        if (spec is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        var result = await _sender.Send(
            new GetAuditLogsByResourceQuery("spec", specId, limit), cancellationToken);
        return Ok(ApiResponse<List<AuditLogDto>>.Ok(result));
    }
}
