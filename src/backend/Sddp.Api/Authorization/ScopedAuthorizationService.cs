using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Api.Authorization;

public interface IScopedAuthorizationService
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(
        GlobalUniqueId userId, GlobalUniqueId tenantId, GlobalUniqueId? projectId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyRoleAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        IReadOnlyCollection<RoleType> roleTypes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves active roles for the current tenant and project instead of trusting JWT role claims.
/// </summary>
public sealed class ScopedAuthorizationService : IScopedAuthorizationService
{
    private readonly IUnitOfWork _unitOfWork;

    public ScopedAuthorizationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        GlobalUniqueId userId, GlobalUniqueId tenantId, GlobalUniqueId? projectId,
        CancellationToken cancellationToken = default)
    {
        var roles = await ResolveScopedRolesAsync(userId, tenantId, projectId, cancellationToken);
        if (roles.Count == 0) return [];
        var permissions = await _unitOfWork.Repository<Permission>().FindAsync(p => p.IsActive, cancellationToken);
        if (roles.Any(r => r.Type == RoleType.Admin))
            return permissions.Where(p => p.IsActive).Select(p => p.Code).Distinct().Order().ToArray();
        var roleIds = roles.Select(r => r.Id).ToHashSet();
        var mappings = await _unitOfWork.Repository<RolePermission>().FindAsync(
            m => m.IsActive && roleIds.Contains(m.RoleId), cancellationToken);
        var permissionIds = mappings.Where(m => m.IsActive).Select(m => m.PermissionId).ToHashSet();
        return permissions.Where(p => p.IsActive && permissionIds.Contains(p.Id))
            .Select(p => p.Code).Distinct().Order().ToArray();
    }

    public async Task<bool> HasPermissionAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var scopedRoles = await ResolveScopedRolesAsync(
            userId, tenantId, projectId, cancellationToken);

        if (scopedRoles.Count == 0)
        {
            return false;
        }

        // Tenant admins have every permission inside their tenant.
        if (scopedRoles.Any(role => role.Type == RoleType.Admin))
        {
            return true;
        }

        var permissions = await _unitOfWork.Repository<Permission>().FindAsync(
            permission => permission.IsActive && permission.Code == permissionCode,
            cancellationToken);
        var permission = permissions.SingleOrDefault(candidate => candidate.IsActive);
        if (permission is null)
        {
            return false;
        }

        var roleIds = scopedRoles.Select(role => role.Id).ToHashSet();
        var mappings = await _unitOfWork.Repository<RolePermission>().FindAsync(
            mapping => mapping.IsActive
                && mapping.PermissionId == permission.Id
                && roleIds.Contains(mapping.RoleId),
            cancellationToken);

        return mappings.Any(mapping => mapping.IsActive);
    }

    public async Task<bool> HasAnyRoleAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        IReadOnlyCollection<RoleType> roleTypes,
        CancellationToken cancellationToken = default)
    {
        var scopedRoles = await ResolveScopedRolesAsync(
            userId, tenantId, projectId, cancellationToken);
        return scopedRoles.Any(role => roleTypes.Contains(role.Type));
    }

    private async Task<IReadOnlyList<Role>> ResolveScopedRolesAsync(
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        CancellationToken cancellationToken)
    {
        var assignments = await _unitOfWork.Repository<UserRole>().FindAsync(
            assignment => assignment.IsActive
                && assignment.UserId == userId
                && assignment.TenantId.HasValue
                && assignment.TenantId.Value == tenantId,
            cancellationToken);

        assignments = assignments.Where(assignment => assignment.IsActive).ToList();

        if (assignments.Count == 0)
        {
            return [];
        }

        var assignedRoleIds = assignments.Select(assignment => assignment.RoleId).ToHashSet();
        var roles = await _unitOfWork.Repository<Role>().FindAsync(
            role => role.IsActive && assignedRoleIds.Contains(role.Id),
            cancellationToken);
        roles = roles.Where(role => role.IsActive).ToList();
        var rolesById = roles.ToDictionary(role => role.Id);

        var scopedRoleIds = assignments
            .Where(assignment => rolesById.TryGetValue(assignment.RoleId, out var role)
                && IsAssignmentInScope(assignment, role.Type, projectId))
            .Select(assignment => assignment.RoleId)
            .ToHashSet();

        return roles.Where(role => scopedRoleIds.Contains(role.Id)).ToList();
    }

    private static bool IsAssignmentInScope(
        UserRole assignment,
        RoleType roleType,
        GlobalUniqueId? projectId)
    {
        if (roleType is RoleType.Admin)
        {
            return !assignment.ProjectId.HasValue;
        }

        return projectId.HasValue
            && assignment.ProjectId.HasValue
            && assignment.ProjectId.Value == projectId.Value;
    }
}
