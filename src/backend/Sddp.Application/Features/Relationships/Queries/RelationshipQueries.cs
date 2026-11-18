using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships.Queries;

public interface IRelationshipReadQuery;

/// <summary>
/// relationship get (ID)
/// </summary>
public sealed record GetRelationshipByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RelationshipId) : IQuery<RelationshipDto?>, IRelationshipReadQuery;

public sealed class GetRelationshipByIdQueryHandler(IUnitOfWork unitOfWork, IRelationshipEndpointAccess access)
    : IRequestHandler<GetRelationshipByIdQuery, RelationshipDto?>
{
    public async Task<RelationshipDto?> Handle(GetRelationshipByIdQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Relationship>();

        var relationship = await (repo.GetByIdAsync(request.RelationshipId, cancellationToken)).ConfigureAwait(false);
        if (relationship is null
            || relationship.TenantId != request.TenantId
            || relationship.ProjectId != request.ProjectId
            || !relationship.IsActive)
        {
            return null;
        }

        var readable = await access.ReadEndpointsAsync(request.TenantId, request.ProjectId, [relationship], cancellationToken);
        return readable.ContainsKey(relationship.From()) && readable.ContainsKey(relationship.To())
            ? RelationshipMapping.MapToDto(relationship) : null;
    }
}

/// <summary>
/// entity relationship get
/// </summary>
public sealed record GetRelationshipsByEntityQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    EntityType EntityType,
    GlobalUniqueId EntityId,
    RelationType? TypeFilter,
    bool IncludeInvalidated) : IQuery<RelationshipListDto>, IRelationshipReadQuery;

public sealed class GetRelationshipsByEntityQueryHandler(IUnitOfWork unitOfWork, IRelationshipEndpointAccess access)
    : IRequestHandler<GetRelationshipsByEntityQuery, RelationshipListDto>
{
    public async Task<RelationshipListDto> Handle(GetRelationshipsByEntityQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Relationship>();

        RelationshipEndpoint root = new(request.EntityType, request.EntityId);
        await access.RequireRootAsync(request.TenantId, request.ProjectId, root, cancellationToken);
        var relationships = await repo.FindAsync(r => r.TenantId == request.TenantId && r.ProjectId == request.ProjectId
            && (r.FromEntityType == request.EntityType && r.FromEntityId == request.EntityId
                || r.ToEntityType == request.EntityType && r.ToEntityId == request.EntityId)
            && (request.IncludeInvalidated || r.ValidTo == null)
            && (request.TypeFilter == null || r.Type == request.TypeFilter), cancellationToken);

        var endpoints = await access.ReadEndpointsAsync(request.TenantId, request.ProjectId, relationships, cancellationToken);
        var incoming = new List<RelationshipDto>();
        var outgoing = new List<RelationshipDto>();
        foreach (var rel in relationships)
        {
            if (!endpoints.TryGetValue(rel.From(), out var from) || !endpoints.TryGetValue(rel.To(), out var to)) continue;
            var dto = RelationshipMapping.MapToDto(rel, from.Label, from.Code, to.Label, to.Code);
            if (rel.From() == root) outgoing.Add(dto);
            if (rel.To() == root) incoming.Add(dto);
        }
        return new RelationshipListDto(incoming, outgoing);
    }
}

/// <summary>
/// relationship get (BFS)
/// </summary>
public sealed record GetRelationshipGraphQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    EntityType RootEntityType,
    GlobalUniqueId RootEntityId,
    int MaxDepth,
    RelationType[]? TypeFilter) : IQuery<RelationshipGraphDto>, IRelationshipReadQuery;

