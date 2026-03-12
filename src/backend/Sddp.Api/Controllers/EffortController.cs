using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Effort.Commands;
using Sddp.Application.Features.Effort.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Effort controller
/// project, log, workday settings
/// </summary>
[Route("api/projects/{projectId:guid}/effort")]
[Authorize]
public class EffortController : BaseApiController
{
    private readonly ISender _sender;

    public EffortController(ISender sender)
    {
        _sender = sender;
    }

    // ============================================
    // Allocations
    // ============================================

    /// <summary>
    /// get
    /// </summary>
    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string? userIds = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var userIdList = ParseUserIds(userIds);

        var result = await _sender.Send(new GetEffortAllocationsQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            start,
            end,
            userIdList), cancellationToken);

        return Ok(ApiResponse<List<EffortAllocationDto>>.Ok(result));
    }

    /// <summary>
    /// create/update
    /// </summary>
    [HttpPost("allocations")]
    [Authorize]
    public async Task<IActionResult> UpsertAllocation(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpsertEffortAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new UpsertEffortAllocationCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            userId,
            request), cancellationToken);

        return Ok(ApiResponse<EffortAllocationDto>.Ok(result));
    }

    /// <summary>
    /// create/update
    /// </summary>
    [HttpPost("allocations/bulk")]
    [Authorize]
    public async Task<IActionResult> BulkUpsertAllocations(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] BulkEffortAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new BulkUpsertEffortAllocationsCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            userId,
            request), cancellationToken);

        return Ok(ApiResponse<List<EffortAllocationDto>>.Ok(result));
    }

    /// <summary>
    /// delete
    /// </summary>
    [HttpDelete("allocations/{allocationId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAllocation(
        Guid projectId,
        Guid allocationId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _sender.Send(new DeleteEffortAllocationCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            GlobalUniqueId.FromGuid(allocationId)), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Allocation not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    // ============================================
    // Worklogs
    // ============================================

    /// <summary>
    /// log get
    /// </summary>
    [HttpGet("worklogs")]
    public async Task<IActionResult> GetWorklogs(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string? userIds = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var userIdList = ParseUserIds(userIds);

        var result = await _sender.Send(new GetWorklogsQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            start,
            end,
            userIdList), cancellationToken);

        return Ok(ApiResponse<List<WorklogDto>>.Ok(result));
    }

    /// <summary>
    /// log create
    /// </summary>
    [HttpPost("worklogs")]
    [Authorize]
    public async Task<IActionResult> CreateWorklog(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateWorklogRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _sender.Send(new CreateWorklogCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            request));

        return Created($"/api/projects/{projectId}/effort/worklogs/{result.Id}", ApiResponse<WorklogDto>.Ok(result));
    }

    /// <summary>
    /// log update
    /// </summary>
    [HttpPut("worklogs/{worklogId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateWorklog(
        Guid projectId,
        Guid worklogId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateWorklogRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _sender.Send(new UpdateWorklogCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            GlobalUniqueId.FromGuid(worklogId),
            request), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Worklog not found"));
        }

        return Ok(ApiResponse<WorklogDto>.Ok(result));
    }

    /// <summary>
    /// log delete
    /// </summary>
    [HttpDelete("worklogs/{worklogId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteWorklog(
        Guid projectId,
        Guid worklogId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _sender.Send(new DeleteWorklogCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            GlobalUniqueId.FromGuid(worklogId)), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Worklog not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    // ============================================
    // Working Days
    // ============================================

    /// <summary>
    /// workday settings get
    /// </summary>
    [HttpGet("working-days")]
    public async Task<IActionResult> GetWorkingDays(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);

        var result = await _sender.Send(new GetWorkingDaysQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            start,
            end), cancellationToken);

        return Ok(ApiResponse<List<WorkingDayDto>>.Ok(result));
    }

    /// <summary>
    /// workday settings
    /// </summary>
    [HttpPost("working-days")]
    [Authorize]
    public async Task<IActionResult> SetWorkingDay(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] SetWorkingDayRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new SetWorkingDayCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            userId,
            request), cancellationToken);

        return Ok(ApiResponse<WorkingDayDto>.Ok(result));
    }

    /// <summary>
    /// workday settings
    /// </summary>
    [HttpPost("working-days/bulk")]
    [Authorize]
    public async Task<IActionResult> BulkSetWorkingDays(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] BulkWorkingDaysRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new BulkSetWorkingDaysCommand(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            userId,
            request), cancellationToken);

        return Ok(ApiResponse<List<WorkingDayDto>>.Ok(result));
    }

    // ============================================
    // Summary & Aggregation
    // ============================================

    /// <summary>
    /// get
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetMembersSummary(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);

        var result = await _sender.Send(new GetMembersSummaryQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            start,
            end), cancellationToken);

        return Ok(ApiResponse<List<MemberEffortSummaryDto>>.Ok(result));
    }

    /// <summary>
    /// user get
    /// </summary>
    [HttpGet("users/{userId:guid}/daily")]
    public async Task<IActionResult> GetMemberDailyEffort(
        Guid projectId,
        Guid userId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);

        var result = await _sender.Send(new GetMemberDailyEffortQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            GlobalUniqueId.FromGuid(userId),
            start,
            end), cancellationToken);

        return Ok(ApiResponse<List<DailyEffortDto>>.Ok(result));
    }

    /// <summary>
    /// user get
    /// </summary>
    [HttpGet("users/{userId:guid}/ownership")]
    public async Task<IActionResult> GetMemberOwnership(
        Guid projectId,
        Guid userId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string? type = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var result = await _sender.Send(new GetMemberOwnershipQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            GlobalUniqueId.FromGuid(userId),
            type,
            q,
            page,
            pageSize), cancellationToken);

        return Ok(ApiResponse<MemberOwnershipPageDto>.Ok(result));
    }

    /// <summary>
    /// get
    /// </summary>
    [HttpGet("conflicts")]
    public async Task<IActionResult> GetConflicts(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);

        var result = await _sender.Send(new GetEffortConflictsQuery(
            tenantId,
            GlobalUniqueId.FromGuid(projectId),
            start,
            end), cancellationToken);

        return Ok(ApiResponse<List<AllocationConflictDto>>.Ok(result));
    }

    // ============================================
    // Helpers
    // ============================================

    private static List<GlobalUniqueId>? ParseUserIds(string? userIds)
    {
        if (string.IsNullOrWhiteSpace(userIds))
            return null;

        return userIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => GlobalUniqueId.FromGuid(Guid.Parse(id.Trim())))
            .ToList();
    }
}
