using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Projects;

internal static class ProjectMapping
{
    internal static ProjectDto MapToProjectDto(Project project, IReadOnlyDictionary<GlobalUniqueId, User> ownerMap)
    {
        UserRefDto owner;
        if (project.OwnerId.HasValue && ownerMap.TryGetValue(project.OwnerId.Value, out var ownerUser))
        {
            owner = UserRefHelper.ToUserRef(ownerUser.Id.ToString(), ownerUser.DisplayName, ownerUser.AvatarUrl);
        }
        else
        {
            owner = UserRefHelper.ToUserRef(project.OwnerId?.ToString() ?? string.Empty, null, null);
        }

        return new ProjectDto(
            Id: project.Id.ToString(),
            TenantId: project.TenantId.ToString(),
            Code: project.Code,
            Name: project.Name,
            Description: project.Description,
            Owner: owner,
            Status: project.Status.ToString().ToLowerInvariant(),
            CreatedAt: project.CreatedAt.ToIso8601(),
            UpdatedAt: project.UpdatedAt.ToIso8601());
    }
}
