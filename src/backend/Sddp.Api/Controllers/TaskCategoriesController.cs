using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
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
    [HttpPut("{id:guid}")]
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
    [HttpDelete("{id:guid}")]
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
