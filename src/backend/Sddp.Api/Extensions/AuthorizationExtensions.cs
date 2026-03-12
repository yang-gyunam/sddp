using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
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
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User, RoleType.Admin)));

            options.AddPolicy("RequireProductOwner", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User, RoleType.ProductOwner, RoleType.Admin)));

            options.AddPolicy("RequireDeveloper", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User, RoleType.Developer, RoleType.ProductOwner, RoleType.Admin)));

            options.AddPolicy("RequireReviewer", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User, RoleType.Reviewer, RoleType.ProductOwner, RoleType.Admin)));

            options.AddPolicy("CanCreateSpec", policy =>
                policy.RequireClaim("permission", "spec:create"));

            options.AddPolicy("CanApproveSpec", policy =>
                policy.RequireClaim("permission", "spec:approve"));

            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireClaim("permission", "user:manage"));

            options.AddPolicy("CanAssignRoles", policy =>
                policy.RequireClaim("permission", "role:assign"));

            options.AddPolicy("CanExecuteGeneration", policy =>
                policy.RequireClaim("permission", "generation:execute"));

            options.AddPolicy("CanReadAuditLogs", policy =>
                policy.RequireClaim("permission", "audit:read"));

            options.AddPolicy("CanCreateConversation", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User,
                        RoleType.ProductOwner,
                        RoleType.DomainExpert,
                        RoleType.Developer,
                        RoleType.Admin)));

            options.AddPolicy("CanCloseConversation", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User, RoleType.ProductOwner, RoleType.Admin)));

            options.AddPolicy("CanTriggerAnalysis", policy =>
                policy.RequireAssertion(context =>
                    HasAnyRole(context.User,
                        RoleType.ProductOwner,
                        RoleType.Developer,
                        RoleType.Reviewer,
                        RoleType.Admin)));

            options.AddPolicy("CanManageProjectMembers", policy =>
                policy.Requirements.Add(new ProjectOwnerRequirement()));
        });

        services.AddScoped<IAuthorizationHandler, ProjectOwnerHandler>();
        return services;
    }

    /// <summary>
    /// Checks whether the user has any of the specified roles.
    /// </summary>
    private static bool HasAnyRole(ClaimsPrincipal user, params RoleType[] roles)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        return roles.Any(role => userRoles.Contains(role.ToString()));
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectOwnerHandler(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectOwnerRequirement requirement)
    {
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        if (userRoles.Contains(RoleType.Admin.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var routeProjectId = httpContext?.GetRouteValue("id")?.ToString();
        if (string.IsNullOrEmpty(routeProjectId) || !GlobalUniqueId.TryParse(routeProjectId, out var projectId))
        {
            context.Fail(new AuthorizationFailureReason(this, "Project ID not found in route"));
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !GlobalUniqueId.TryParse(userIdClaim, out var userId))
        {
            context.Fail(new AuthorizationFailureReason(this, "User identity not found"));
            return;
        }

        var projectRepo = _unitOfWork.Repository<Project>();
        var projects = await projectRepo.FindAsync(p => p.Id == projectId);
        var project = projects.FirstOrDefault();

        if (project is not null && project.OwnerId.HasValue && project.OwnerId.Value == userId)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, "Only the project owner or admin can manage project members"));
    }
}
