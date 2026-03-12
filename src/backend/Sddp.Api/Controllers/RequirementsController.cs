using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.AuditLogs.Queries;
using Sddp.Application.Features.Requirements.Commands;
using Sddp.Application.Features.Requirements.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// requirement controller
/// </summary>
[Route("api/requirements")]
[Authorize]
public class RequirementsController : BaseApiController
{
    private readonly ISender _sender;

    public RequirementsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequirements(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RequirementLevel? level = null,
        [FromQuery] RequirementStatus? status = null,
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

        var result = await _sender.Send(new GetRequirementsQuery(tenantId, projectId, page, pageSize, level, status), cancellationToken);
        return Ok(ApiResponse<RequirementPageDto>.Ok(result));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchRequirements(
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

        var projectError = TryGetProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(ApiResponse<IEnumerable<RequirementSummaryDto>>.Ok([]));
        }

        var result = await _sender.Send(
            new SearchRequirementsQuery(tenantId, projectId, q.Trim(), Math.Min(limit, 50)),
            cancellationToken);
        return Ok(ApiResponse<IEnumerable<RequirementSummaryDto>>.Ok(result));
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetRequirementTree(
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

        var result = await _sender.Send(new GetRequirementTreeQuery(tenantId, projectId), cancellationToken);
        return Ok(ApiResponse<IEnumerable<RequirementTreeNodeDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRequirementById(
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

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var requirementId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetRequirementByIdQuery(tenantId, projectId, requirementId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDetailDto>.Ok(result));
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetRequirementByCode(
        string code,
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

        var result = await _sender.Send(new GetRequirementByCodeQuery(tenantId, projectId, code), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDetailDto>.Ok(result));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersionHistory(
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

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var requirementId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(
            new GetRequirementVersionHistoryQuery(tenantId, projectId, requirementId),
            cancellationToken);

        return Ok(ApiResponse<List<RequirementVersionDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> GetChildren(
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

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var parentId = GlobalUniqueId.FromGuid(id);
        var result = await _sender.Send(new GetRequirementChildrenQuery(tenantId, projectId, parentId), cancellationToken);

        return Ok(ApiResponse<IEnumerable<RequirementDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRequirement(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateRequirementDto dto,
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

        var result = await _sender.Send(new CreateRequirementCommand(tenantId, projectId, userId, dto), cancellationToken);

        if (result is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Crud.CreateFailed, "Failed to create requirement"));
        }

        return Created($"/api/requirements/{result.Id}", ApiResponse<RequirementDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateRequirement(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateRequirementDto dto,
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

        var requirementId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UpdateRequirementCommand(tenantId, projectId, requirementId, userId, dto), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDetailDto>.Ok(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> TransitionStatus(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] TransitionStatusDto dto,
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

        var requirementId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new TransitionRequirementStatusCommand(
            tenantId, projectId, requirementId, userId, dto.NewStatus), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteRequirement(
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

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var requirementId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new DeleteRequirementCommand(tenantId, projectId, requirementId), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse.Ok());
    }

    [HttpPost("{id:guid}/link-conversation")]
    [Authorize]
    public async Task<IActionResult> LinkConversation(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] LinkConversationDto dto,
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

        if (!GlobalUniqueId.TryParse(dto.ConversationId, out var conversationId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.InvalidEntity.ConversationId, "Invalid Conversation ID format"));
        }

        var userError = RequireUserId(out var userId);
        if (userError is not null)
        {
            return userError;
        }

        var requirementId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new LinkRequirementConversationCommand(tenantId, projectId, requirementId, conversationId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDto>.Ok(result));
    }

    [HttpDelete("{id:guid}/link-conversation")]
    [Authorize]
    public async Task<IActionResult> UnlinkConversation(
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

        var requirementId = GlobalUniqueId.FromGuid(id);

        var result = await _sender.Send(new UnlinkRequirementConversationCommand(tenantId, projectId, requirementId, userId), cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        return Ok(ApiResponse<RequirementDto>.Ok(result));
    }

    /// <summary>
    /// requirement update get
    /// </summary>
    [HttpGet("{id:guid}/field-authors")]
    public async Task<IActionResult> GetFieldAuthors(
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

        var projectError = RequireProjectId(projectIdHeader, out var projectId);
        if (projectError is not null)
        {
            return projectError;
        }

        var requirementId = GlobalUniqueId.FromGuid(id);

        // Verify requirement exists
        var requirement = await _sender.Send(new GetRequirementByIdQuery(tenantId, projectId, requirementId), cancellationToken);
        if (requirement is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Requirement not found"));
        }

        var result = await _sender.Send(
            new GetFieldAuthorsQuery("requirement", requirementId), cancellationToken);
        return Ok(ApiResponse<List<FieldAuthorDto>>.Ok(result));
    }
}
