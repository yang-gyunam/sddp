using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
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

    [HttpGet("{id:guid}")]
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

    [HttpGet("by-path")]
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

    [HttpPost("{id:guid}/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateArtifact(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
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

        var result = await _sender.Send(new RegenerateArtifactCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id)), cancellationToken);
        return Ok(ApiResponse<ArtifactRegenerateResult>.Ok(result));
    }

    [HttpPost("verify")]
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
    [HttpDelete("{id:guid}")]
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
    [HttpPut("{id:guid}/activate")]
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

    [HttpGet("mappings/{id:guid}")]
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

    [HttpPut("mappings/{id:guid}/source")]
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

    [HttpDelete("mappings/{id:guid}")]
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
