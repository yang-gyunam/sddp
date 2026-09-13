using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.Artifacts.Commands;
using Sddp.Application.Features.Artifacts.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Artifact Upsert DTO
/// </summary>
public record UpsertArtifactRequest(
    Guid SpecId,
    string ArtifactPath,
    string ArtifactType,
    string ContentHash,
    string GeneratorVersion,
    string TemplateVersion,
    string EntityName,
    Guid? GlossaryTermId = null,
    Guid? SourceConversationId = null,
    Guid? SourceRequirementId = null,
    Guid? OwnerUserId = null,
    Guid? ArtifactId = null);

/// <summary>
/// Artifact DTO
/// </summary>
public record VerifyArtifactRequest(Guid ArtifactId, string CurrentHash);

/// <summary>
/// Artifact controller
/// </summary>
[Route("api/artifacts")]
[Authorize]
public class ArtifactsController : BaseApiController
{
    private readonly ISender _sender;
    private readonly bool _isArtifactMaintenanceEnabled;
    private readonly string _artifactMaintenanceDisabledMessage;

    public ArtifactsController(ISender sender, IConfiguration configuration)
    {
        _sender = sender;
        _isArtifactMaintenanceEnabled = configuration.GetValue("FeatureFlags:Artifacts:MaintenanceEnabled", false);
        _artifactMaintenanceDisabledMessage = configuration.GetValue<string>("FeatureFlags:Artifacts:MaintenanceDisabledMessage")
            ?? "Artifact maintenance actions are coming soon.";
    }

