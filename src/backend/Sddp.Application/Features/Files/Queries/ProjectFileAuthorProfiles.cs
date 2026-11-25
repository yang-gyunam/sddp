using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Users;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Files.Queries;

internal static class ProjectFileAuthorProfiles
{
    internal static async Task<Dictionary<string, UserRefDto>> ResolveAsync(IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId, GlobalUniqueId projectId, IEnumerable<string?> emails, CancellationToken ct)
    {
        var matches = emails.Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (matches.Count == 0) return [];

        var members = await unitOfWork.Repository<ProjectMember>().FindAsync(
            member => member.ProjectId == projectId && member.IsActive, ct).ConfigureAwait(false);
        if (members.Count == 0) return [];

        var tenantMembers = await TenantUserScope.MemberIdsAsync(unitOfWork, tenantId, ct).ConfigureAwait(false);
        var ids = members.Select(member => member.UserId).Where(tenantMembers.Contains).Distinct().ToList();
        if (ids.Count == 0) return [];

        var users = await unitOfWork.Repository<User>().FindAsync(user => user.IsActive && ids.Contains(user.Id), ct).ConfigureAwait(false);
        return users.Where(user => matches.Contains(user.Email))
            .GroupBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group =>
            {
                var user = group.Single();
                return new UserRefDto(user.Id.ToString(), user.DisplayName, user.AvatarUrl);
            }, StringComparer.OrdinalIgnoreCase);
    }

    internal static UserRefDto? Find(IReadOnlyDictionary<string, UserRefDto> profiles, string? email) =>
        email is null ? null : profiles.GetValueOrDefault(email.Trim());
}
