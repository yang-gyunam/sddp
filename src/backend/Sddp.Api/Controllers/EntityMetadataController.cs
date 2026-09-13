using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.EntityMetadata.Commands;
using Sddp.Application.Features.EntityMetadata.Queries;

namespace Sddp.Api.Controllers;

[Route("api/specs/{specId}/entities")]
[Authorize]
public class EntityMetadataController : BaseApiController
{
    private readonly ISender _sender;

    public EntityMetadataController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ResourceScope<Spec>("specId")]
    public async Task<IActionResult> GetEntities(
        Guid specId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken)
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

        var result = await _sender.Send(
            new GetEntityMetadataQuery(specId, tenantId, projectId),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Ok(ApiResponse<IReadOnlyList<EntityMetadataDto>>.Ok(result));
    }

    [HttpPost]
    [ResourceScope<Spec>("specId")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> CreateEntity(
        Guid specId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateEntityMetadataDto dto,
        CancellationToken cancellationToken)
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

        var result = await _sender.Send(
            new CreateEntityMetadataCommand(specId, tenantId, projectId, dto),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Spec not found"));
        }

        return Created($"/api/specs/{specId}/entities/{result.Id}", ApiResponse<EntityMetadataDto>.Ok(result));
    }

    [HttpPut("{entityId}")]
    [ResourceScope<Sddp.Domain.Entities.EntityMetadata>("entityId")]
    [RequirePermission(PermissionCodes.GenerationExecute)]
    [Authorize]
    public async Task<IActionResult> UpdateEntity(
        Guid specId,
        Guid entityId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateEntityMetadataDto dto,
        CancellationToken cancellationToken)
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

        var result = await _sender.Send(
            new UpdateEntityMetadataCommand(specId, entityId, tenantId, projectId, dto),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Entity not found"));
        }

        return Ok(ApiResponse<EntityMetadataDto>.Ok(result));
    }

    [HttpDelete("{entityId}")]
    [ResourceScope<Sddp.Domain.Entities.EntityMetadata>("entityId")]
    [RequirePermission(PermissionCodes.GenerationRollback)]
    [Authorize]
    public async Task<IActionResult> DeleteEntity(
        Guid specId,
        Guid entityId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken)
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

        var deleted = await _sender.Send(
            new DeleteEntityMetadataCommand(specId, entityId, tenantId, projectId),
            cancellationToken);

        if (!deleted)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Entity not found"));
        }

        return Ok(ApiResponse<object>.Ok(new { Message = "Entity deleted" }));
    }
}
