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


public record ArtifactVerifyResult(
    bool IsValid,
    string StoredHash,
    string CurrentHash,
    string ArtifactPath,
    bool FileExists = true);

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

public record ArtifactDescriptorDto(
    string ArtifactId,
    int SchemaVersion,
    string Yaml);

public record ArtifactContentDto(
    string ArtifactId,
    string Path,
    string? Content,
    string? ContentHash,
    long SizeBytes,
    bool IsBinary,
    string Status,
    string Language,
    bool TooLarge = false,
    long MaxBytes = 0);

public record ArtifactStatusCounts(int Valid, int Modified, int Missing, int Unverified);

/// <summary>
/// A page of artifacts plus the project-wide counts.
///
/// Derives from PagedResult so the response keeps the same shape it always had -- items,
/// totalCount, pageNumber and the rest stay where callers expect them, with statusCounts added
/// alongside. TotalCount reflects the filtered set, because that is what paging is over;
/// StatusCounts reflects the whole project, because that is what the dashboard is asking.
/// </summary>
public sealed class ProjectArtifactsPage : PagedResult<ArtifactTrackingSummaryDto>
{
    public ArtifactStatusCounts StatusCounts { get; init; } = new(0, 0, 0, 0);
}
