using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Files.Queries;

internal static class ProjectFileOwnership
{
    internal static string RepositoryPath(string? root, string path)
        => string.Join('/', new[] { root?.Replace('\\', '/').Trim('/'), path.Replace('\\', '/').TrimStart('/') }
            .Where(p => !string.IsNullOrEmpty(p) && p != "."));

    internal static async Task<Dictionary<GlobalUniqueId, UserRefDto>> ReadOwnersAsync(IUnitOfWork unitOfWork,
        IEnumerable<GlobalUniqueId> ownerIds, CancellationToken cancellationToken)
    {
        var ids = ownerIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var users = await unitOfWork.Repository<User>().FindAsync(user => user.IsActive && ids.Contains(user.Id),
            cancellationToken).ConfigureAwait(false);
        return users.ToDictionary(user => user.Id, user => new UserRefDto(user.Id.ToString(), user.DisplayName, user.AvatarUrl));
    }
}
