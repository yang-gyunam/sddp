using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Users;

public static class TenantUserScope
{
    /// <summary>
    /// True when <paramref name="userId"/> holds at least one active role in
    /// <paramref name="tenantId"/>.
    ///
    /// A user with no roles anywhere is deliberately not visible: they can already do nothing in
    /// any project, and admitting them here would mean admitting them to every tenant at once,
    /// since there is nothing to tell one tenant's claim on them from another's.
    /// </summary>
    public static async Task<bool> IsMemberAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId userId,
        GlobalUniqueId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        var roles = await unitOfWork.Repository<UserRole>()
            .FindAsync(
                assignment => assignment.UserId == userId
                    && assignment.TenantId == tenantId
                    && assignment.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        return roles.Count > 0;
    }

    /// <summary>
    /// Every user id holding an active role in <paramref name="tenantId"/>, for the listing
    /// handlers, which read every user in the installation and filtered none of them.
    /// </summary>
    public static async Task<HashSet<GlobalUniqueId>> MemberIdsAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        var roles = await unitOfWork.Repository<UserRole>()
            .FindAsync(
                assignment => assignment.TenantId == tenantId && assignment.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        return roles.Select(assignment => assignment.UserId).ToHashSet();
    }
}
