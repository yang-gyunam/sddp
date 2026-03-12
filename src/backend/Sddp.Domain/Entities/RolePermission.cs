using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// role-permission entity ()
/// </summary>
public class RolePermission : EntityBase
{
    /// <summary>
    /// role ID
    /// </summary>
    public GlobalUniqueId RoleId { get; private set; }

    /// <summary>
    /// permission ID
    /// </summary>
    public GlobalUniqueId PermissionId { get; private set; }

    /// <summary>
    /// role
    /// </summary>
    public Role? Role { get; private set; }

    /// <summary>
    /// permission
    /// </summary>
    public Permission? Permission { get; private set; }

    // EF Core default create
    private RolePermission() { }

    public RolePermission(GlobalUniqueId roleId, GlobalUniqueId permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
