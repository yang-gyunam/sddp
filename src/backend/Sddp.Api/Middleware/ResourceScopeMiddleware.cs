using System.Text.Json;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Authorization;
using Sddp.Api.Services;
using Sddp.Domain.Entities;

namespace Sddp.Api.Middleware;

public class ResourceScopeMiddleware
{
    /// <summary>
    /// Marks that this middleware decided the scope for the request, so
    /// <c>BaseApiController</c> uses the resolved value instead of re-reading the header — including
    /// when the decision was "no project", which a header fallback would otherwise undo.
    /// </summary>
    public const string ResolvedItemKey = "ResourceScope.Resolved";

    /// <summary>
    /// Marks that the project was derived from a resource the caller knew only by id, rather than
    /// named directly.
    ///
    /// The distinction decides what a later denial may say. Naming a project — in the header or as
    /// a route segment that is the project — means the caller already knows it exists, so 403 gives
    /// nothing away and stays the contract. Deriving it from an opaque resource id does not, and
    /// there a 403 would confirm that id lives in a project they cannot see while an unknown id
    /// answers 404.
    /// </summary>
    public const string DerivedItemKey = "ResourceScope.Derived";

    private readonly RequestDelegate _next;
    private readonly ILogger<ResourceScopeMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ResourceScopeMiddleware(RequestDelegate next, ILogger<ResourceScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantContext);

        // An unauthenticated caller must not learn anything from this layer. The middleware runs
        // ahead of UseAuthorization, so resolving here would answer 404 for an absent id and let an
        // existing one fall through to 401 — an existence oracle for anyone, which is the opposite
        // of what D8 asks for. Authentication answers first and answers the same either way.
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await (_next(context)).ConfigureAwait(false);
            return;
        }

        var scope = context.GetEndpoint()?.Metadata.GetMetadata<IResourceScopeMetadata>();
        if (scope is null)
        {
            // No declaration: either the endpoint is exempt, is not a controller action, or the
            // declaration is missing. ResourceScopeMetadataTests is what distinguishes the third
            // case — failing the request here would take down health checks and static endpoints.
            await (_next(context)).ConfigureAwait(false);
            return;
        }

        if (!context.Request.RouteValues.TryGetValue(scope.RouteKey, out var rawId)
            || rawId is null
            || !Guid.TryParse(rawId.ToString(), out var parsed))
        {
            // A value that is not an identifier at all is a malformed request, not a hidden
            // resource. Answering 404 here would say nothing useful and would silently convert the
            // 400 several endpoints already return for an unparseable id; nothing leaks either way,
            // because there is no id to have existed.
            await (WriteInvalidIdAsync(context, scope.RouteKey)).ConfigureAwait(false);
            return;
        }

        // When the route also names the parent, hand it over so a mismatched pair resolves to
        // nothing. An unparseable parent value is treated the same way rather than ignored.
        GlobalUniqueId? expectedParentId = null;
        if (scope.ParentRouteKey is not null)
        {
            if (!context.Request.RouteValues.TryGetValue(scope.ParentRouteKey, out var rawParentId)
                || rawParentId is null
                || !Guid.TryParse(rawParentId.ToString(), out var parsedParent))
            {
                await (WriteNotFoundAsync(context)).ConfigureAwait(false);
                return;
            }

            expectedParentId = GlobalUniqueId.FromGuid(parsedParent);
        }

        var resolution = await (scope.ResolveAsync(
            unitOfWork,
            GlobalUniqueId.FromGuid(parsed),
            tenantContext.TenantId,
            expectedParentId,
            context.RequestAborted)).ConfigureAwait(false);

        switch (resolution.Outcome)
        {
            case ResourceScopeOutcome.Resolved:
                var owningProject = resolution.ProjectId!.Value;

                if (tenantContext.ProjectId.HasValue && tenantContext.ProjectId.Value != owningProject)
                {
                    _logger.LogWarning(
                        "Resource scope mismatch: resource={ResourceType} owner={OwnerProjectId} requested={RequestedProjectId}",
                        scope.ResourceType.Name, owningProject, tenantContext.ProjectId.Value);

                    await (WriteNotFoundAsync(context)).ConfigureAwait(false);
                    return;
                }

                // Authoritative from here on: membership, permission and the handler all read this.
                tenantContext.SetProjectId(owningProject);
                context.Items["ProjectId"] = owningProject;
                context.Items[ResolvedItemKey] = true;

                // A Project resource is the project the route named, so a denial on it may keep the
                // established 403. Everything else was reached through an id that reveals nothing.
                if (scope.ResourceType != typeof(Project))
                {
                    context.Items[DerivedItemKey] = true;
                }

                break;

            case ResourceScopeOutcome.Personal:
            case ResourceScopeOutcome.Tenant:
                // The resource carries no project. Leaving the scope unset keeps the request on the
                // tenant-level path it already takes today; ProjectMembershipMiddleware skips when
                // there is no project, and the handler's own owner check still applies.
                tenantContext.SetProjectId(null);
                context.Items.Remove("ProjectId");
                context.Items[ResolvedItemKey] = true;
                context.Items[DerivedItemKey] = true;
                break;

            case ResourceScopeOutcome.NotFound:
            default:
                await (WriteNotFoundAsync(context)).ConfigureAwait(false);
                return;
        }

        await (_next(context)).ConfigureAwait(false);
    }

    /// <summary>The route value is not a GUID, so there is no resource to look for.</summary>
    private static async Task WriteInvalidIdAsync(HttpContext context, string routeKey)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new
                {
                    success = false,
                    errorCode = "INVALID_ID",
                    errorMessage = $"'{routeKey}' is not a valid identifier."
                },
                JsonOptions));
    }

    /// <summary>
    /// One response for "absent", "in another project" and "in another tenant". A caller must not be
    /// able to map the estate by watching which ids answer differently.
    /// </summary>
    private static async Task WriteNotFoundAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new
                {
                    success = false,
                    errorCode = "NOT_FOUND",
                    errorMessage = "Resource not found."
                },
                JsonOptions));
    }
}

/// <summary>Registers <see cref="ResourceScopeMiddleware"/>.</summary>
public static class ResourceScopeMiddlewareExtensions
{
    /// <summary>
    /// Adds resource scope resolution to the pipeline. Call after UseTenantContext() and before
    /// UseProjectMembership(): membership has to be evaluated against the resolved project.
    /// </summary>
    public static IApplicationBuilder UseResourceScope(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ResourceScopeMiddleware>();
    }
}
