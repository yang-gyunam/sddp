using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Api.Services;
using Sddp.Domain.Entities;

namespace Sddp.Api.Middleware;

/// <summary>
/// Project membership validation middleware.
/// When X-Project-Id is present, it verifies that the current user belongs to that project.
/// Admin users can access all projects and bypass this check.
/// </summary>
public class ProjectMembershipMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProjectMembershipMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ProjectMembershipMiddleware(RequestDelegate next, ILogger<ProjectMembershipMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        IMemoryCache cache)
    {
        // Skip if not authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Skip if no project context
        if (!tenantContext.ProjectId.HasValue)
        {
            await _next(context);
            return;
        }

        // Admin bypasses project membership check
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        if (userRoles.Contains(RoleType.Admin.ToString()))
        {
            await _next(context);
            return;
        }

        var userId = tenantContext.UserId;
        if (!userId.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    success = false,
                    errorCode = "FORBIDDEN",
                    errorMessage = "User identity not found"
                }, JsonOptions));
            return;
        }

        var projectId = tenantContext.ProjectId.Value;
        var cacheKey = $"project-membership:{userId.Value}:{projectId}";

        var isMember = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var memberRepo = unitOfWork.Repository<ProjectMember>();
            var count = await memberRepo.CountAsync(
                m => m.UserId == userId.Value && m.ProjectId == projectId && m.IsActive);
            return count > 0;
        });

        if (!isMember)
        {
            _logger.LogWarning(
                "Project membership denied: UserId={UserId}, ProjectId={ProjectId}",
                userId, projectId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    success = false,
                    errorCode = "FORBIDDEN",
                    errorMessage = "Not a member of this project"
                }, JsonOptions));
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Project membership middleware extension methods.
/// </summary>
public static class ProjectMembershipMiddlewareExtensions
{
    /// <summary>
    /// Adds project membership middleware to the pipeline.
    /// Call this after UseTenantContext().
    /// </summary>
    public static IApplicationBuilder UseProjectMembership(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ProjectMembershipMiddleware>();
    }
}
