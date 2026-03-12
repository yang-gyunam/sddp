using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships;

internal static class RelationshipValidation
{
    internal static async Task<RelationshipValidationResultDto> ValidateAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        EntityType fromEntityType,
        GlobalUniqueId fromEntityId,
        EntityType toEntityType,
        GlobalUniqueId toEntityId,
        RelationType type,
        CancellationToken cancellationToken)
    {
        if (fromEntityType == toEntityType && fromEntityId == toEntityId)
        {
            return new RelationshipValidationResultDto(
                IsValid: false,
                ErrorCode: "SELF_REFERENCE",
                ErrorMessage: "Cannot create relationship to self");
        }

        var repo = unitOfWork.Repository<Relationship>();
        var existing = await (repo.FindAsync(
            r => r.TenantId == tenantId
                && r.ProjectId == projectId
                && r.FromEntityType == fromEntityType
                && r.FromEntityId == fromEntityId
                && r.ToEntityType == toEntityType
                && r.ToEntityId == toEntityId
                && r.Type == type
                && r.IsActive
                && r.ValidTo == null,
            cancellationToken)).ConfigureAwait(false);

        if (existing.Any())
        {
            return new RelationshipValidationResultDto(
                IsValid: false,
                ErrorCode: "DUPLICATE",
                ErrorMessage: "Relationship already exists");
        }

        if (type is RelationType.DependsOn or RelationType.Extends or RelationType.Implements)
        {
            var hasCycle = await (DetectCycleAsync(
                unitOfWork,
                tenantId,
                projectId,
                fromEntityType,
                fromEntityId,
                toEntityType,
                toEntityId,
                type,
                cancellationToken)).ConfigureAwait(false);

            if (hasCycle)
            {
                return new RelationshipValidationResultDto(
                    IsValid: false,
                    ErrorCode: "CYCLE_DETECTED",
                    ErrorMessage: "Creating this relationship would cause a cycle");
            }
        }

        return new RelationshipValidationResultDto(
            IsValid: true,
            ErrorCode: null,
            ErrorMessage: null);
    }

    private static async Task<bool> DetectCycleAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        EntityType fromEntityType,
        GlobalUniqueId fromEntityId,
        EntityType toEntityType,
        GlobalUniqueId toEntityId,
        RelationType type,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Relationship>();
        var visited = new HashSet<string>();
        var stack = new Stack<(EntityType entityType, GlobalUniqueId entityId)>();

        stack.Push((toEntityType, toEntityId));

        while (stack.Count > 0)
        {
            var (currentType, currentId) = stack.Pop();
            var key = $"{currentType}:{currentId}";

            if (visited.Contains(key))
                continue;

            if (currentType == fromEntityType && currentId == fromEntityId)
                return true;

            visited.Add(key);

            var outgoingRelationships = await (repo.FindAsync(
                r => r.TenantId == tenantId
                    && r.ProjectId == projectId
                    && r.FromEntityType == currentType
                    && r.FromEntityId == currentId
                    && r.Type == type
                    && r.IsActive
                    && r.ValidTo == null,
                cancellationToken)).ConfigureAwait(false);

            foreach (var rel in outgoingRelationships)
            {
                stack.Push((rel.ToEntityType, rel.ToEntityId));
            }
        }

        return false;
    }
}
