using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Auth;

internal static class AuthHelpers
{
    internal static async Task<IReadOnlyList<UserRole>> GetUserRolesEntitiesAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var userRoleRepo = unitOfWork.Repository<UserRole>();
        return await (userRoleRepo.FindAsync(ur => ur.UserId == userId, cancellationToken)).ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyList<string>> ResolveRoleNamesAsync(
        IUnitOfWork unitOfWork,
        IEnumerable<UserRole> userRoles,
        CancellationToken cancellationToken)
    {
        var roleRepo = unitOfWork.Repository<Role>();
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        return roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Type.ToString()).ToList();
    }

    internal static async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var userRoleRepo = unitOfWork.Repository<UserRole>();
        var rolePermissionRepo = unitOfWork.Repository<RolePermission>();
        var permissionRepo = unitOfWork.Repository<Permission>();

        var userRoles = await (userRoleRepo.FindAsync(ur => ur.UserId == userId, cancellationToken)).ConfigureAwait(false);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

        var rolePermissions = await (rolePermissionRepo.FindAsync(rp => roleIds.Contains(rp.RoleId), cancellationToken)).ConfigureAwait(false);
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).ToList();

        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        return permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Code).ToList();
    }

    internal static GlobalUniqueId ResolveTenantId(IEnumerable<UserRole> userRoles)
    {
        var tenantId = userRoles
            .Where(ur => ur.TenantId != null && ur.IsActive)
            .Select(ur => ur.TenantId!.Value.ToGuid())
            .FirstOrDefault();

        if (tenantId == Guid.Empty)
        {
            throw new UnauthorizedException("No tenant-scoped active role found for user.");
        }

        return GlobalUniqueId.FromGuid(tenantId);
    }

    internal static UserInfo BuildUserInfo(
        User user,
        GlobalUniqueId tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        return new UserInfo(
            Id: user.Id.ToString(),
            Username: user.Username,
            DisplayName: user.DisplayName ?? user.Username,
            TenantId: tenantId.ToString(),
            Roles: roles,
            Permissions: permissions);
    }
}
