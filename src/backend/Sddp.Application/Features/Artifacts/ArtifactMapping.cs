using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts;

internal static class ArtifactMapping
{
    internal static ArtifactTrackingDto MapToDto(ArtifactTracking artifact)
    {
        return new ArtifactTrackingDto(
            Id: artifact.Id.ToString(),
            TenantId: artifact.TenantId.ToString(),
            ProjectId: artifact.ProjectId.ToString(),
            SpecId: artifact.SpecId.ToString(),
            ArtifactPath: artifact.ArtifactPath,
            ArtifactType: artifact.ArtifactType.ToString(),
            ContentHash: artifact.ContentHash,
            GeneratorVersion: artifact.GeneratorVersion,
            TemplateVersion: artifact.TemplateVersion,
            EntityName: artifact.EntityName,
            GlossaryTermId: artifact.GlossaryTermId?.ToString(),
            SourceConversationId: artifact.SourceConversationId?.ToString(),
            SourceRequirementId: artifact.SourceRequirementId?.ToString(),
            OwnerUserId: artifact.OwnerUserId?.ToString(),
            CreatedAt: artifact.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: artifact.UpdatedAt.ToDateTimeOffset());
    }

    internal static ArtifactTrackingSummaryDto MapToSummaryDto(
        ArtifactTracking artifact,
        Dictionary<GlobalUniqueId, string> specCodeMap,
        Dictionary<GlobalUniqueId, string> specTitleMap,
        Dictionary<GlobalUniqueId, string>? glossaryTermNameMap,
        Dictionary<GlobalUniqueId, string>? conversationNameMap,
        Dictionary<GlobalUniqueId, string>? requirementCodeMap,
        Dictionary<GlobalUniqueId, UserRefDto>? userRefMap,
        string status)
    {
        string? glossaryTermName = null;
        if (artifact.GlossaryTermId.HasValue && glossaryTermNameMap != null)
        {
            glossaryTermNameMap.TryGetValue(artifact.GlossaryTermId.Value, out glossaryTermName);
        }

        string? conversationName = null;
        if (artifact.SourceConversationId.HasValue && conversationNameMap != null)
        {
            conversationNameMap.TryGetValue(artifact.SourceConversationId.Value, out conversationName);
        }

        string? requirementCode = null;
        if (artifact.SourceRequirementId.HasValue && requirementCodeMap != null)
        {
            requirementCodeMap.TryGetValue(artifact.SourceRequirementId.Value, out requirementCode);
        }

        UserRefDto? owner = null;
        if (artifact.OwnerUserId.HasValue && userRefMap != null)
        {
            userRefMap.TryGetValue(artifact.OwnerUserId.Value, out owner);
        }

        UserRefDto? createdBy = null;
        UserRefDto? updatedBy = null;
        if (userRefMap != null)
        {
            userRefMap.TryGetValue(artifact.CreatedBy, out createdBy);
            userRefMap.TryGetValue(artifact.UpdatedBy, out updatedBy);
        }

        return new ArtifactTrackingSummaryDto(
            Id: artifact.Id.ToString(),
            ArtifactPath: artifact.ArtifactPath,
            ArtifactType: artifact.ArtifactType.ToString(),
            EntityName: artifact.EntityName,
            SpecCode: specCodeMap.TryGetValue(artifact.SpecId, out var code) ? code : string.Empty,
            SpecTitle: specTitleMap.TryGetValue(artifact.SpecId, out var title) ? title : null,
            GlossaryTermId: artifact.GlossaryTermId?.ToString(),
            GlossaryTermName: glossaryTermName,
            SourceConversationId: artifact.SourceConversationId?.ToString(),
            SourceConversationName: conversationName,
            SourceRequirementId: artifact.SourceRequirementId?.ToString(),
            SourceRequirementCode: requirementCode,
            Owner: owner,
            CreatedBy: createdBy,
            UpdatedBy: updatedBy,
            Status: status,
            GeneratorVersion: artifact.GeneratorVersion,
            TemplateVersion: artifact.TemplateVersion,
            ContentHash: artifact.ContentHash,
            CreatedAt: artifact.CreatedAt.ToDateTimeOffset(),
            UpdatedAt: artifact.UpdatedAt.ToDateTimeOffset());
    }

    internal static ArtifactSearchResultDto MapToSearchResultDto(ArtifactTracking artifact)
    {
        return new ArtifactSearchResultDto(
            Id: artifact.Id.ToString(),
            ArtifactPath: artifact.ArtifactPath,
            ArtifactType: artifact.ArtifactType.ToString(),
            EntityName: artifact.EntityName);
    }
}
