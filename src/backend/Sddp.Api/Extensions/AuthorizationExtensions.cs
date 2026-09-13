using Microsoft.AspNetCore.Authorization;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Services;
using Sddp.Domain.Entities;

namespace Sddp.Api.Extensions;

/// <summary>
/// Authorization policy extension methods.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers role-based and permission-based authorization policies.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            AddScopedRolePolicy(options, "RequireAdmin", RoleType.Admin);
            AddScopedRolePolicy(options, "RequireProductOwner", RoleType.ProductOwner, RoleType.Admin);
            AddScopedRolePolicy(
                options, "RequireDeveloper", RoleType.Developer, RoleType.ProductOwner, RoleType.Admin);
            AddScopedRolePolicy(
                options, "RequireReviewer", RoleType.Reviewer, RoleType.ProductOwner, RoleType.Admin);

            AddScopedPermissionPolicy(options, "CanCreateSpec", PermissionCodes.SpecCreate);
            AddScopedPermissionPolicy(options, "CanApproveSpec", PermissionCodes.SpecApprove);
            AddScopedPermissionPolicy(options, "CanManageUsers", PermissionCodes.UserManage);
            AddScopedPermissionPolicy(options, "CanAssignRoles", PermissionCodes.RoleAssign);
            AddScopedPermissionPolicy(options, "CanExecuteGeneration", PermissionCodes.GenerationExecute);
            AddScopedPermissionPolicy(options, "CanReadAuditLogs", PermissionCodes.AuditRead);
            AddScopedPermissionPolicy(options, "CanCreateConversation", PermissionCodes.ConversationCreate);
            AddScopedPermissionPolicy(options, "CanCloseConversation", PermissionCodes.ConversationClose);
            AddScopedPermissionPolicy(options, "CanTriggerAnalysis", PermissionCodes.GenerationExecute);

            options.AddPolicy("CanManageProjectMembers", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ProjectOwnerRequirement());
            });

            foreach (var permissionCode in PermissionCodes.All)
            {
                options.AddPolicy(PermissionPolicyNames.For(permissionCode), policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ScopedPermissionRequirement(permissionCode));
                });
            }
        });

        services.AddScoped<IScopedAuthorizationService, ScopedAuthorizationService>();
        services.AddScoped<IAuthorizationHandler, ScopedPermissionHandler>();
        services.AddScoped<IAuthorizationHandler, ScopedRoleHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectOwnerHandler>();
        return services;
    }

    private static void AddScopedRolePolicy(
        AuthorizationOptions options,
        string policyName,
        params RoleType[] roleTypes)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ScopedRoleRequirement(roleTypes));
        });
    }

    private static void AddScopedPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string permissionCode)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ScopedPermissionRequirement(permissionCode));
        });
    }
}

/// <summary>
/// Requirement that allows only the project owner or an admin.
/// </summary>
public class ProjectOwnerRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler for the project-owner-or-admin requirement.
/// </summary>
public class ProjectOwnerHandler : AuthorizationHandler<ProjectOwnerRequirement>
{
    private static readonly RoleType[] AdminRoles = [RoleType.Admin];

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScopedAuthorizationService _authorizationService;
    private readonly ITenantContext _tenantContext;

    public ProjectOwnerHandler(
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        IScopedAuthorizationService authorizationService,
        ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
        _tenantContext = tenantContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectOwnerRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var routeProjectId = httpContext?.GetRouteValue("id")?.ToString();
        if (string.IsNullOrEmpty(routeProjectId) || !GlobalUniqueId.TryParse(routeProjectId, out var projectId))
        {
            context.Fail(new AuthorizationFailureReason(this, "Project ID not found in route"));
            return;
        }

        if (!_tenantContext.UserId.HasValue || !_tenantContext.TenantId.HasValue)
        {
            context.Fail(new AuthorizationFailureReason(this, "User identity not found"));
            return;
        }

        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;
        var isAdmin = await _authorizationService.HasAnyRoleAsync(
            _tenantContext.UserId.Value,
            _tenantContext.TenantId.Value,
            projectId,
            AdminRoles,
            cancellationToken);
        if (isAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var projectRepo = _unitOfWork.Repository<Project>();
        var projects = await projectRepo.FindAsync(
            p => p.Id == projectId && p.TenantId == _tenantContext.TenantId.Value,
            cancellationToken);
        var project = projects.FirstOrDefault();

        if (project is not null
            && project.OwnerId.HasValue
            && project.OwnerId.Value == _tenantContext.UserId.Value)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "Only the project owner or admin can manage project members"));
    }
}
