using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Constants;
using Sddp.Application.Features.Sla.Commands;
using Sddp.Application.Features.Sla.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Approval SLA controller
/// </summary>
[Route("api/sla")]
[Authorize]
public class SlaPoliciesController : BaseApiController
{
    private readonly ISender _sender;

    public SlaPoliciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetSlaPoliciesByProjectQuery(tenantId, projectId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<SlaPolicyDto>>.Ok(result));
    }

    [HttpGet("policies/{id:guid}")]
    public async Task<IActionResult> GetPolicyById(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetSlaPolicyByIdQuery(tenantId, projectId, GlobalUniqueId.FromGuid(id)),
            cancellationToken);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "SLA policy not found"));
        }

        return Ok(ApiResponse<SlaPolicyDto>.Ok(result));
    }

    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] CreateSlaPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(new CreateSlaPolicyCommand(
            tenantId,
            projectId,
            request.SlaType,
            request.SlaHours,
            request.UrgentSlaHours,
            request.ReminderAtPercent,
            request.EscalationRole), cancellationToken);

        return CreatedResponse($"api/sla/policies/{result.Id}", result);
    }

    [HttpPut("policies/{id:guid}")]
    public async Task<IActionResult> UpdatePolicy(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        [FromBody] UpdateSlaPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(new UpdateSlaPolicyCommand(
            tenantId,
            projectId,
            GlobalUniqueId.FromGuid(id),
            request.SlaHours,
            request.UrgentSlaHours,
            request.ReminderAtPercent,
            request.EscalationRole), cancellationToken);

        return Ok(ApiResponse<SlaPolicyDto>.Ok(result));
    }

    [HttpDelete("policies/{id:guid}")]
    public async Task<IActionResult> DeletePolicy(
        Guid id,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(new DeleteSlaPolicyCommand(
            tenantId, projectId, GlobalUniqueId.FromGuid(id)), cancellationToken);

        if (!result)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "SLA policy not found"));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetSlaStatus(
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromHeader(Name = "X-Project-Id")] string? projectIdHeader,
        CancellationToken cancellationToken = default)
    {
        var headerError = RequireTenantAndProject(
            tenantIdHeader, projectIdHeader, out var tenantId, out var projectId,
            ApiErrorCodes.Header.MissingHeaders, "X-Tenant-Id and X-Project-Id headers are required");
        if (headerError is not null) return headerError;

        var result = await _sender.Send(
            new GetPendingSignOffSlaStatusQuery(tenantId, projectId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PendingSignOffSlaDto>>.Ok(result));
    }
}
