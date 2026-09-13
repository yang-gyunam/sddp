using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Domain.Entities;
using Sddp.Application.Features.Tasks.Commands;
using Sddp.Application.Features.Tasks.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Task controller
/// </summary>
[Route("api/tasks")]
[Authorize]
public class TasksController : BaseApiController
{
    private readonly ISender _sender;
    private readonly IScopedAuthorizationService _authorizationService;

    public TasksController(ISender sender, IScopedAuthorizationService authorizationService)
    {
        _sender = sender;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Task get (,)
    /// </summary>
    [HttpGet]
    [ResourceScopeExempt("Listing endpoint: no resource id in the route, so the project comes from the header as before.")]
    public async Task<IActionResult> GetTasks(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TaskItemStatus? status = null,
        [FromQuery] TaskItemPriority? priority = null,
        [FromQuery] bool myTasksOnly = false,
        [FromQuery] Guid? categoryId = null,
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

        GlobalUniqueId? assigneeId = null;
        if (myTasksOnly)
        {
            var userError = RequireUserId(out var userId);
            if (userError is not null)
            {
                return userError;
            }
            assigneeId = userId;
        }

        GlobalUniqueId? categoryFilter = categoryId.HasValue ? GlobalUniqueId.FromGuid(categoryId.Value) : null;

        var result = await _sender.Send(new GetTasksQuery(
            tenantId, projectId, assigneeId, page, pageSize, status, priority, categoryFilter), cancellationToken);
        return Ok(ApiResponse<TaskItemPageDto>.Ok(result));
    }

    /// <summary>
    /// Task get
    /// </summary>
    [HttpGet("{id}")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    public async Task<IActionResult> GetTaskById(
        Guid id,
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

        var taskId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetTaskByIdQuery(tenantId, taskId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Ok(ApiResponse<TaskItemDetailDto>.Ok(result));
    }

    /// <summary>
    /// Task search ()
    /// </summary>
    [HttpGet("search")]
    [ResourceScopeExempt("Search endpoint: no resource id in the route; results are filtered by the requested scope.")]
    public async Task<IActionResult> SearchTasks(
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

        TryGetProjectId(projectIdHeader, out var projectId);

        var result = await _sender.Send(new SearchTasksQuery(tenantId, projectId, q, limit), cancellationToken);
        return Ok(ApiResponse<IEnumerable<TaskSearchResultDto>>.Ok(result));
    }

    /// <summary>
    /// Task create
    /// </summary>
    [HttpPost]
    [ResourceScopeExempt("Creation endpoint: the resource does not exist yet, so there is nothing to resolve a project from.")]
    [RequirePermission(PermissionCodes.TaskCreate)]
    [Authorize]
    public async Task<IActionResult> CreateTask(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateTaskItemDto dto,
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

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var result = await _sender.Send(new CreateTaskCommand(tenantId, projectId, userId, dto), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create task"));
        }

        return Created($"/api/tasks/{result.Id}", ApiResponse<TaskItemDetailDto>.Ok(result));
    }

    /// <summary>
    /// Task (, description, priority, assignee,)
    /// </summary>
    [HttpPut("{id}")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateTaskItemDto dto,
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

        var taskId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateTaskCommand(tenantId, taskId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Ok(ApiResponse<TaskItemDetailDto>.Ok(result));
    }

    /// <summary>
    /// Task status change
    /// </summary>
    [HttpPut("{id}/status")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateTaskStatus(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateTaskStatusDto dto,
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

        var taskId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateTaskStatusCommand(tenantId, taskId, userId, dto.NewStatus), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Ok(ApiResponse<TaskItemDto>.Ok(result));
    }

    /// <summary>
    ///
    /// </summary>
    [HttpPost("{id}/time-logs")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> AddTimeLog(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateTaskTimeLogDto dto,
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

        var taskId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new AddTaskTimeLogCommand(tenantId, taskId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Created($"/api/tasks/{id}/time-logs/{result.Id}", ApiResponse<TaskTimeLogDto>.Ok(result));
    }

    /// <summary>
    /// Task delete (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskDelete)]
    [Authorize]
    public async Task<IActionResult> DeleteTask(
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

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var taskId = GlobalUniqueId.FromGuid(id);
        var isAdmin = await _authorizationService.HasAnyRoleAsync(
            userId,
            tenantId,
            projectId,
            [RoleType.Admin],
            cancellationToken);

        var result = await _sender.Send(new DeleteTaskCommand(tenantId, taskId, userId, projectId, isAdmin), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Task get
    /// </summary>
    [HttpGet("my-stats")]
    [ResourceScopeExempt("Personal aggregate keyed by the caller's own user id, with no single resource in the route.")]
    public async Task<IActionResult> GetMyStats(
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

        var result = await _sender.Send(new GetMyTaskStatsQuery(tenantId, userId), cancellationToken);
        return Ok(ApiResponse<MyTaskStatsDto>.Ok(result));
    }

    // ============================================
    // Backlog Endpoints
    // ============================================

    /// <summary>
    /// Backlog (project task)
    /// </summary>
    [HttpGet("backlog/summary")]
    [ResourceScopeExempt("Cross-project personal summary keyed by the caller's own user id.")]
    public async Task<IActionResult> GetBacklogSummary(
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

        var result = await _sender.Send(new GetBacklogSummaryQuery(tenantId, userId), cancellationToken);
        return Ok(ApiResponse<BacklogSummaryDto>.Ok(result));
    }

    /// <summary>
    /// Backlog project (status/priority,, assignee)
    /// </summary>
    [HttpGet("backlog/stats")]
    [ResourceScopeExempt("Project-wide aggregate with no resource id in the route; scoped by the requested project.")]
    public async Task<IActionResult> GetBacklogStats(
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

        var result = await _sender.Send(new GetBacklogStatsQuery(tenantId, projectId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Project not found"));
        }

        return Ok(ApiResponse<BacklogStatsDto>.Ok(result));
    }

    /// <summary>
    /// Task change (Kanban)
    /// </summary>
    [HttpPut("{id}/position")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateTaskPosition(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateTaskPositionDto dto,
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

        var taskId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateTaskPositionCommand(tenantId, taskId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Ok(ApiResponse<TaskItemDto>.Ok(result));
    }

    /// <summary>
    /// Task
    /// </summary>
    [HttpPost("{id}/linked-items")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> AddLinkedItem(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateTaskLinkedItemDto dto,
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

        var taskId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new AddTaskLinkedItemCommand(tenantId, taskId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Task not found"));
        }

        return Created($"/api/tasks/{id}/linked-items/{result.Id}", ApiResponse<TaskLinkedItemDto>.Ok(result));
    }

    /// <summary>
    /// Task
    /// </summary>
    [HttpDelete("{id}/linked-items/{linkedItemId}")]
    [ResourceScope<TaskItem>("id", OnNullProject = NullProjectPolicy.Personal)]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> RemoveLinkedItem(
        Guid id,
        Guid linkedItemId,
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

        var taskId = GlobalUniqueId.FromGuid(id);
        var linkedId = GlobalUniqueId.FromGuid(linkedItemId);

        var success = await _sender.Send(
            new RemoveTaskLinkedItemCommand(tenantId, taskId, linkedId, userId), cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Linked item not found"));
        }

        return NoContent();
    }
}
