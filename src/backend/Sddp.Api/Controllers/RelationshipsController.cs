using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Relationships.Commands;
using Sddp.Application.Features.Relationships.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// relationship DTO
/// </summary>
public record ValidateRelationshipRequestDto(
    string FromEntityType,
    string FromEntityId,
    string ToEntityType,
    string ToEntityId,
    string Type);

/// <summary>
/// relationship controller
/// </summary>
[Route("api/relationships")]
[Authorize]
public class RelationshipsController : BaseApiController
{
    private readonly ISender _sender;

    public RelationshipsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRelationship(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateRelationshipDto dto,
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

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(
            new CreateRelationshipCommand(tenantId, projectId, userId, dto),
            cancellationToken);
        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create relationship"));
        }

        return Created($"/api/relationships/{result.Id}", ApiResponse<RelationshipDto>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRelationshipById(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        Guid id,
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

        var relationshipId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(
            new GetRelationshipByIdQuery(tenantId, projectId, relationshipId),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Relationship not found"));
        }

        return Ok(ApiResponse<RelationshipDto>.Ok(result));
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetRelationshipsByEntity(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        string entityType,
        Guid entityId,
        [FromQuery] string? typeFilter,
        [FromQuery] bool includeInvalidated,
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

        if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.EntityType, "Invalid entity type"));
        }

        RelationType? parsedTypeFilter = null;
        if (!string.IsNullOrEmpty(typeFilter) && Enum.TryParse<RelationType>(typeFilter, true, out var tf))
        {
            parsedTypeFilter = tf;
        }

        var parsedEntityId = GlobalUniqueId.FromGuid(entityId);
        var result = await _sender.Send(
            new GetRelationshipsByEntityQuery(
                tenantId,
                projectId,
                parsedEntityType,
                parsedEntityId,
                parsedTypeFilter,
                includeInvalidated),
            cancellationToken);

        return Ok(ApiResponse<RelationshipListDto>.Ok(result));
    }

    [HttpGet("graph/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetRelationshipGraph(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        string entityType,
        Guid entityId,
        [FromQuery] int maxDepth,
        [FromQuery] string? typeFilter,
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

        if (!Enum.TryParse<EntityType>(entityType, true, out var parsedEntityType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.EntityType, "Invalid entity type"));
        }

        RelationType[]? parsedTypeFilter = null;
        if (!string.IsNullOrEmpty(typeFilter))
        {
            var types = typeFilter.Split(',');
            var parsedTypes = new List<RelationType>();
            foreach (var t in types)
            {
                if (Enum.TryParse<RelationType>(t.Trim(), true, out var rt))
                {
                    parsedTypes.Add(rt);
                }
            }
            if (parsedTypes.Any())
            {
                parsedTypeFilter = parsedTypes.ToArray();
            }
        }

        if (maxDepth <= 0) maxDepth = 3;
        if (maxDepth > 10) maxDepth = 10;

        var parsedEntityId = GlobalUniqueId.FromGuid(entityId);
        var result = await _sender.Send(
            new GetRelationshipGraphQuery(
                tenantId,
                projectId,
                parsedEntityType,
                parsedEntityId,
                maxDepth,
                parsedTypeFilter),
            cancellationToken);

        return Ok(ApiResponse<RelationshipGraphDto>.Ok(result));
    }

    [HttpGet("decision/{messageId:guid}")]
    public async Task<IActionResult> GetDecisionImpact(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        Guid messageId,
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

        var decisionMessageId = GlobalUniqueId.FromGuid(messageId);
        var result = await _sender.Send(
            new GetDecisionImpactQuery(tenantId, projectId, decisionMessageId),
            cancellationToken);

        return Ok(ApiResponse<DecisionImpactDto>.Ok(result));
    }

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateRelationship(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] ValidateRelationshipRequestDto dto,
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

        if (!Enum.TryParse<EntityType>(dto.FromEntityType, true, out var fromEntityType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.FromEntityType, "Invalid FromEntityType"));
        }

        if (!Enum.TryParse<EntityType>(dto.ToEntityType, true, out var toEntityType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ToEntityType, "Invalid ToEntityType"));
        }

        if (!Enum.TryParse<RelationType>(dto.Type, true, out var relationType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.RelationType, "Invalid RelationType"));
        }

        if (!GlobalUniqueId.TryParse(dto.FromEntityId, out var fromEntityId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.FromEntityId, "Invalid FromEntityId"));
        }

        if (!GlobalUniqueId.TryParse(dto.ToEntityId, out var toEntityId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ToEntityId, "Invalid ToEntityId"));
        }

        var result = await _sender.Send(
            new ValidateRelationshipQuery(
                tenantId,
                projectId,
                fromEntityType,
                fromEntityId,
                toEntityType,
                toEntityId,
                relationType),
            cancellationToken);

        return Ok(ApiResponse<RelationshipValidationResultDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> InvalidateRelationship(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        Guid id,
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

        var relationshipId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(
            new InvalidateRelationshipCommand(tenantId, projectId, relationshipId),
            cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Relationship not found or already invalidated"));
        }

        return Ok(ApiResponse<object>.Ok(new { Message = "Relationship invalidated successfully" }));
    }
}
