using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Api.Authorization;
using Sddp.Application.Features.AuditLogs.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// audit log controller
/// audit log get ()
/// </summary>
[Route("api/audit-logs")]
[Authorize(Policy = "CanReadAuditLogs")]
public class AuditLogsController : BaseApiController
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// audit log get (+)
    /// GET /api/audit-logs
    /// </summary>
    [HttpGet]
        [ResourceScopeExempt("Listing endpoint with no resource id in the route; gated by the AuditRead permission evaluated in the caller's scope.")]
    public async Task<IActionResult> GetAuditLogs(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        GlobalUniqueId? actorId = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (!GlobalUniqueId.TryParse(userId, out var parsed))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Auth.InvalidUser, "Invalid user ID format"));
            }
            actorId = parsed;
        }

        DateTimeOffset? start = null;
        if (!string.IsNullOrWhiteSpace(startDate))
        {
            if (!DateTimeOffset.TryParse(startDate, out var parsedStart))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidQuery, "Invalid start date format"));
            }
            start = parsedStart;
        }

        DateTimeOffset? end = null;
        if (!string.IsNullOrWhiteSpace(endDate))
        {
            if (!DateTimeOffset.TryParse(endDate, out var parsedEnd))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidQuery, "Invalid end date format"));
            }
            end = parsedEnd;
        }

        var result = await _sender.Send(
            new GetAuditLogsQuery(
                TenantId: tenantId,
                ProjectId: TryGetProjectId(out var projectId) ? projectId : null,
                actorId,
                action,
                resourceType,
                ExcludedResourceTypes: null,
                start,
                end,
                pageNumber,
                pageSize),
            cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }

    /// <summary>
    /// audit log get
    /// GET /api/audit-logs/{id}
    /// </summary>
    [HttpGet("{id}")]
        [ResourceScope<AuditLog>("id", OnNullProject = NullProjectPolicy.Tenant)]
    public async Task<IActionResult> GetAuditLogById(
        string id,
        CancellationToken cancellationToken)
    {
        if (!GlobalUniqueId.TryParse(id, out var logId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid audit log ID format"));
        }

        var log = await _sender.Send(new GetAuditLogByIdQuery(logId), cancellationToken);
        if (log is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Audit log not found"));
        }
        return Ok(ApiResponse<AuditLogDto>.Ok(log));
    }
}
