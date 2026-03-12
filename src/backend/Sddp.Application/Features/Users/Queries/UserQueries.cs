using System.Text.Json;
using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Users.Queries;

/// <summary>
/// tenant DTO (DM user)
/// </summary>
public record TenantMemberDto(string Id, string Name, string Email);

/// <summary>
/// tenant user get (permission)
/// DM
/// </summary>
public sealed record GetTenantMembersQuery(
    GlobalUniqueId TenantId,
    string? Search) : IQuery<IReadOnlyList<TenantMemberDto>>;

public sealed class GetTenantMembersQueryHandler : IRequestHandler<GetTenantMembersQuery, IReadOnlyList<TenantMemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTenantMembersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TenantMemberDto>> Handle(GetTenantMembersQuery request, CancellationToken cancellationToken)
    {
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var userRepo = _unitOfWork.Repository<User>();

        // tenant user get (UserRole)
        var tenantUserRoles = await (userRoleRepo.FindAsync(
            ur => ur.TenantId == request.TenantId && ur.IsActive,
            cancellationToken)).ConfigureAwait(false);
        var tenantUserIds = tenantUserRoles
            .Select(ur => ur.UserId)
            .Distinct()
            .ToHashSet();

        var allUsers = await (userRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var tenantUsers = allUsers
            .Where(u => u.IsActive && tenantUserIds.Contains(u.Id) && !u.IsAI);

        // search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            tenantUsers = tenantUsers.Where(u =>
                u.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return tenantUsers
            .OrderBy(u => u.DisplayName)
            .Select(u => new TenantMemberDto(
                u.Id.ToString(),
                u.DisplayName,
                u.Email))
            .ToList();
    }
}

public sealed record GetUsersQuery(
    int PageNumber,
    int PageSize,
    string? Search) : IQuery<PagedResult<UserDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var search = request.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var (pagedUsers, totalCount) = await (userRepo.FindPagedAsync(
            u => !hasSearch
                || u.Username.Contains(search!)
                || u.Email.Contains(search!)
                || u.DisplayName.Contains(search!),
            request.PageNumber, request.PageSize,
            orderBy: u => u.DisplayName,
            cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = pagedUsers.Select(UserMapping.MapToDto).ToList();
        return PagedResult<UserDto>.Create(items, totalCount, request.PageNumber, request.PageSize);
    }
}

public sealed record GetUserByIdQuery(GlobalUniqueId UserId) : IQuery<UserDto?>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        return user is null ? null : UserMapping.MapToDto(user);
    }
}

public sealed record GetSystemUsersQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    string? Role,
    string? Status) : IQuery<PagedResult<SystemUserDto>>;