    private IActionResult ArtifactMaintenanceDisabled()
    {
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.Fail("ARTIFACT_MAINTENANCE_DISABLED", _artifactMaintenanceDisabledMessage));
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Listing endpoint with no resource id in the route; the handler filters artifacts by the requested tenant and project, so a spec id from another project returns nothing.")]
    public async Task<IActionResult> GetArtifactsBySpec(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] Guid specId,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new GetArtifactsBySpecQuery(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(specId)), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArtifactTrackingDto>>.Ok(result));
    }

    [HttpGet("search")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Search endpoint with no resource id in the route; results are filtered by the requested tenant and project.")]
    public async Task<IActionResult> SearchArtifacts(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string q = "",
        [FromQuery] int limit = 15,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new SearchArtifactsQuery(tenantId, projectId, q, limit), cancellationToken);
        return Ok(ApiResponse<IEnumerable<ArtifactSearchResultDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<ArtifactTracking>("id")]
    public async Task<IActionResult> GetArtifactById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new GetArtifactByIdQuery(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact not found"));
        }

        return Ok(ApiResponse<ArtifactTrackingDto>.Ok(result));
    }

    /// <summary>
    /// The artifact's descriptor — what this file was supposed to be, per the Spec it was
    /// generated from. Projected from the database, so it is available even when the YAML
    /// file has not been materialized.
    /// </summary>
    [HttpGet("{id}/descriptor")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<ArtifactTracking>("id")]
    public async Task<IActionResult> GetArtifactDescriptor(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new GetArtifactDescriptorQuery(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact not found"));
        }

        return Ok(ApiResponse<ArtifactDescriptorDto>.Ok(result));
    }

    /// <summary>
    /// The artifact file's actual contents, for reviewing it against the descriptor.
    /// </summary>
    [HttpGet("{id}/content")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<ArtifactTracking>("id")]
    public async Task<IActionResult> GetArtifactContent(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new GetArtifactContentQuery(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                ApiErrorCodes.Crud.NotFound, "Artifact file not found"));
        }

        if (result.TooLarge)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse<object>.Fail(
                    ApiErrorCodes.Artifact.TooLarge,
                    $"Artifact is {result.SizeBytes} bytes, above the {result.MaxBytes} byte limit for reading over the API."));
        }

        return Ok(ApiResponse<ArtifactContentDto>.Ok(result));
    }

    [HttpGet("by-path")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Lookup by path with no resource id in the route; the handler matches the path only within the requested tenant and project.")]
    public async Task<IActionResult> GetArtifactByPath(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string path,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.MissingPath, "path query parameter is required"));
        }

        var result = await _sender.Send(new GetArtifactByPathQuery(tenantId, projectId, path), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, $"Artifact not found for path: {path}"));
        }

        return Ok(ApiResponse<ArtifactTrackingDto>.Ok(result));
    }

    [HttpPost]
    [ResourceScopeExempt("Upsert endpoint: the artifact id, when present, arrives in the body rather than the route, and the handler ignores an artifact whose ProjectId differs from the requested project.")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> UpsertArtifact(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpsertArtifactRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        if (string.IsNullOrWhiteSpace(request.ArtifactPath) || string.IsNullOrWhiteSpace(request.ContentHash))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidRequest, "artifactPath and contentHash are required"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(new UpsertArtifactCommand(
            tenantId,
            projectId,
            userId,
            GlobalUniqueId.FromGuid(request.SpecId),
            request.ArtifactPath,
            request.ArtifactType,
            request.ContentHash,
            request.GeneratorVersion,
            request.TemplateVersion,
            request.EntityName,
            request.GlossaryTermId.HasValue ? GlobalUniqueId.FromGuid(request.GlossaryTermId.Value) : null,
            request.SourceConversationId.HasValue ? GlobalUniqueId.FromGuid(request.SourceConversationId.Value) : null,
            request.SourceRequirementId.HasValue ? GlobalUniqueId.FromGuid(request.SourceRequirementId.Value) : null,
            request.OwnerUserId.HasValue ? GlobalUniqueId.FromGuid(request.OwnerUserId.Value) : null,
            request.ArtifactId.HasValue ? GlobalUniqueId.FromGuid(request.ArtifactId.Value) : null), cancellationToken);

        return Ok(ApiResponse<ArtifactTrackingDto>.Ok(result));
    }

    [HttpPost("{id}/regenerate")]
    [ResourceScopeExempt("Gone: the action reads no resource and returns 410 for every id, so there is nothing to scope. Removing the route entirely would drop a surface external users have had since v0.1.0.")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public IActionResult RegenerateArtifact(Guid id)
    {
        _ = id;
        return StatusCode(
            StatusCodes.Status410Gone,
            ApiResponse<object>.Fail(
                "GENERATION_VIA_CLI",
                "Artifact regeneration is not available over HTTP. Generation tools are not included in this installation. "
                + "Contact your deployment operator for separately managed generation; this API displays artifacts already "
                + "produced."));
    }

    [HttpPost("verify")]
    [ResourceScopeExempt("Verification endpoint: the artifact id arrives in the body rather than the route, and the handler refuses an artifact whose ProjectId differs from the requested project.")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> VerifyArtifact(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] VerifyArtifactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_isArtifactMaintenanceEnabled)
        {
            return ArtifactMaintenanceDisabled();
        }

        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new VerifyArtifactCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(request.ArtifactId),
            request.CurrentHash), cancellationToken);

        return Ok(ApiResponse<ArtifactVerifyResult>.Ok(result));
    }

    /// <summary>
    /// Artifact (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ResourceScope<ArtifactTracking>("id")]
    [RequirePermission(PermissionCodes.GenerationRollback)]
    [Authorize]
    public async Task<IActionResult> DeactivateArtifact(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new DeactivateArtifactCommand(
            tenantId, projectId, GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Artifact ()
    /// </summary>
    [HttpPut("{id}/activate")]
    [ResourceScope<ArtifactTracking>("id")]
    [RequirePermission(PermissionCodes.GenerationRollback)]
    [Authorize]
    public async Task<IActionResult> ActivateArtifact(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader,
            projectIdHeader,
            out var tenantId,
            out var projectId,
            ApiErrorCodes.Header.MissingHeaders,
            "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null)
        {
            return headerError;
        }

        var result = await _sender.Send(new ActivateArtifactCommand(
            tenantId, projectId, GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    // =========================================================================
    // Artifact-to-Spec Mappings (Brownfield)
    // =========================================================================

    [HttpGet("mappings")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Listing endpoint with no resource id in the route; the handler returns only mappings of the requested tenant and project.")]
    public async Task<IActionResult> GetProjectMappings(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetProjectMappingsQuery(tenantId, projectId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArtifactToSpecMappingDto>>.Ok(result));
    }

    [HttpGet("mappings/by-spec")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Listing endpoint with no resource id in the route; the spec id is a query filter and the handler restricts mappings to the requested tenant and project.")]
    public async Task<IActionResult> GetMappingsBySpec(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] Guid specId,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetMappingsBySpecQuery(tenantId, projectId, GlobalUniqueId.FromGuid(specId)),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArtifactToSpecMappingDto>>.Ok(result));
    }

    [HttpGet("mappings/by-path")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScopeExempt("Listing endpoint with no resource id in the route; the handler matches the path only within the requested tenant and project.")]
    public async Task<IActionResult> GetMappingsByPath(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string path,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.MissingPath, "path query parameter is required"));
        }

        var result = await _sender.Send(
            new GetMappingsByPathQuery(tenantId, projectId, path), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArtifactToSpecMappingDto>>.Ok(result));
    }

    [HttpGet("mappings/{id}")]
    [RequirePermission(PermissionCodes.ProjectRead)]
    [ResourceScope<ArtifactToSpecMapping>("id")]
    public async Task<IActionResult> GetMappingById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetMappingByIdQuery(tenantId, projectId, GlobalUniqueId.FromGuid(id)),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact mapping not found"));
        }

        return Ok(ApiResponse<ArtifactToSpecMappingDto>.Ok(result));
    }

    [HttpPost("mappings")]
    [ResourceScopeExempt("Creation endpoint: the mapping does not exist yet, so there is nothing to resolve a project from.")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> CreateMapping(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateArtifactMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(new CreateArtifactMappingCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(request.SpecId),
            request.ArtifactPath,
            request.MappingReason ?? "Manual",
            request.SourceContent,
            request.Notes,
            userId), cancellationToken);

        return CreatedResponse($"api/artifacts/mappings/{result.Id}", result);
    }

    [HttpPut("mappings/{id}/source")]
    [ResourceScope<ArtifactToSpecMapping>("id")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> UpdateMappingSource(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateMappingSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(new UpdateMappingSourceCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id),
            request.SourceContent,
            request.Notes,
            userId), cancellationToken);

        return Ok(ApiResponse<ArtifactToSpecMappingDto>.Ok(result));
    }

    [HttpDelete("mappings/{id}")]
    [ResourceScope<ArtifactToSpecMapping>("id")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> DeleteMapping(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(new DeleteArtifactMappingCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id),
            userId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Artifact mapping not found"));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
