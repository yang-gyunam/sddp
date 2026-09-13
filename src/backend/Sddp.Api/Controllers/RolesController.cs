using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Api.Constants;
using Sddp.Api.Authorization;
using Sddp.Application.Features.Roles.Queries;

namespace Sddp.Api.Controllers;

/// <summary>
/// role controller
/// </summary>
[Route("api/roles")]
[Authorize]
public class RolesController : BaseApiController
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ResourceScopeExempt("Role is a global catalog entity with neither ProjectId nor TenantId; the CanManageUsers permission policy in the caller's tenant is the gate.")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetRoles()
    {
        var roleDtos = await _sender.Send(new GetRolesQuery());
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roleDtos));
    }

    [HttpGet("{id}")]
    [ResourceScopeExempt("Role is a global catalog entity with neither ProjectId nor TenantId, so nothing project-owned is addressed; CanManageUsers is the gate.")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetRoleById(string id)
    {
        if (!Sddp.Abstractions.ValueObjects.GlobalUniqueId.TryParse(id, out var roleId))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiErrorCodes.Validation.InvalidId, "Invalid role ID format"));
        }

        var role = await _sender.Send(new GetRoleByIdQuery(roleId));
        if (role is null)
        {
            return NotFound(ApiResponse<object>.Fail(ApiErrorCodes.Crud.NotFound, "Role not found"));
        }
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }
}
