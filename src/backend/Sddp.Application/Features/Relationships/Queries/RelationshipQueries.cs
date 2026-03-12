using MediatR;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Features.Relationships;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships.Queries;

/// <summary>
/// relationship get (ID)
/// </summary>
public sealed record GetRelationshipByIdQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId RelationshipId) : IQuery<RelationshipDto?>;

public sealed class GetRelationshipByIdQueryHandler : IRequestHandler<GetRelationshipByIdQuery, RelationshipDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRelationshipByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RelationshipDto?> Handle(GetRelationshipByIdQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Relationship>();

        var relationship = await (repo.GetByIdAsync(request.RelationshipId, cancellationToken)).ConfigureAwait(false);
        if (relationship is null
            || relationship.TenantId != request.TenantId
            || relationship.ProjectId != request.ProjectId
            || !relationship.IsActive)
        {
            return null;
        }

        return RelationshipMapping.MapToDto(relationship);
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
    bool IncludeInvalidated) : IQuery<RelationshipListDto>;

public sealed class GetRelationshipsByEntityQueryHandler : IRequestHandler<GetRelationshipsByEntityQuery, RelationshipListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRelationshipsByEntityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RelationshipListDto> Handle(GetRelationshipsByEntityQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Relationship>();

        var outgoing = await (repo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.FromEntityType == request.EntityType
                && r.FromEntityId == request.EntityId
                && r.IsActive
                && (request.IncludeInvalidated || r.ValidTo == null)
                && (request.TypeFilter == null || r.Type == request.TypeFilter),
            cancellationToken)).ConfigureAwait(false);

        var incoming = await (repo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.ToEntityType == request.EntityType
                && r.ToEntityId == request.EntityId
                && r.IsActive
                && (request.IncludeInvalidated || r.ValidTo == null)
                && (request.TypeFilter == null || r.Type == request.TypeFilter),
            cancellationToken)).ConfigureAwait(false);

        var incomingDtos = new List<RelationshipDto>();
        foreach (var rel in incoming)
        {
            incomingDtos.Add(await (RelationshipMapping.MapToDtoWithLabelsAsync(rel, _unitOfWork, cancellationToken)).ConfigureAwait(false));
        }

        var outgoingDtos = new List<RelationshipDto>();
        foreach (var rel in outgoing)
        {
            outgoingDtos.Add(await (RelationshipMapping.MapToDtoWithLabelsAsync(rel, _unitOfWork, cancellationToken)).ConfigureAwait(false));
        }

        return new RelationshipListDto(
            Incoming: incomingDtos,
            Outgoing: outgoingDtos);
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
    RelationType[]? TypeFilter) : IQuery<RelationshipGraphDto>;

public sealed class GetRelationshipGraphQueryHandler : IRequestHandler<GetRelationshipGraphQuery, RelationshipGraphDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRelationshipGraphQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RelationshipGraphDto> Handle(GetRelationshipGraphQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Relationship>();
        var nodes = new Dictionary<string, GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        var queue = new Queue<(EntityType entityType, GlobalUniqueId entityId, int depth)>();
        var visited = new HashSet<string>();

        var rootKey = $"{request.RootEntityType}:{request.RootEntityId}";
        queue.Enqueue((request.RootEntityType, request.RootEntityId, 0));
        visited.Add(rootKey);

        var rootLabel = await (RelationshipMapping.GetEntityLabelAsync(
            _unitOfWork,
            request.RootEntityType,
            request.RootEntityId,
            cancellationToken)).ConfigureAwait(false);
        var rootStatus = await (RelationshipMapping.GetEntityStatusAsync(
            _unitOfWork,
            request.RootEntityType,
            request.RootEntityId,
            cancellationToken)).ConfigureAwait(false);

        nodes[rootKey] = new GraphNodeDto(
            Id: request.RootEntityId.ToString(),
            EntityType: request.RootEntityType.ToString(),
            Label: rootLabel,
            Status: rootStatus,
            Depth: 0);

        while (queue.Count > 0)
        {
            var (entityType, entityId, depth) = queue.Dequeue();

            if (depth >= request.MaxDepth)
                continue;

            var relationships = await (repo.FindAsync(
                r => r.TenantId == request.TenantId
                    && r.ProjectId == request.ProjectId
                    && ((r.FromEntityType == entityType && r.FromEntityId == entityId)
                        || (r.ToEntityType == entityType && r.ToEntityId == entityId))
                    && r.IsActive
                    && r.ValidTo == null
                    && (request.TypeFilter == null || request.TypeFilter.Contains(r.Type)),
                cancellationToken)).ConfigureAwait(false);

            foreach (var rel in relationships)
            {
                edges.Add(new GraphEdgeDto(
                    Id: rel.Id.ToString(),
                    SourceId: rel.FromEntityId.ToString(),
                    TargetId: rel.ToEntityId.ToString(),
                    Type: rel.Type.ToString(),
                    TypeLabel: RelationshipMapping.GetRelationTypeLabel(rel.Type)));

                EntityType connectedType;
                GlobalUniqueId connectedId;

                if (rel.FromEntityType == entityType && rel.FromEntityId == entityId)
                {
                    connectedType = rel.ToEntityType;
                    connectedId = rel.ToEntityId;
                }
                else
                {
                    connectedType = rel.FromEntityType;
                    connectedId = rel.FromEntityId;
                }

                var connectedKey = $"{connectedType}:{connectedId}";
                if (!visited.Contains(connectedKey))
                {
                    visited.Add(connectedKey);

                    var label = await (RelationshipMapping.GetEntityLabelAsync(
                        _unitOfWork,
                        connectedType,
                        connectedId,
                        cancellationToken)).ConfigureAwait(false);
                    var status = await (RelationshipMapping.GetEntityStatusAsync(
                        _unitOfWork,
                        connectedType,
                        connectedId,
                        cancellationToken)).ConfigureAwait(false);

                    nodes[connectedKey] = new GraphNodeDto(
                        Id: connectedId.ToString(),
                        EntityType: connectedType.ToString(),
                        Label: label,
                        Status: status,
                        Depth: depth + 1);

                    queue.Enqueue((connectedType, connectedId, depth + 1));
                }
            }
        }

        return new RelationshipGraphDto(
            Nodes: nodes.Values.ToList(),
            Edges: edges.Distinct().ToList());
    }
}

/// <summary>
/// get
/// </summary>
public sealed record GetDecisionImpactQuery(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    GlobalUniqueId DecisionMessageId) : IQuery<DecisionImpactDto>;

public sealed class GetDecisionImpactQueryHandler : IRequestHandler<GetDecisionImpactQuery, DecisionImpactDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDecisionImpactQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DecisionImpactDto> Handle(GetDecisionImpactQuery request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Relationship>();

        var relationships = await (repo.FindAsync(
            r => r.TenantId == request.TenantId
                && r.ProjectId == request.ProjectId
                && r.SourceDecisionId == request.DecisionMessageId
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        var items = new List<DecisionImpactItemDto>();
        foreach (var rel in relationships)
        {
            var label = await (RelationshipMapping.GetEntityLabelAsync(
                _unitOfWork, rel.ToEntityType, rel.ToEntityId, cancellationToken)).ConfigureAwait(false);

            items.Add(new DecisionImpactItemDto(
                EntityType: rel.ToEntityType.ToString(),
                EntityId: rel.ToEntityId.ToString(),
                Label: label,
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
