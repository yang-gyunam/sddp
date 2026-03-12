namespace Sddp.Abstractions.DTOs;

public record ArtifactTrackingDto(
    string Id,
    string TenantId,
    string ProjectId,
    string SpecId,
    string ArtifactPath,
    string ArtifactType,
    string ContentHash,
    string GeneratorVersion,
    string TemplateVersion,
    string EntityName,
    string? GlossaryTermId,
    string? SourceConversationId,
    string? SourceRequirementId,
    string? OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ArtifactTrackingSummaryDto(
    string Id,
    string ArtifactPath,
    string ArtifactType,
    string EntityName,
    string SpecCode,
    string? SpecTitle,
    string? GlossaryTermId,
    string? GlossaryTermName,
    string? SourceConversationId,
    string? SourceConversationName,
    string? SourceRequirementId,
    string? SourceRequirementCode,
    UserRefDto? Owner,
    UserRefDto? CreatedBy,
    UserRefDto? UpdatedBy,
    string Status,
    string GeneratorVersion,
    string TemplateVersion,
    string ContentHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ArtifactRegenerateResult(
    string ArtifactId,
    string SpecId,
    string ArtifactPath,
    string NewContentHash,
    string PreviousContentHash,
    DateTimeOffset RegeneratedAt);

public record ArtifactVerifyResult(
    bool IsValid,
    string StoredHash,
    string CurrentHash,
    string ArtifactPath);

// --- Brownfield: Artifact-to-Spec Mapping ---

public record ArtifactToSpecMappingDto(
    string Id,
    string TenantId,
    string ProjectId,
    string SpecId,
    string ArtifactPath,
    string MappingReason,
    string? Notes,
    bool HasSource,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateArtifactMappingRequest(
    Guid SpecId,
    string ArtifactPath,
    string? MappingReason,
    string? SourceContent,
    string? Notes);

public record UpdateMappingSourceRequest(
    string? SourceContent,
    string? Notes);

public record ArtifactSearchResultDto(string Id, string ArtifactPath, string ArtifactType, string EntityName);
