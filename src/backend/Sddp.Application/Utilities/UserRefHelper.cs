using System.Linq.Expressions;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Utilities;

internal static class UserRefHelper
{
    internal static async Task<UserRefDto> ToUserRefAsync(
        IRepository<User> userRepo,
        GlobalUniqueId userId,
        CancellationToken cancellationToken)
    {
        var user = await (userRepo.GetByIdAsync(userId, cancellationToken)).ConfigureAwait(false);
        return new UserRefDto(
            Id: userId.ToString(),
            Name: user?.DisplayName ?? user?.Username,
            AvatarUrl: user?.AvatarUrl);
    }

    internal static async Task<UserRefDto?> ToUserRefAsync(
        IRepository<User> userRepo,
        GlobalUniqueId? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null) return null;
        return await (ToUserRefAsync(userRepo, userId.Value, cancellationToken)).ConfigureAwait(false);
    }

    internal static async Task<Dictionary<string, UserRefDto>> ResolveMapAsync(
        IRepository<User> userRepo,
        IEnumerable<GlobalUniqueId> userIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = userIds.Where(id => id != default).Distinct().ToList();
        if (distinctIds.Count == 0) return new();

        var users = await (userRepo.FindAsync(
            u => distinctIds.Contains(u.Id), cancellationToken)).ConfigureAwait(false);

        return users.ToDictionary(
            u => u.Id.ToString(),
            u => new UserRefDto(
                Id: u.Id.ToString(),
                Name: u.DisplayName ?? u.Username,
                AvatarUrl: u.AvatarUrl));
    }

    internal static UserRefDto ToUserRef(string id, string? name, string? avatarUrl)
    {
        return new UserRefDto(Id: id, Name: name, AvatarUrl: avatarUrl);
    }
}