public sealed class GetRelationshipGraphQueryHandler(IUnitOfWork unitOfWork, IRelationshipEndpointAccess access)
    : IRequestHandler<GetRelationshipGraphQuery, RelationshipGraphDto>
{
    public async Task<RelationshipGraphDto> Handle(GetRelationshipGraphQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Relationship>();
        RelationshipEndpoint root = new(request.RootEntityType, request.RootEntityId);
        var display = await access.RequireRootAsync(request.TenantId, request.ProjectId, root, cancellationToken);
        var nodes = new Dictionary<RelationshipEndpoint, GraphNodeDto> { [root] = Node(root, display, 0) };
        var edges = new Dictionary<GlobalUniqueId, GraphEdgeDto>();
        var frontier = new HashSet<RelationshipEndpoint> { root };
        for (var depth = 1; depth <= request.MaxDepth && frontier.Count > 0; depth++)
        {
            var ids = frontier.Select(e => e.Id).Distinct().ToArray();
            var adjacent = await repo.FindAsync(r => r.TenantId == request.TenantId && r.ProjectId == request.ProjectId
                && (ids.Contains(r.FromEntityId) || ids.Contains(r.ToEntityId))
                && r.ValidTo == null && (request.TypeFilter == null || request.TypeFilter.Contains(r.Type)), cancellationToken);
            // IDs are scoped by entity type. Only actual frontier endpoints can start a traversal step.
            var candidates = adjacent.Where(r => !edges.ContainsKey(r.Id) && (frontier.Contains(r.From()) || frontier.Contains(r.To()))).ToArray();
            var readable = await access.ReadEndpointsAsync(request.TenantId, request.ProjectId, candidates, cancellationToken);
            var next = new HashSet<RelationshipEndpoint>();
            foreach (var rel in candidates)
            {
                var from = rel.From();
                var to = rel.To();
                if (!readable.TryGetValue(from, out var fromDisplay) || !readable.TryGetValue(to, out var toDisplay)) continue;
                edges.Add(rel.Id, new(rel.Id.ToString(), from.Id.ToString(), to.Id.ToString(), rel.Type.ToString(),
                    RelationshipMapping.GetRelationTypeLabel(rel.Type)));
                if (nodes.TryAdd(from, Node(from, fromDisplay, depth))) next.Add(from);
                if (nodes.TryAdd(to, Node(to, toDisplay, depth))) next.Add(to);
            }
            frontier = next;
        }
        return new RelationshipGraphDto(nodes.Values.ToList(), edges.Values.ToList());
    }

    private static GraphNodeDto Node(RelationshipEndpoint endpoint, EndpointDisplay display, int depth) =>
        new(endpoint.Id.ToString(), endpoint.Type.ToString(), display.Label, display.Status, depth);
}

/// <summary>
/// get
/// </summary>
public sealed record GetDecisionImpactQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId DecisionMessageId) : IQuery<DecisionImpactDto>, IRelationshipReadQuery;

public sealed class GetDecisionImpactQueryHandler(IUnitOfWork unitOfWork, IRelationshipEndpointAccess access)
    : IRequestHandler<GetDecisionImpactQuery, DecisionImpactDto>
{
    public async Task<DecisionImpactDto> Handle(GetDecisionImpactQuery request, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Relationship>();

        var relationships = await (repo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.SourceDecisionId == request.DecisionMessageId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var endpoints = await access.ReadEndpointsAsync(request.TenantId, request.ProjectId, relationships, cancellationToken);
        var items = new List<DecisionImpactItemDto>();
        foreach (var rel in relationships)
        {
            if (!endpoints.ContainsKey(rel.From()) || !endpoints.TryGetValue(rel.To(), out var target)) continue;
            items.Add(new DecisionImpactItemDto(
                EntityType: rel.ToEntityType.ToString(),
                EntityId: rel.ToEntityId.ToString(),
                Label: target.Label,
                RelationType: rel.Type.ToString()));
        }

        return new DecisionImpactDto(
            TotalCount: items.Count,
            Items: items);
    }
}

/// <summary>
/// relationship
/// </summary>
public sealed record ValidateRelationshipQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    EntityType FromEntityType,
    GlobalUniqueId FromEntityId,
    EntityType ToEntityType,
    GlobalUniqueId ToEntityId,
    RelationType Type) : IQuery<RelationshipValidationResultDto>;

public sealed class ValidateRelationshipQueryHandler : IRequestHandler<ValidateRelationshipQuery, RelationshipValidationResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateRelationshipQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<RelationshipValidationResultDto> Handle(ValidateRelationshipQuery request, CancellationToken cancellationToken)
    {
        return RelationshipValidation.ValidateAsync(
            _unitOfWork,
            request.TenantId,
            request.ProjectId,
            request.FromEntityType,
            request.FromEntityId,
            request.ToEntityType,
            request.ToEntityId,
            request.Type,
            cancellationToken);
    }
}
