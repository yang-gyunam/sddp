using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.Dashboard.Queries;
using Sddp.Application.Features.Notifications;

namespace Sddp.Api.Controllers;

/// <summary>
/// Dashboard controller
/// Fetch aggregated dashboard data (personal/system)
/// </summary>
[Route("api/dashboard")]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get personal dashboard
    /// GET /api/dashboard/my
    /// </summary>
    [HttpGet("my")]
        [ResourceScopeExempt("Personal aggregate keyed by the caller's own user id; no resource id in the route.")]
    public async Task<IActionResult> GetMyDashboard(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
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

        var dashboard = await _sender.Send(new GetMyDashboardQuery(tenantId, userId), cancellationToken);
        return Ok(ApiResponse<MyDashboardDto>.Ok(dashboard));
    }

    /// <summary>
    /// Get system dashboard (admin)
    /// GET /api/dashboard/system
    /// </summary>
    [HttpGet("system")]
        [ResourceScopeExempt("Tenant-wide aggregate with no resource id in the route; bounded by the tenant context.")]
    public async Task<IActionResult> GetSystemDashboard(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        var dashboard = await _sender.Send(new GetSystemDashboardQuery(tenantId), cancellationToken);
        return Ok(ApiResponse<SystemDashboardDto>.Ok(dashboard));
    }

    #region My Dashboard - Split Endpoints

    /// <summary>
    /// Personal dashboard - overview
    /// GET /api/dashboard/my/overview
    /// </summary>
    [HttpGet("my/overview")]
        [ResourceScopeExempt("Personal overview keyed by the caller's own user id; no resource id in the route.")]
    public async Task<IActionResult> GetMyOverview(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(
            tenantIdHeader,
            out var tenantId,
            errorMessage: "Invalid tenant");
        if (tenantError is not null)
        {
            return tenantError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var overview = await _sender.Send(new GetMyOverviewQuery(tenantId, userId), cancellationToken);
        return Ok(ApiResponse<MyOverviewDto>.Ok(overview));
    }

    /// <summary>
    /// Personal dashboard - my tasks
    /// GET /api/dashboard/my/tasks
    /// </summary>
    [HttpGet("my/tasks")]
        [ResourceScopeExempt("Personal task aggregate with no resource id in the route; bounded by the caller.")]
    public async Task<IActionResult> GetMyTasks()
    {
        // Task entity not implemented yet - return an empty list
        var tasks = await _sender.Send(new GetMyTasksQuery());
        return Ok(ApiResponse<MyTasksDto>.Ok(tasks));
    }

    /// <summary>
    /// Personal dashboard - recent activities
    /// GET /api/dashboard/my/activities?page=1&pageSize=20
    /// </summary>
    [HttpGet("my/activities")]
        [ResourceScopeExempt("Personal activity feed keyed by the caller's own user id; no resource id in the route.")]
    public async Task<IActionResult> GetMyActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userError = RequireUserId(out var userId, errorMessage: "Invalid user");
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new GetMyActivitiesQuery(userId, page, pageSize), cancellationToken);
        return Ok(ApiResponse<MyActivitiesDto>.Ok(result));
    }

    /// <summary>
    /// Personal dashboard - notifications
    /// GET /api/dashboard/my/notifications?page=1&pageSize=20
    /// </summary>
    [HttpGet("my/notifications")]
        [ResourceScopeExempt("Listing bounded by tenant and the caller's own recipient id, with no resource id in the route.")]
    public async Task<IActionResult> GetMyNotifications(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
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

        var notifications = await _sender.Send(
            new GetMyNotificationsQuery(tenantId, userId, page, pageSize), cancellationToken);
        return Ok(ApiResponse<MyNotificationsDto>.Ok(notifications));
    }

    /// <summary>
    /// Mark a personal notification as read
    /// PATCH /api/dashboard/my/notifications/{id}/read
    /// </summary>
    [HttpPatch("my/notifications/{id}/read")]
        [ResourceScopeExempt("Notification carries no owning project; the handler accepts the id only when tenant and recipient both match the caller.")]
    [MutationAuthorizationExempt("A user may update only their own notification read state.")]
    [Authorize]
    public async Task<IActionResult> MarkNotificationRead(
        [FromRoute] string id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
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

        if (!GlobalUniqueId.TryParse(id, out var notificationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid notification ID"));
        }

        var result = await _sender.Send(
            new MarkNotificationReadCommand(tenantId, notificationId, userId), cancellationToken);

        if (!result)
        {
            return NotFoundResponse("Notification");
        }

        return Ok(ApiResponse<object>.Ok(new { success = true }));
    }

    /// <summary>
    /// Mark all personal notifications as read
    /// PATCH /api/dashboard/my/notifications/read-all
    /// </summary>
    [HttpPatch("my/notifications/read-all")]
        [ResourceScopeExempt("Bulk update restricted to the caller's own notifications; no resource id appears in the route.")]
    [MutationAuthorizationExempt("A user may update only their own notification read state.")]
    [Authorize]
    public async Task<IActionResult> MarkAllNotificationsRead(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
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

        var count = await _sender.Send(
            new MarkAllNotificationsReadCommand(tenantId, userId), cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { markedCount = count }));
    }

    /// <summary>
    /// Personal dashboard - phase 1 widget data (batch)
    /// GET /api/dashboard/my/widgets
    /// </summary>
    [HttpGet("my/widgets")]
        [ResourceScopeExempt("Personal widget batch keyed by the caller's own user id; no resource id in the route.")]
    public async Task<IActionResult> GetMyDashboardWidgets(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
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

        var widgets = await _sender.Send(
            new GetMyDashboardWidgetsQuery(tenantId, userId), cancellationToken);
        return Ok(ApiResponse<MyDashboardWidgetsDto>.Ok(widgets));
    }

    #endregion

    #region Project Dashboard

    /// <summary>
    /// project dashboard get
    /// GET /api/dashboard/projects/{projectId}
    /// </summary>
    [HttpGet("projects/{projectId}")]
        [ResourceScope<Project>("projectId")]
    public async Task<IActionResult> GetProjectDashboard(
        [FromRoute] string projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (!GlobalUniqueId.TryParse(projectId, out var parsedProjectId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.Project, "Invalid project ID"));
        }

        var dashboard = await _sender.Send(
            new GetProjectDashboardQuery(tenantId, parsedProjectId), cancellationToken);

        if (dashboard is null)
        {
            return NotFoundResponse("Project");
        }

        return Ok(ApiResponse<ProjectDashboardDto>.Ok(dashboard));
    }

    #endregion

    #region System Dashboard - Split Endpoints

    /// <summary>
    /// system dashboard -
    /// GET /api/dashboard/system/stats
    /// </summary>
    [HttpGet("system/stats")]
        [ResourceScopeExempt("Tenant-wide statistics with no resource id in the route; bounded by the tenant context.")]
    public async Task<IActionResult> GetSystemStats(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(
            tenantIdHeader,
            out var tenantId,
            errorMessage: "Invalid tenant");
        if (tenantError is not null)
        {
            return tenantError;
        }

        var stats = await _sender.Send(new GetSystemStatsQuery(tenantId), cancellationToken);
        return Ok(ApiResponse<SystemStatsDto>.Ok(stats));
    }

    /// <summary>
    /// system dashboard - audit log
    /// GET /api/dashboard/system/audit-logs?page=1&pageSize=50
    /// </summary>
    [Authorize(Policy = "CanReadAuditLogs")]
    [HttpGet("system/audit-logs")]
        [ResourceScopeExempt("Listing endpoint with no resource id in the route; gated by the AuditRead permission evaluated in the caller's scope.")]
    public async Task<IActionResult> GetSystemAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSystemAuditLogsQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<AuditLogsDto>.Ok(result));
    }

    /// <summary>
    /// system dashboard -
    /// GET /api/dashboard/system/health
    /// </summary>
    [HttpGet("system/health")]
        [ResourceScopeExempt("System health probe that reads no tenant or project owned data whatsoever.")]
    public async Task<IActionResult> GetSystemHealth()
    {
        // - status
        var health = await _sender.Send(new GetSystemHealthQuery());
        return Ok(ApiResponse<HealthCheckDto>.Ok(health));
    }

    #endregion
}
