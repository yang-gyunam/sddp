using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
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
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
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
                TenantId: null,
                ProjectId: null,
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
