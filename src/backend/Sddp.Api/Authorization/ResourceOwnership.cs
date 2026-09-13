using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Sddp.Abstractions.Base;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Api.Authorization;

/// <summary>
/// Reads the tenant and project an entity belongs to.
///
/// The domain has no shared interface for this: nineteen entities declare
/// <c>GlobalUniqueId ProjectId</c> and six declare <c>GlobalUniqueId? ProjectId</c>, and a
/// non-nullable property does not satisfy a nullable interface member in C#, so adopting one
/// would mean explicit implementations across twenty-five entities for no behavioural gain.
/// Accessors are compiled once per type instead and cached.
///
/// Absence of the property is a programming error rather than a permission to skip the check —
/// <see cref="Describe"/> lets ResourceScopeMetadataTests reject such a declaration at test time,
/// long before a request could reach it.
/// </summary>
internal static class ResourceOwnership
{
    private const string ProjectIdProperty = "ProjectId";
    private const string TenantIdProperty = "TenantId";

    private static readonly ConcurrentDictionary<Type, Func<object, GlobalUniqueId?>?> ProjectReaders = new();
    private static readonly ConcurrentDictionary<Type, Func<object, GlobalUniqueId?>?> TenantReaders = new();
    private static readonly ConcurrentDictionary<(Type, string), Func<object, GlobalUniqueId?>?> LinkReaders = new();
    private static readonly ConcurrentDictionary<
        Type,
        Func<IUnitOfWork, GlobalUniqueId, CancellationToken, Task<object?>>> ParentLoaders = new();

    /// <summary>Owning project, or null when the entity declares a nullable one and it is unset.</summary>
    internal static GlobalUniqueId? ReadProjectId(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var reader = ProjectReaders.GetOrAdd(entity.GetType(), type => Compile(type, ProjectIdProperty));
        if (reader is null)
        {
            throw new InvalidOperationException(
                $"{entity.GetType().Name} has no readable {ProjectIdProperty}, so it cannot be used " +
                $"with [ResourceScope<{entity.GetType().Name}>]. Declare the endpoint with " +
                "[ResourceScopeExempt(\"reason\")] instead, or scope it through a parent that owns a project.");
        }

        return reader(entity);
    }

    /// <summary>Owning tenant, or null when the entity does not carry one.</summary>
    internal static GlobalUniqueId? ReadTenantId(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var reader = TenantReaders.GetOrAdd(entity.GetType(), type => Compile(type, TenantIdProperty));
        return reader?.Invoke(entity);
    }

    /// <summary>
    /// Reads a foreign-key property named <paramref name="propertyName"/> — the link an entity
    /// follows to reach a parent that owns a project.
    /// </summary>
    internal static GlobalUniqueId? ReadLinkId(object entity, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = (entity.GetType(), propertyName);
        var reader = LinkReaders.GetOrAdd(key, k => Compile(k.Item1, k.Item2));

        if (reader is null)
        {
            throw new InvalidOperationException(
                $"{entity.GetType().Name} has no readable {propertyName}, so it cannot be scoped "
                + "through that link. Check the Through value on the declaration.");
        }

        return reader(entity);
    }

    /// <summary>
    /// Loads <paramref name="parentType"/> by id and returns the project it belongs to.
    ///
    /// The parent type is only known at runtime, so this reaches the repository reflectively —
    /// once per parent type, cached. A missing parent yields null, which the caller turns into the
    /// same not-found answer as a missing resource.
    /// </summary>
    internal static async Task<GlobalUniqueId?> ReadParentProjectAsync(
        IUnitOfWork unitOfWork,
        Type parentType,
        GlobalUniqueId parentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(parentType);

        var loader = ParentLoaders.GetOrAdd(parentType, BuildParentLoader);
        var parent = await (loader(unitOfWork, parentId, cancellationToken)).ConfigureAwait(false);

        return parent is null ? null : ReadProjectId(parent);
    }

    private static Func<IUnitOfWork, GlobalUniqueId, CancellationToken, Task<object?>> BuildParentLoader(
        Type parentType)
    {
        var repositoryMethod = typeof(IUnitOfWork)
            .GetMethod(nameof(IUnitOfWork.Repository))!
            .MakeGenericMethod(parentType);

        var getByIdMethod = typeof(IRepository<>)
            .MakeGenericType(parentType)
            .GetMethod(nameof(IRepository<EntityBase>.GetByIdAsync))!;

        return async (unitOfWork, id, cancellationToken) =>
        {
            var repository = repositoryMethod.Invoke(unitOfWork, null)!;
            var task = (Task)getByIdMethod.Invoke(repository, [id, cancellationToken])!;
            await task.ConfigureAwait(false);

            return task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        };
    }

    /// <summary>
    /// Whether <paramref name="resourceType"/> can be scoped, and how. Used by the metadata test so
    /// a declaration naming an entity without a project fails in CI rather than at runtime.
    /// </summary>
    internal static ResourceOwnershipShape Describe(Type resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        var property = FindProperty(resourceType, ProjectIdProperty);
        if (property is null)
        {
            return new ResourceOwnershipShape(false, false);
        }

        var nullable = Nullable.GetUnderlyingType(property.PropertyType) is not null;
        return new ResourceOwnershipShape(true, nullable);
    }

    /// <summary>Whether <paramref name="type"/> exposes a readable id property named
    /// <paramref name="propertyName"/>. Lets the metadata test reject a Through that goes nowhere.</summary>
    internal static bool HasLink(Type type, string propertyName) =>
        FindProperty(type, propertyName) is not null;

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        var property = type.GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

        if (property is null || !property.CanRead)
        {
            return null;
        }

        // Both shapes are accepted; anything else is not an id we know how to read.
        return property.PropertyType == typeof(GlobalUniqueId)
            || property.PropertyType == typeof(GlobalUniqueId?)
            ? property
            : null;
    }

    /// <summary>
    /// Builds <c>entity =&gt; (GlobalUniqueId?)((TEntity)entity).Property</c> once per type. Reflection
    /// on every request would put a property lookup in the authorization path for no reason.
    /// </summary>
    private static Func<object, GlobalUniqueId?>? Compile(Type type, string name)
    {
        var property = FindProperty(type, name);
        if (property is null)
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(object), "entity");
        Expression access = Expression.Property(Expression.Convert(parameter, type), property);

        if (property.PropertyType == typeof(GlobalUniqueId))
        {
            access = Expression.Convert(access, typeof(GlobalUniqueId?));
        }

        return Expression.Lambda<Func<object, GlobalUniqueId?>>(access, parameter).Compile();
    }
}

/// <summary>Whether an entity carries a project, and whether that project may be absent.</summary>
internal readonly record struct ResourceOwnershipShape(bool HasProjectId, bool ProjectIdIsNullable);
