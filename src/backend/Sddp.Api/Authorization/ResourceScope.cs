using Sddp.Abstractions.Base;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Api.Authorization;

/// <summary>
/// What a resolved resource with no owning project means for a given endpoint.
///
/// A nullable ProjectId does not carry one meaning across the model: it is a personal task or
/// direct message for some entities and a tenant-level record for others. Treating null as
/// "no project, therefore allowed" would turn every such endpoint into a way around the check,
/// so each declaration says which it is and the default refuses.
/// </summary>
public enum NullProjectPolicy
{
    /// <summary>Refuse the request. The default, so an unconsidered case fails closed.</summary>
    Reject = 0,

    /// <summary>The caller must own the resource. Personal tasks, direct messages.</summary>
    Personal = 1,

    /// <summary>Tenant membership is enough; no project check applies. Audit logs, system config.</summary>
    Tenant = 2,
}

/// <summary>
/// How a request's authoritative project was decided, so the middleware can act on it and tests
/// can assert on it without reading status codes alone.
/// </summary>
public enum ResourceScopeOutcome
{
    /// <summary>The resource was found and owns a project. <see cref="ResourceScopeResolution.ProjectId"/> is set.</summary>
    Resolved,

    /// <summary>The resource does not exist, or exists under a tenant the caller is not in.</summary>
    NotFound,

    /// <summary>
    /// The resource exists with no owning project and the endpoint allows personal access.
    ///
    /// This asserts no project scope, so the request is evaluated at tenant level and whatever
    /// owner check the handler already performs still applies — the same path a personal task or
    /// direct message takes today. It is declared rather than inferred so the case is visible;
    /// moving the owner check itself into this layer needs an owner concept the entities do not
    /// share yet (a task has a creator and an assignee, a direct message has members).
    /// </summary>
    Personal,

    /// <summary>The resource exists with no owning project and the endpoint allows tenant access.</summary>
    Tenant,
}

/// <summary>Result of resolving one route id to the project that owns it.</summary>
public readonly record struct ResourceScopeResolution(
    ResourceScopeOutcome Outcome,
    GlobalUniqueId? ProjectId)
{
    public static ResourceScopeResolution NotFound() => new(ResourceScopeOutcome.NotFound, null);

    public static ResourceScopeResolution Resolved(GlobalUniqueId projectId) =>
        new(ResourceScopeOutcome.Resolved, projectId);
}

/// <summary>
/// The non-generic face of <see cref="ResourceScopeAttribute{TResource}"/>. The middleware works
/// against this so it never needs to know the entity type; the attribute closes over it at compile
/// time, which keeps the repository call typed and out of reflection.
/// </summary>
public interface IResourceScopeMetadata
{
    /// <summary>Route parameter holding the resource id.</summary>
    string RouteKey { get; }

    /// <summary>What a null owning project means here.</summary>
    NullProjectPolicy OnNullProject { get; }

    /// <summary>The entity this endpoint operates on. Used by the metadata test, not at runtime.</summary>
    Type ResourceType { get; }

    /// <summary>Link followed to reach a project-owning parent, or null when the entity owns one.</summary>
    string? Through { get; }

    /// <summary>Entity <see cref="Through"/> points at, or null.</summary>
    Type? ParentType { get; }

    /// <summary>
    /// Route parameter naming the parent, when the route states it. Declaring it makes a mismatched
    /// pair — child B addressed under parent A — resolve to nothing rather than quietly operating on
    /// B under its real parent.
    /// </summary>
    string? ParentRouteKey { get; }

    /// <summary>
    /// Reads the owning project for <paramref name="id"/>. When <see cref="ParentRouteKey"/> is
    /// declared, <paramref name="expectedParentId"/> carries what the route claimed the parent is.
    /// </summary>
    Task<ResourceScopeResolution> ResolveAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId id,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? expectedParentId,
        CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResourceScopeAttribute<TResource> : Attribute, IResourceScopeMetadata
    where TResource : EntityBase
{
    public ResourceScopeAttribute(string routeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeKey);
        RouteKey = routeKey;
    }