public sealed class GetSystemUsersQueryHandler : IRequestHandler<GetSystemUsersQuery, PagedResult<SystemUserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<SystemUserDto>> Handle(GetSystemUsersQuery request, CancellationToken cancellationToken)
    {
        // Role/Status User+UserRole+Role → in-memory
        // (tenant ~100)
        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var projectRepo = _unitOfWork.Repository<Project>();
        var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();

        // user (all user)
        IReadOnlyList<User> users;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            users = await (userRepo.FindIncludingInactiveAsync(u =>
                u.Username.Contains(request.Search) ||
                u.Email.Contains(request.Search) ||
                u.DisplayName.Contains(request.Search), cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            users = await (userRepo.GetAllIncludingInactiveAsync(cancellationToken)).ConfigureAwait(false);
        }

        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var roleMap = roles.ToDictionary(r => r.Id, r => r);

        var normalizedRoleFilter = string.IsNullOrWhiteSpace(request.Role)
            ? null
            : request.Role.Trim();
        var normalizedStatusFilter = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim().ToLowerInvariant();

        var candidateUserIds = users.Select(u => u.Id).ToList();
        var allUserRoles = candidateUserIds.Count == 0
            ? Array.Empty<UserRole>()
            : await (userRoleRepo.FindAsync(ur => candidateUserIds.Contains(ur.UserId), cancellationToken)).ConfigureAwait(false);
        var allUserRolesByUserId = allUserRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<UserRole>)g.ToList());

        var filteredUsers = users
            .Where(u =>
            {
                if (normalizedStatusFilter is not null)
                {
                    var status = u.IsActive ? "active" : "inactive";
                    if (!string.Equals(status, normalizedStatusFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                if (normalizedRoleFilter is not null)
                {
                    allUserRolesByUserId.TryGetValue(u.Id, out var rolesForUser);
                    var globalRole = ResolveGlobalRole(rolesForUser ?? [], roleMap);
                    if (!string.Equals(globalRole, normalizedRoleFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            })
            .ToList();

        var totalCount = filteredUsers.Count;
        var pagedUsers = filteredUsers
            .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var userIds = pagedUsers.Select(u => u.Id).ToList();
        var userIdSet = userIds.ToHashSet();

        var userRoles = allUserRoles
            .Where(ur => userIdSet.Contains(ur.UserId))
            .ToList();
        var userRolesByUserId = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userRoleMap = userRoles.ToDictionary(ur => ur.Id, ur => ur);

        var projectMembers = await (projectMemberRepo.FindAsync(pm => userIds.Contains(pm.UserId), cancellationToken)).ConfigureAwait(false);
        var projectIds = projectMembers.Select(pm => pm.ProjectId).Distinct().ToList();

        var projectNameMap = projectIds.Count == 0
            ? new Dictionary<GlobalUniqueId, string>()
            : (await (projectRepo.FindAsync(p => projectIds.Contains(p.Id), cancellationToken)).ConfigureAwait(false))
                .ToDictionary(p => p.Id, p => p.Name);

        var projectsByUserId = projectMembers
            .GroupBy(pm => pm.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pm => UserMapping.MapToUserProjectDto(pm, userRoleMap, roleMap, projectNameMap))
                    .Where(dto => dto != null)
                    .Cast<UserProjectDto>()
                    .ToList());

        var resultUsers = pagedUsers
            .Select(u =>
            {
                userRolesByUserId.TryGetValue(u.Id, out var rolesForUser);
                projectsByUserId.TryGetValue(u.Id, out var projectsForUser);
                return UserMapping.MapToSystemUserDto(
                    u,
                    rolesForUser ?? [],
                    roleMap,
                    projectsForUser ?? []);
            })
            .ToList();

        return PagedResult<SystemUserDto>.Create(resultUsers, totalCount, request.PageNumber, request.PageSize);
    }

    private static string ResolveGlobalRole(
        IReadOnlyList<UserRole> userRoles,
        IReadOnlyDictionary<GlobalUniqueId, Role> roleMap)
    {
        return userRoles
            .Where(ur => roleMap.ContainsKey(ur.RoleId))
            .Select(ur => roleMap[ur.RoleId].Type)
            .OrderByDescending(t => t == RoleType.Admin ? int.MaxValue : (int)t)
            .Cast<RoleType?>()
            .FirstOrDefault()
            ?.ToString() ?? "Developer";
    }
}

public sealed record GetSystemUserByIdQuery(GlobalUniqueId UserId) : IQuery<SystemUserDto?>;

public sealed class GetSystemUserByIdQueryHandler : IRequestHandler<GetSystemUserByIdQuery, SystemUserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemUserByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemUserDto?> Handle(GetSystemUserByIdQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();
        var projectRepo = _unitOfWork.Repository<Project>();
        var projectMemberRepo = _unitOfWork.Repository<ProjectMember>();

        // user get ()
        var user = await (userRepo.GetByIdIncludingInactiveAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var roleMap = roles.ToDictionary(r => r.Id, r => r);

        var userRoles = await (userRoleRepo.FindAsync(ur => ur.UserId == request.UserId, cancellationToken)).ConfigureAwait(false);
        var userRoleMap = userRoles.ToDictionary(ur => ur.Id, ur => ur);

        var projectMembers = await (projectMemberRepo.FindAsync(pm => pm.UserId == request.UserId, cancellationToken)).ConfigureAwait(false);
        var projectIds = projectMembers.Select(pm => pm.ProjectId).Distinct().ToList();

        var projectNameMap = projectIds.Count == 0
            ? new Dictionary<GlobalUniqueId, string>()
            : (await (projectRepo.FindAsync(p => projectIds.Contains(p.Id), cancellationToken)).ConfigureAwait(false))
                .ToDictionary(p => p.Id, p => p.Name);

        var projects = projectMembers
            .Select(pm => UserMapping.MapToUserProjectDto(pm, userRoleMap, roleMap, projectNameMap))
            .Where(dto => dto != null)
            .Cast<UserProjectDto>()
            .ToList();

        return UserMapping.MapToSystemUserDto(user, userRoles, roleMap, projects);
    }
}

public sealed record GetCurrentUserPreferencesQuery(GlobalUniqueId UserId) : IQuery<UserPreferencesDto?>;

public sealed class GetCurrentUserPreferencesQueryHandler : IRequestHandler<GetCurrentUserPreferencesQuery, UserPreferencesDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserPreferencesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPreferencesDto?> Handle(GetCurrentUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        if (user is null) return null;

        return new UserPreferencesDto(
            user.Preferences is not null
                ? JsonSerializer.Deserialize<object>(user.Preferences)
                : null);
    }
}

public sealed record GetNotificationSettingsQuery(GlobalUniqueId UserId) : IQuery<NotificationSettingsDto?>;

public sealed class GetNotificationSettingsQueryHandler : IRequestHandler<GetNotificationSettingsQuery, NotificationSettingsDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationSettingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationSettingsDto?> Handle(GetNotificationSettingsQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        if (user is null) return null;

        if (user.Preferences is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(user.Preferences);
                if (doc.RootElement.TryGetProperty("notifications", out var notificationsElement))
                {
                    var settings = JsonSerializer.Deserialize<NotificationSettingsDto>(
                        notificationsElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settings is not null) return settings;
                }
            }
            catch
            {
                // Ignore parse errors, return defaults
            }
        }

        return UserMapping.GetDefaultNotificationSettings();
    }
}

public sealed record GetCurrentUserQuery(GlobalUniqueId UserId) : IQuery<UserDto?>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        return user is null ? null : UserMapping.MapToDto(user);
    }
}
