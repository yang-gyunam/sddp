using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships;

internal static class RelationshipMapping
{
    internal static RelationshipEndpoint From(this Relationship relationship) => new(relationship.FromEntityType, relationship.FromEntityId);
    internal static RelationshipEndpoint To(this Relationship relationship) => new(relationship.ToEntityType, relationship.ToEntityId);

    internal static Task<IReadOnlyDictionary<RelationshipEndpoint, EndpointDisplay>> ReadEndpointsAsync(
        this IRelationshipEndpointAccess access, GlobalUniqueId tenant, GlobalUniqueId project,
        IEnumerable<Relationship> relationships, CancellationToken ct) =>
        access.ReadAsync(tenant, project, relationships.SelectMany(r => new[] { r.From(), r.To() }), ct);

    internal static RelationshipDto MapToDto(Relationship relationship)
    {
        return MapToDto(relationship, null, null, null, null);
    }

    internal static RelationshipDto MapToDto(
        Relationship relationship,
        string? fromLabel, string? fromCode,
        string? toLabel, string? toCode)
    {
        return new RelationshipDto(
            Id: relationship.Id.ToString(),
            TenantId: relationship.TenantId.ToString(),
            ProjectId: relationship.ProjectId.ToString(),
            FromEntityType: relationship.FromEntityType.ToString(),
            FromEntityId: relationship.FromEntityId.ToString(),
            FromEntityLabel: fromLabel,
            FromEntityCode: fromCode,
            ToEntityType: relationship.ToEntityType.ToString(),
            ToEntityId: relationship.ToEntityId.ToString(),
            ToEntityLabel: toLabel,
            ToEntityCode: toCode,
            Type: relationship.Type.ToString(),
            Reason: relationship.Reason,
            SourceSpecId: relationship.SourceSpecId?.ToString(),
            SourceDecisionId: relationship.SourceDecisionId?.ToString(),
            CreatedBy: UserRefHelper.ToUserRef(relationship.CreatedBy.ToString(), null, null),
            ValidFrom: relationship.ValidFrom.ToDateTimeOffset(),
            ValidTo: relationship.ValidTo?.ToDateTimeOffset(),
            IsCurrent: relationship.IsCurrent,
            CreatedAt: relationship.CreatedAt.ToDateTimeOffset());
    }

    internal static string GetRelationTypeLabel(RelationType type)
    {
        return type switch
        {
            RelationType.Supersedes => "Supersedes",
            RelationType.EvolvesFrom => "Evolves From",
            RelationType.Extends => "Extends",
            RelationType.ConflictsWith => "Conflicts With",
            RelationType.DependsOn => "Depends On",
            RelationType.Implements => "Implements",
            RelationType.Replaces => "Replaces",
            RelationType.Affects => "Affects",
            _ => type.ToString()
        };
    }

}