    /// <inheritdoc />
    public string RouteKey { get; }

    /// <inheritdoc />
    public NullProjectPolicy OnNullProject { get; init; } = NullProjectPolicy.Reject;

    /// <inheritdoc />
    /// <remarks>
    /// For entities that carry no project of their own — <c>Topic</c> reaches one only through
    /// <c>ForumId</c>. Set together with <see cref="ParentType"/>.
    /// </remarks>
    public string? Through { get; init; }

    /// <inheritdoc />
    /// <remarks>Must itself carry a project.</remarks>
    public Type? ParentType { get; init; }

    /// <inheritdoc />
    public string? ParentRouteKey { get; init; }

    /// <inheritdoc />
    public Type ResourceType => typeof(TResource);

    /// <inheritdoc />
    public async Task<ResourceScopeResolution> ResolveAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId id,
        GlobalUniqueId? tenantId,
        GlobalUniqueId? expectedParentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        var entity = await (unitOfWork.Repository<TResource>()
            .GetByIdAsync(id, cancellationToken)).ConfigureAwait(false);

        if (entity is null)
        {
            return ResourceScopeResolution.NotFound();
        }

        // Tenant first: a resource in another tenant must be indistinguishable from one that does
        // not exist, exactly as a resource in another project is.
        var entityTenantId = ResourceOwnership.ReadTenantId(entity);
        if (entityTenantId.HasValue && tenantId.HasValue && entityTenantId.Value != tenantId.Value)
        {
            return ResourceScopeResolution.NotFound();
        }

        // Parent traversal answers not-found on its own terms. Folding a mismatched parent into
        // "no project" would send it to OnNullProject, and a Tenant policy there would let the
        // request through — which is exactly what happened before this was split out.
        if (Through is not null && ParentType is not null)
        {
            var parentId = ResourceOwnership.ReadLinkId(entity, Through);
            if (!parentId.HasValue)
            {
                return ResourceScopeResolution.NotFound();
            }

            if (expectedParentId.HasValue && expectedParentId.Value != parentId.Value)
            {
                // The route named one parent and the child belongs to another. Resolving from the
                // real parent would authorize correctly while still acting on a resource the caller
                // did not address.
                return ResourceScopeResolution.NotFound();
            }

            var parentProjectId = await (ResourceOwnership.ReadParentProjectAsync(
                unitOfWork, ParentType, parentId.Value, cancellationToken)).ConfigureAwait(false);

            return parentProjectId.HasValue
                ? ResourceScopeResolution.Resolved(parentProjectId.Value)
                : ApplyNullProjectPolicy();
        }

        var projectId = entity is Project project
            ? project.Id
            : ResourceOwnership.ReadProjectId(entity);

        if (projectId.HasValue)
        {
            return ResourceScopeResolution.Resolved(projectId.Value);
        }

        return ApplyNullProjectPolicy();
    }

    /// <summary>What a resource with no owning project means for this endpoint.</summary>
    private ResourceScopeResolution ApplyNullProjectPolicy() => OnNullProject switch
    {
        NullProjectPolicy.Personal => new ResourceScopeResolution(ResourceScopeOutcome.Personal, null),
        NullProjectPolicy.Tenant => new ResourceScopeResolution(ResourceScopeOutcome.Tenant, null),
        _ => ResourceScopeResolution.NotFound(),
    };

}

/// <summary>
/// Marks an endpoint that has no project-owned resource in its route, with the reason recorded
/// rather than left to inference. Listing and creation endpoints, personal resources keyed by the
/// caller's own id, and tenant- or system-level resources belong here.
///
/// The reason is required and asserted non-empty: an exemption without one is how a gap gets
/// reintroduced under the appearance of a decision.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ResourceScopeExemptAttribute : Attribute
{
    public ResourceScopeExemptAttribute(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Reason = reason;
    }

    public string Reason { get; }
}
