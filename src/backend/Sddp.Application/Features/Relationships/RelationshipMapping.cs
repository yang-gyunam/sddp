using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Utilities;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Relationships;

internal static class RelationshipMapping
{
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

    internal static async Task<RelationshipDto> MapToDtoWithLabelsAsync(
        Relationship relationship,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var (fromLabel, fromCode) = await (GetEntityLabelAndCodeAsync(unitOfWork, relationship.FromEntityType, relationship.FromEntityId, cancellationToken)).ConfigureAwait(false);
        var (toLabel, toCode) = await (GetEntityLabelAndCodeAsync(unitOfWork, relationship.ToEntityType, relationship.ToEntityId, cancellationToken)).ConfigureAwait(false);
        return MapToDto(relationship, fromLabel, fromCode, toLabel, toCode);
    }

    internal static async Task<string> GetEntityLabelAsync(
        IUnitOfWork unitOfWork,
        EntityType entityType,
        GlobalUniqueId entityId,
        CancellationToken cancellationToken)
    {
        var (label, _) = await (GetEntityLabelAndCodeAsync(unitOfWork, entityType, entityId, cancellationToken)).ConfigureAwait(false);
        return label;
    }

    internal static async Task<(string Label, string? Code)> GetEntityLabelAndCodeAsync(
        IUnitOfWork unitOfWork,
        EntityType entityType,
        GlobalUniqueId entityId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            EntityType.Spec => await (GetSpecLabelAndCodeAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false),
            EntityType.Requirement => await (GetRequirementLabelAndCodeAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false),
            EntityType.Topic => (await (GetConversationLabelAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false), null),
            EntityType.Conversation => (await (GetConversationLabelAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false), null),
            EntityType.GlossaryTerm => (await (GetGlossaryTermLabelAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false), null),
            EntityType.Artifact => (await (GetArtifactLabelAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false), null),
            _ => (entityId.ToString()[..8], null)
        };
    }

    internal static async Task<string?> GetEntityStatusAsync(
        IUnitOfWork unitOfWork,
        EntityType entityType,
        GlobalUniqueId entityId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            EntityType.Spec => await (GetSpecStatusAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false),
            EntityType.Requirement => await (GetRequirementStatusAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false),
            EntityType.Topic => await (GetConversationStatusAsync(unitOfWork, entityId, cancellationToken)).ConfigureAwait(false),
            _ => null
        };
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

    private static async Task<(string Label, string? Code)> GetSpecLabelAndCodeAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId specId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Spec>();
        var spec = await (repo.GetByIdAsync(specId, cancellationToken)).ConfigureAwait(false);
        return (spec?.Title ?? specId.ToString()[..8], spec?.Code);
    }

    private static async Task<string?> GetSpecStatusAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId specId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Spec>();
        var spec = await (repo.GetByIdAsync(specId, cancellationToken)).ConfigureAwait(false);
        return spec?.Status.ToString();
    }

    private static async Task<(string Label, string? Code)> GetRequirementLabelAndCodeAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId requirementId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Requirement>();
        var requirement = await (repo.GetByIdAsync(requirementId, cancellationToken)).ConfigureAwait(false);
        return (requirement?.Title ?? requirementId.ToString()[..8], requirement?.Code);
    }

    private static async Task<string?> GetRequirementStatusAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId requirementId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Requirement>();
        var requirement = await (repo.GetByIdAsync(requirementId, cancellationToken)).ConfigureAwait(false);
        return requirement?.Status.ToString();
    }

    private static async Task<string> GetConversationLabelAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId conversationId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Conversation>();
        var conversation = await (repo.GetByIdAsync(conversationId, cancellationToken)).ConfigureAwait(false);
        if (conversation is null) return conversationId.ToString()[..8];
        var prefix = conversation.ConversationType == ConversationType.Channel ? "#" : "";
        var desc = !string.IsNullOrWhiteSpace(conversation.Description)
            ? $" — {conversation.Description}" : "";
        return $"{prefix}{conversation.Name}{desc}";
    }

    private static async Task<string?> GetConversationStatusAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId conversationId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Conversation>();
        var conversation = await (repo.GetByIdAsync(conversationId, cancellationToken)).ConfigureAwait(false);
        return conversation?.IsArchived == true ? "Archived" : "Active";
    }

    private static async Task<string> GetGlossaryTermLabelAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId termId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<GlossaryTerm>();
        var term = await (repo.GetByIdAsync(termId, cancellationToken)).ConfigureAwait(false);
        return term?.Term ?? termId.ToString()[..8];
    }

    private static async Task<string> GetArtifactLabelAsync(
        IUnitOfWork unitOfWork,
        GlobalUniqueId artifactId,
        CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<ArtifactTracking>();
        var artifact = await (repo.GetByIdAsync(artifactId, cancellationToken)).ConfigureAwait(false);
        return artifact?.EntityName ?? artifact?.ArtifactPath ?? artifactId.ToString()[..8];
    }
}
