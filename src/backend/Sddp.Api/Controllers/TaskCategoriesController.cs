using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Constants;
using Sddp.Application.Features.TaskCategories.Commands;
using Sddp.Application.Features.TaskCategories.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// user task
/// </summary>
[Route("api/task-categories")]
[Authorize]
public class TaskCategoriesController : BaseApiController
{
    private readonly ISender _sender;

    public TaskCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// get
    /// </summary>
    [HttpGet]
    [ResourceScopeExempt("Personal listing with no resource id in the route; TaskCategory carries no project and the query returns only the categories owned by the caller's own user id.")]
    public async Task<IActionResult> GetCategories(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new GetTaskCategoriesQuery(tenantId, userId), cancellationToken);

        return Ok(ApiResponse<IEnumerable<TaskCategoryDto>>.Ok(result));
    }

    /// <summary>
    /// create
    /// </summary>
    [HttpPost]
    [ResourceScopeExempt("Creation endpoint for a personal resource that carries no project; the new category is bound to the caller's own user id, so there is no project scope to assert.")]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> CreateCategory(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] CreateTaskCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var result = await _sender.Send(
            new CreateTaskCategoryCommand(tenantId, userId, dto), cancellationToken);

        if (result is null)
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create category"));

        return Created($"/api/task-categories/{result.Id}",
            ApiResponse<TaskCategoryDto>.Ok(result));
    }

    /// <summary>
    /// update
    /// </summary>
    [HttpPut("{id}")]
    [ResourceScopeExempt("TaskCategory carries no project, so project scope cannot apply; the handler rejects any category whose UserId is not the caller's, which is the ownership check that stands in for it.")]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] UpdateTaskCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var categoryId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(
            new UpdateTaskCategoryCommand(tenantId, userId, categoryId, dto), cancellationToken);

        if (result is null)
            return NotFoundResponse("TaskCategory");

        return Ok(ApiResponse<TaskCategoryDto>.Ok(result));
    }

    /// <summary>
    /// delete (delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ResourceScopeExempt("TaskCategory carries no project, so project scope cannot apply; the handler refuses to delete a category whose UserId is not the caller's, which is the ownership check that stands in for it.")]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        var categoryId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(
            new DeleteTaskCategoryCommand(tenantId, userId, categoryId), cancellationToken);

        if (!result)
            return NotFoundResponse("TaskCategory");

        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// change
    /// </summary>
    [HttpPut("reorder")]
    [ResourceScopeExempt("Reorder takes its ids in the request body, not the route; TaskCategory carries no project and the handler silently skips any category the caller does not own.")]
    [RequirePermission(PermissionCodes.TaskUpdate)]
    [Authorize]
    public async Task<IActionResult> ReorderCategories(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromBody] ReorderTaskCategoriesDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null) return tenantError;

        var userError = RequireUserId(out var userId);
        if (userError is not null) return userError;

        await _sender.Send(
            new ReorderTaskCategoriesCommand(tenantId, userId, dto), cancellationToken);

        return Ok(ApiResponse.Ok());
    }
}
