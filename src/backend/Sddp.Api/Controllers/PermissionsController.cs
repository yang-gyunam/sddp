using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Application.Features.Permissions.Queries;
using Sddp.Api.Authorization;

namespace Sddp.Api.Controllers;

/// <summary>
/// permission controller
/// </summary>
[Route("api/permissions")]
[Authorize(Policy = "CanManageUsers")]
public class PermissionsController : BaseApiController
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ResourceScopeExempt("Permission is a global catalog entity with no ProjectId; the controller-level CanManageUsers policy is what guards the list.")]
    public async Task<IActionResult> GetPermissions()
    {
        var dtos = await _sender.Send(new GetPermissionsQuery());
        return Ok(ApiResponse<IEnumerable<PermissionDto>>.Ok(dtos));
    }

    [HttpGet("by-resource")]
    [ResourceScopeExempt("Permission is a global catalog entity with no ProjectId; the controller-level CanManageUsers policy guards this grouped view.")]
    public async Task<IActionResult> GetPermissionsByResource()
    {
        var grouped = await _sender.Send(new GetPermissionsByResourceQuery());
        return Ok(ApiResponse<Dictionary<string, IEnumerable<PermissionDto>>>.Ok(grouped));
    }
}
