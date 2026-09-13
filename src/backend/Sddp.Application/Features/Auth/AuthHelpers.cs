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
        var userRoles = await (userRoleRepo.FindAsync(
            ur => ur.UserId == userId && ur.IsActive,
            cancellationToken)).ConfigureAwait(false);
        return userRoles.Where(ur => ur.IsActive).ToList();
    }

    internal static async Task<IReadOnlyList<string>> ResolveRoleNamesAsync(
        IUnitOfWork unitOfWork,
        IEnumerable<UserRole> userRoles,
        CancellationToken cancellationToken)
    {
        var roleRepo = unitOfWork.Repository<Role>();
        var roleIds = userRoles.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToHashSet();
        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        return roles
            .Where(r => r.IsActive && roleIds.Contains(r.Id))
            .Select(r => r.Type.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    internal static async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        IUnitOfWork unitOfWork,
        IEnumerable<UserRole> userRoles,
        CancellationToken cancellationToken)
    {
        var rolePermissionRepo = unitOfWork.Repository<RolePermission>();
        var permissionRepo = unitOfWork.Repository<Permission>();

        var roleIds = userRoles.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToHashSet();

        var rolePermissions = await (rolePermissionRepo.FindAsync(
            rp => rp.IsActive && roleIds.Contains(rp.RoleId),
            cancellationToken)).ConfigureAwait(false);
        var permissionIds = rolePermissions
            .Where(rp => rp.IsActive)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var permissions = await (permissionRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        return permissions
            .Where(p => p.IsActive && permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    internal static GlobalUniqueId ResolveTenantId(IEnumerable<UserRole> userRoles, string? requestedTenantId = null)
    {
        var tenantIds = userRoles.Where(ur => ur.TenantId.HasValue && ur.IsActive)
            .Select(ur => ur.TenantId!.Value).Where(id => id != GlobalUniqueId.Empty).Distinct().ToList();
        if (requestedTenantId is not null)
        {
            if (!GlobalUniqueId.TryParse(requestedTenantId, out var selected) || !tenantIds.Contains(selected))
                throw new UnauthorizedException("Invalid tenant selection");
            return selected;
        }
        if (tenantIds.Count == 0) throw new UnauthorizedException("No tenant-scoped active role found for user.");
        if (tenantIds.Count > 1)
            throw new TenantSelectionRequiredException(tenantIds.Select(id => id.ToString()).Order(StringComparer.Ordinal).ToList());
        return tenantIds[0];
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
