using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Application.Features.Permissions.Queries;

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
    public async Task<IActionResult> GetPermissions()
    {
        var dtos = await _sender.Send(new GetPermissionsQuery());
        return Ok(ApiResponse<IEnumerable<PermissionDto>>.Ok(dtos));
    }

    [HttpGet("by-resource")]
    public async Task<IActionResult> GetPermissionsByResource()
    {
        var grouped = await _sender.Send(new GetPermissionsByResourceQuery());
        return Ok(ApiResponse<Dictionary<string, IEnumerable<PermissionDto>>>.Ok(grouped));
    }
}
