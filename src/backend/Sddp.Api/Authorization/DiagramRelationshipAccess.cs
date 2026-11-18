using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Api.Services;
using Sddp.Domain.Entities;

namespace Sddp.Api.Authorization;

/// <summary>
/// Applies per-type read permissions, project scope and diagram origin restrictions to relationship endpoints.
/// Request-local caches must never outlive a permission or membership check.
/// </summary>
public sealed class DiagramRelationshipAccess(
    IUnitOfWork unitOfWork, IScopedAuthorizationService authorization, ITenantContext context, IDiagramOriginScopeReader reader,
    IRelationshipEndpointReader endpoints, IStoredDiagramReader models) : IDiagramOriginAccess, IRelationshipEndpointAccess
{
    private readonly Dictionary<(EntityType, GlobalUniqueId), bool> _scopes = new();
    private readonly Dictionary<string, bool> _permissions = new(StringComparer.Ordinal);
    private GlobalUniqueId Tenant => context.TenantId ?? throw new UnauthorizedException("Tenant required.");
    private GlobalUniqueId Project => context.ProjectId ?? throw new UnauthorizedException("Project required.");

    internal void ClearCachedAccess()
    {
        _scopes.Clear();
        _permissions.Clear();
    }

    public async Task<EndpointDisplay> RequireRootAsync(GlobalUniqueId tenant, GlobalUniqueId project,
        RelationshipEndpoint root, CancellationToken ct)
    {
        if (!await CanRead(root.Type, ct)) throw new ForbiddenAccessException("The relationship endpoint must be readable.");
        var readable = await ReadAsync(tenant, project, [root], ct);
        return readable.TryGetValue(root, out var display) ? display : throw new NotFoundException(root.Type.ToString(), root.Id.ToString());
    }

    public async Task<IReadOnlyDictionary<RelationshipEndpoint, EndpointDisplay>> ReadAsync(GlobalUniqueId tenant,
        GlobalUniqueId project, IEnumerable<RelationshipEndpoint> candidates, CancellationToken ct)
    {
        if (context.UserId is null) throw new UnauthorizedException("User required.");
        if (tenant != Tenant || project != Project) throw new ForbiddenAccessException("Relationship scope mismatch.");
        var readable = new Dictionary<RelationshipEndpoint, EndpointDisplay>();
        var diagrams = new List<(ScopedRelationshipEndpoint Row, DiagramOrigin[] Origins)>();
        foreach (var group in candidates.Distinct().GroupBy(e => e.Type))
        {
            if (!await CanRead(group.Key, ct)) continue;
            var rows = await endpoints.ReadScopedAsync(group.Key, group.Select(e => e.Id).ToArray(), tenant, project, context.UserId, ct);
            foreach (var row in rows)
            {
                if (group.Key != EntityType.Diagram)
                {
                    readable.Add(new(group.Key, row.Id), new(row.Label, row.Code, row.Status));
                    continue;
                }
                var inspected = models.Inspect(row.Id, row.ModelJson!, row.DiagramKind!);
                if (inspected is not null) diagrams.Add((row, inspected.Origins));
            }
        }
        var allowed = await ReadableOrigins(diagrams.SelectMany(d => d.Origins), ct);
        foreach (var (row, origins) in diagrams)
            if (origins.All(allowed.Contains)) readable.Add(new(EntityType.Diagram, row.Id), new(row.Label, null, null));
        return readable;
    }

    public async Task RequireLink(EntityType from, GlobalUniqueId fromId, EntityType to,
        GlobalUniqueId toId, bool write, CancellationToken ct)
    {
        if (!await CanRead(from, ct) || !await CanRead(to, ct))
            throw new ForbiddenAccessException("Both relationship endpoints must be readable.");
        RelationshipEndpoint source = new(from, fromId);
        RelationshipEndpoint target = new(to, toId);
        var readable = await ReadAsync(Tenant, Project, [source, target], ct);
        if (!readable.ContainsKey(source) || !readable.ContainsKey(target))
            throw new NotFoundException("Relationship endpoint", "requested project");
        if (write && (from == EntityType.Diagram || to == EntityType.Diagram)) await RequirePermission(PermissionCodes.DiagramUpdate, ct);
    }

    public async Task RequireRelationshipUpdate(GlobalUniqueId id, CancellationToken ct)
    {
        var relationship = await unitOfWork.Repository<Relationship>().GetByIdAsync(id, ct);
        if (relationship is null || relationship.TenantId != Tenant || relationship.ProjectId != Project)
            throw new NotFoundException("Relationship", id.ToString());
        RelationshipEndpoint from = new(relationship.FromEntityType, relationship.FromEntityId);
        RelationshipEndpoint to = new(relationship.ToEntityType, relationship.ToEntityId);
        var readable = await ReadAsync(Tenant, Project, [from, to], ct);
        if (!readable.ContainsKey(from) || !readable.ContainsKey(to)) throw new NotFoundException("Relationship", id.ToString());
        if (from.Type == EntityType.Diagram || to.Type == EntityType.Diagram)
            await RequirePermission(PermissionCodes.DiagramUpdate, ct);
    }

    private async Task<GlobalUniqueId> RequireOrigin(DiagramOrigin origin, CancellationToken ct)
    {
        var id = GlobalUniqueId.FromGuid(Guid.Parse(origin.Id));
        if (!await InScope(origin.Type, id, ct)) throw new NotFoundException("Projection origin", origin.Id);
        if (!await CanRead(origin.Type, ct)) throw new ForbiddenAccessException("All projection origins must be readable.");
        return id;
    }

    public async Task<string> ReadLabel(DiagramOrigin origin, CancellationToken ct)
    {
        var id = await RequireOrigin(origin, ct);
        return origin.Type switch
        {
            EntityType.Spec => (await unitOfWork.Repository<Spec>().GetByIdAsync(id, ct))!.Title,
            EntityType.Requirement => (await unitOfWork.Repository<Requirement>().GetByIdAsync(id, ct))!.Title,
            EntityType.Topic => (await unitOfWork.Repository<Topic>().GetByIdAsync(id, ct))!.Title,
            EntityType.GlossaryTerm => (await unitOfWork.Repository<GlossaryTerm>().GetByIdAsync(id, ct))!.Term,
            EntityType.Conversation => (await unitOfWork.Repository<Conversation>().GetByIdAsync(id, ct))!.Name,
            EntityType.Artifact => (await unitOfWork.Repository<ArtifactTracking>().GetByIdAsync(id, ct))!.ArtifactPath,
            EntityType.Task => (await unitOfWork.Repository<TaskItem>().GetByIdAsync(id, ct))!.Title,
            _ => throw new ValidationException("origins", "Unsupported projection origin.")
        };
    }

    public async Task RequireModel(DiagramRevision model, string? kind, CancellationToken ct)
    {
        DiagramOrigin[]? origins;
        try
        {
            if (model is null) throw new InvalidDataException("A diagram model is required.");
            origins = DiagramProjectionSource.ReadForKind(model, kind);
        }
        catch (InvalidDataException ex) { throw new ValidationException("model", ex.Message); }
        if (origins is not null) await RequireOrigins(origins, ct);
    }

    public async Task RequireOrigins(DiagramOrigin[] origins, CancellationToken ct)
    {
        foreach (var origin in origins) await RequireOrigin(origin, ct);
    }

    public async Task<bool> CanReadModel(DiagramRevision model, string kind, CancellationToken ct)
    {
        try { await RequireModel(model, kind, ct); return true; }
        catch (NotFoundException) { return false; }
        catch (ForbiddenAccessException) { return false; }
        catch (ValidationException) { return false; }
    }

    private async Task<bool> HasPermission(string code, CancellationToken ct)
    {
        if (_permissions.TryGetValue(code, out var allowed)) return allowed;
        allowed = context.UserId is { } user && await authorization.HasPermissionAsync(user, Tenant, Project, code, ct);
        _permissions[code] = allowed;
        return allowed;
    }

    private async Task RequirePermission(string code, CancellationToken ct)
    {
        if (!await HasPermission(code, ct)) throw new ForbiddenAccessException($"Permission '{code}' is required.");
    }

    private Task<bool> CanRead(EntityType type, CancellationToken ct) => type switch
    {
        EntityType.Diagram => HasPermission(PermissionCodes.DiagramRead, ct),
        EntityType.Spec => HasPermission(PermissionCodes.SpecRead, ct),
        EntityType.Requirement => HasPermission(PermissionCodes.RequirementRead, ct),
        EntityType.Conversation or EntityType.Topic => HasPermission(PermissionCodes.ConversationRead, ct),
        EntityType.GlossaryTerm => HasPermission(PermissionCodes.GlossaryRead, ct),
        EntityType.Task => HasPermission(PermissionCodes.TaskRead, ct),
        // Artifact reads use project membership, already enforced by middleware.
        EntityType.Artifact => Task.FromResult(true),
        _ => Task.FromResult(false)
    };

    public async Task<HashSet<DiagramOrigin>> ReadableOrigins(IEnumerable<DiagramOrigin> origins, CancellationToken ct)
    {
        var readable = new HashSet<DiagramOrigin>();
        foreach (var group in origins.Distinct().GroupBy(o => o.Type))
        {
            if (!await CanRead(group.Key, ct)) continue;
            var ids = group.Select(o => GlobalUniqueId.Parse(o.Id)).ToArray();
            var scoped = await reader.ReadScopedOriginIdsAsync(group.Key, ids, Tenant, Project, context.UserId, ct);
            foreach (var id in scoped) readable.Add(new(group.Key, id.ToString()));
        }
        return readable;
    }

    private async Task<bool> InScope(EntityType type, GlobalUniqueId id, CancellationToken ct)
    {
        if (_scopes.TryGetValue((type, id), out var valid)) return valid;
        valid = (await reader.ReadScopedOriginIdsAsync(type, [id], Tenant, Project, context.UserId, ct)).Contains(id);
        _scopes[(type, id)] = valid;
        return valid;
    }
}
