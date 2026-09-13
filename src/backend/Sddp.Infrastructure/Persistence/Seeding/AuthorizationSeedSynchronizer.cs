using Microsoft.EntityFrameworkCore;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.Enums;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Reconciles the EF authorization seed with the executable role-permission contract.
/// </summary>
internal static class AuthorizationSeedSynchronizer
{
    internal static async Task<AuthorizationSeedSyncResult> SynchronizeAsync(
        SddpDbContext context,
        CancellationToken cancellationToken = default)
    {
        var createdRoles = await SynchronizeRolesAsync(context, cancellationToken).ConfigureAwait(false);
        var createdPermissions = await SynchronizePermissionsAsync(context, cancellationToken).ConfigureAwait(false);

        var roles = await context.Roles
            .Where(role => role.IsSystemRole)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var systemRoleTypes = AuthorizationSeedIds.SystemRoleTypes.ToHashSet();
        var canonicalRoles = roles
            .Where(role => systemRoleTypes.Contains(role.Type))
            .ToList();

        var duplicateRoleType = canonicalRoles
            .GroupBy(role => role.Type)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoleType is not null)
        {
            throw new InvalidOperationException(
                $"Multiple active seed candidates exist for system role type '{duplicateRoleType.Key}'.");
        }

        var rolesByType = canonicalRoles.ToDictionary(role => role.Type);
        var missingRoleTypes = systemRoleTypes.Except(rolesByType.Keys).ToArray();
        if (missingRoleTypes.Length > 0)
        {
            throw new InvalidOperationException(
                $"System role seed is incomplete: {string.Join(", ", missingRoleTypes)}.");
        }

        var permissions = await context.Permissions
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var permissionsByCode = permissions.ToDictionary(
            permission => permission.Code,
            StringComparer.Ordinal);
        var missingPermissionCodes = PermissionCodes.All.Except(
            permissionsByCode.Keys,
            StringComparer.Ordinal).ToArray();
        if (missingPermissionCodes.Length > 0)
        {
            throw new InvalidOperationException(
                $"System permission seed is incomplete: {string.Join(", ", missingPermissionCodes)}.");
        }

        var permissionCodesById = permissions.ToDictionary(
            permission => permission.Id,
            permission => permission.Code);
        var mappings = await context.RolePermissions
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var mappingsByPair = mappings.ToDictionary(
            mapping => (mapping.RoleId, mapping.PermissionId));
        var rolesById = rolesByType.Values.ToDictionary(role => role.Id);

        var createdMappings = 0;
        var reactivatedMappings = 0;
        var deactivatedMappings = 0;

        foreach (var mapping in mappings.Where(mapping => rolesById.ContainsKey(mapping.RoleId)))
        {
            var role = rolesById[mapping.RoleId];
            var desiredCodes = RolePermissionMatrix.GetPermissions(role.Type);
            var isDesired = permissionCodesById.TryGetValue(mapping.PermissionId, out var permissionCode)
                && desiredCodes.Contains(permissionCode, StringComparer.Ordinal);

            if (!isDesired && mapping.IsActive)
            {
                mapping.Deactivate();
                deactivatedMappings++;
            }
        }

        foreach (var (roleType, role) in rolesByType)
        {
            foreach (var permissionCode in RolePermissionMatrix.GetPermissions(roleType))
            {
                var permission = permissionsByCode[permissionCode];
                if (mappingsByPair.TryGetValue((role.Id, permission.Id), out var existingMapping))
                {
                    if (!existingMapping.IsActive)
                    {
                        existingMapping.Activate();
                        reactivatedMappings++;
                    }

                    continue;
                }

                var mapping = new RolePermission(role.Id, permission.Id);
                context.RolePermissions.Add(mapping);
                mappingsByPair.Add((role.Id, permission.Id), mapping);
                createdMappings++;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthorizationSeedSyncResult(
            createdRoles,
            createdPermissions,
            createdMappings,
            reactivatedMappings,
            deactivatedMappings);
    }

    private static async Task<int> SynchronizeRolesAsync(
        SddpDbContext context,
        CancellationToken cancellationToken)
    {
        var defaultRoles = Role.CreateDefaultRoles().ToList();
        var systemRoles = await context.Roles
            .Where(role => role.IsSystemRole)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var duplicateRoleType = systemRoles
            .GroupBy(role => role.Type)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRoleType is not null)
        {
            throw new InvalidOperationException(
                $"Multiple seed candidates exist for system role type '{duplicateRoleType.Key}'.");
        }

        var systemRoleTypes = AuthorizationSeedIds.SystemRoleTypes.ToHashSet();
        var existingTypes = systemRoles.Select(role => role.Type).ToHashSet();
        var created = 0;

        foreach (var role in systemRoles.Where(role =>
                     systemRoleTypes.Contains(role.Type) && !role.IsActive))
        {
            role.Activate();
        }

        foreach (var role in defaultRoles.Where(role => !existingTypes.Contains(role.Type)))
        {
            context.Entry(role).Property(entity => entity.Id).CurrentValue =
                AuthorizationSeedIds.GetRoleId(role.Type);
            context.Roles.Add(role);
            created++;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }

    private static async Task<int> SynchronizePermissionsAsync(
        SddpDbContext context,
        CancellationToken cancellationToken)
    {
        var defaultPermissions = Permission.CreateDefaultPermissions().ToList();
        var permissions = await context.Permissions
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var permissionsByCode = permissions.ToDictionary(
            permission => permission.Code,
            StringComparer.Ordinal);
        var created = 0;

        foreach (var permission in permissions.Where(permission =>
                     PermissionCodes.All.Contains(permission.Code, StringComparer.Ordinal)))
        {
            permission.MarkAsSystemPermission();
            if (!permission.IsActive)
            {
                permission.Activate();
            }
        }

        foreach (var permission in defaultPermissions.Where(permission =>
                     !permissionsByCode.ContainsKey(permission.Code)))
        {
            context.Entry(permission).Property(entity => entity.Id).CurrentValue =
                AuthorizationSeedIds.GetPermissionId(permission.Code);
            context.Permissions.Add(permission);
            created++;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}

internal sealed record AuthorizationSeedSyncResult(
    int CreatedRoles,
    int CreatedPermissions,
    int CreatedMappings,
    int ReactivatedMappings,
    int DeactivatedMappings);
