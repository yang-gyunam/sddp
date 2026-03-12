using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Users;

/// <summary>
/// User domain DTO utility
/// </summary>

internal static class UserMapping
{
    internal static UserDto MapToDto(User user) => new(
        Id: user.Id.ToString(),
        Username: user.Username,
        Email: user.Email,
        DisplayName: user.DisplayName,
        AvatarUrl: user.AvatarUrl,
        IsActive: user.IsActive,
        LastLoginAt: user.LastLoginAt?.ToIso8601(),
        CreatedAt: user.CreatedAt.ToIso8601());

    internal static SystemUserDto MapToSystemUserDto(
        User user,
        IReadOnlyList<UserRole> userRoles,
        Dictionary<GlobalUniqueId, Role> roleMap,
        IReadOnlyList<UserProjectDto> projects)
    {
        // user role 1 (UpdateSystemUser handler)
        // role: Admin, enum ordinal
        var globalRole = userRoles
            .Where(ur => roleMap.ContainsKey(ur.RoleId))
            .Select(ur => roleMap[ur.RoleId].Type)
            .OrderByDescending(t => t == RoleType.Admin ? int.MaxValue : (int)t)
            .Cast<RoleType?>()
            .FirstOrDefault()
            ?.ToString() ?? "Developer";

        var isBuiltIn = (Guid)user.Id == WellKnownUsers.AdminUserId
                     || (Guid)user.Id == WellKnownUsers.AiAgentUserId;

        return new SystemUserDto(
            Id: user.Id.ToString(),
            Name: user.DisplayName,
            Email: user.Email,
            Username: user.Username,
            GlobalRole: globalRole,
            Status: user.IsActive ? "active" : "inactive",
            IsBuiltIn: isBuiltIn,
            Projects: projects.ToList(),
            CreatedAt: user.CreatedAt.ToIso8601(),
            LastLoginAt: user.LastLoginAt?.ToIso8601());
    }

    internal static UserProjectDto? MapToUserProjectDto(
        ProjectMember member,
        Dictionary<GlobalUniqueId, UserRole> userRoleMap,
        Dictionary<GlobalUniqueId, Role> roleMap,
        Dictionary<GlobalUniqueId, string> projectNameMap)
    {
        if (!userRoleMap.TryGetValue(member.UserRoleId, out var userRole))
            return null;

        var roleName = roleMap.TryGetValue(userRole.RoleId, out var role)
            ? role.Type.ToString()
            : "Developer";

        var projectName = projectNameMap.TryGetValue(member.ProjectId, out var name)
            ? name
            : "";

        return new UserProjectDto(
            ProjectId: member.ProjectId.ToString(),
            ProjectName: projectName,
            Role: roleName);
    }

    internal static NotificationSettingsDto GetDefaultNotificationSettings() => new(
        Email: new NotificationEmailDto(
            Mentions: true,
            Conversations: true,
            SpecApprovals: true,
            TaskAssignments: true,
            DailyDigest: false),
        Browser: new NotificationBrowserDto(
            Enabled: true,
            Sound: false,
            Preview: true),
        Channels: new NotificationChannelsDto(
            Default: "mentions",
            Custom: new Dictionary<string, string>()));
}
