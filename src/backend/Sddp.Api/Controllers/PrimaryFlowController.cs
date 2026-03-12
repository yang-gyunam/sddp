using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.PrimaryFlow.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// Primary Flow get controller
/// FK: Conversation → Requirement → Spec → GlossaryTerm / Artifact
/// </summary>
[Route("api/projects/{projectId:guid}/primary-flow")]
[Authorize]
public class PrimaryFlowController : BaseApiController
{
    private readonly ISender _sender;

    public PrimaryFlowController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetPrimaryFlow(
        Guid projectId,
        [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken)
    {
        var tenantError = RequireTenantId(tenantIdHeader, out var tenantId);
        if (tenantError is not null)
        {
            return tenantError;
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            return ValidationErrorResponse("entityType is required");
        }

        var parsedProjectId = GlobalUniqueId.FromGuid(projectId);
        var parsedEntityId = GlobalUniqueId.FromGuid(entityId);

        var result = await _sender.Send(
            new GetPrimaryFlowQuery(tenantId, parsedProjectId, entityType, parsedEntityId),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PrimaryFlowNodeDto>>.Ok(result));
    }
}
