using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.AuditLogs.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// (audit log) controller
/// X-Tenant-Id, X-Project-Id
/// </summary>
[Route("api/timeline")]
[Authorize]
public class TimelineController : BaseApiController
{
    private readonly ISender _sender;

    public TimelineController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// get
    /// GET /api/timeline/by-resource/{resourceType}/{resourceId}
    /// </summary>
    [HttpGet("by-resource/{resourceType}/{resourceId:guid}")]
    [Authorize(Policy = "CanReadAuditLogs")]
    public async Task<IActionResult> GetByResource(
        string resourceType,
        Guid resourceId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out _);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ResourceType, "Resource type is required"));
        }

        var resId = GlobalUniqueId.FromGuid(resourceId);
        var entries = await _sender.Send(
            new GetTimelineByResourceQuery(resourceType, resId),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AuditLogEntry>>.Ok(entries));
    }

    /// <summary>
    /// project all get
    /// GET /api/timeline/by-project
    /// Timeline: tenantId (all user, 1000)
    /// Recent Activity: tenantId + actorId + startDate ()
    /// </summary>
    [HttpGet("by-project")]
    public async Task<IActionResult> GetByProject(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] string? actorId = null,
        [FromQuery] string? startDate = null,
        [FromQuery] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        GlobalUniqueId? projectId = null;
        if (!string.IsNullOrWhiteSpace(projectIdHeader))
        {
            if (!GlobalUniqueId.TryParse(projectIdHeader, out var parsed))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Header.InvalidProject, "Invalid X-Project-Id header"));
            }
            projectId = parsed;
        }

        GlobalUniqueId? actorGuid = null;
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            if (!GlobalUniqueId.TryParse(actorId, out var parsedActor))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.Actor, "Invalid actorId format"));
            }
            actorGuid = parsedActor;
        }

        DateTimeOffset? start = null;
        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!DateTimeOffset.TryParse(startDate, out var parsedStart))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidDate, "Invalid startDate format"));
            }
            start = parsedStart;
        }

        var result = await _sender.Send(
            new GetAuditLogsQuery(
                TenantId: tenantId,
                ProjectId: projectId,
                ActorId: actorGuid,
                Action: null,
                ResourceType: null,
                ExcludedResourceTypes: new[] { "auth" },
                StartDate: start,
                EndDate: null,
                PageNumber: 1,
                PageSize: limit),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AuditLogDto>>.Ok(result.Items));
    }

    /// <summary>
    /// user get
    /// GET /api/timeline/by-actor/{actorId}
    /// </summary>
    [HttpGet("by-actor/{actorId:guid}")]
    [Authorize(Policy = "CanReadAuditLogs")]
    public async Task<IActionResult> GetByActor(
        Guid actorId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out _);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var actId = GlobalUniqueId.FromGuid(actorId);
        var entries = await _sender.Send(new GetTimelineByActorQuery(actId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AuditLogEntry>>.Ok(entries));
    }
}
