using Sddp.Abstractions.DTOs;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Roles;

internal static class RoleMapping
{
    internal static RoleDto MapToDto(Role role, IEnumerable<string> permissionCodes)
    {
        return new RoleDto(
            Id: role.Id.ToString(),
            Name: role.Name,
            Description: role.Description,
            Type: role.Type,
            IsSystemRole: role.IsSystemRole,
            Permissions: permissionCodes);
    }
}
