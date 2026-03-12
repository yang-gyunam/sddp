using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Enums;

namespace Sddp.Domain.Entities;

/// <summary>
/// Generated artifact tracking entity
/// Preserves traceability between generator output files and their source specs
/// </summary>
public class ArtifactTracking : AuditableEntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// Linked spec ID
    /// </summary>
    public GlobalUniqueId SpecId { get; private set; }

    /// <summary>
    /// Artifact file path (for example: "Employee.g.cs")
    /// </summary>
    public string ArtifactPath { get; private set; } = string.Empty;

    /// <summary>
    /// Artifact type
    /// </summary>
    public ArtifactType ArtifactType { get; private set; }

    /// <summary>
    /// File content hash (SHA-256)
    /// </summary>
    public string ContentHash { get; private set; } = string.Empty;

    /// <summary>
    /// Generator version
    /// </summary>
    public string GeneratorVersion { get; private set; } = string.Empty;

    /// <summary>
    /// Template version
    /// </summary>
    public string TemplateVersion { get; private set; } = string.Empty;

    /// <summary>
    /// Artifact category (Code, Database, Other)
    /// </summary>
    public string ArtifactCategory { get; private set; } = "Code";

    /// <summary>
    /// Database object name (when artifact_category = 'Database')
    /// </summary>
    public string? DbObjectName { get; private set; }

    /// <summary>
    /// Database schema name
    /// </summary>
    public string? DbSchema { get; private set; }

    /// <summary>
    /// Dependent database objects (JSON)
    /// </summary>
    public string? DependsOn { get; private set; }

    /// <summary>
    /// Entity name (PascalCase)
    /// </summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>
    /// Linked glossary term ID
    /// </summary>
    public GlobalUniqueId? GlossaryTermId { get; private set; }

    /// <summary>
    /// Linked conversation ID
    /// </summary>
    public GlobalUniqueId? SourceConversationId { get; private set; }

    /// <summary>
    /// Linked requirement ID
    /// </summary>
    public GlobalUniqueId? SourceRequirementId { get; private set; }

    /// <summary>
    /// Artifact owner user ID
    /// </summary>
    public GlobalUniqueId? OwnerUserId { get; private set; }

    /// <summary>
    /// Linked spec navigation property
    /// </summary>
    public Spec Spec { get; private set; } = null!;

    // Default constructor for EF Core
    private ArtifactTracking() { }

    /// <summary>
    /// Creates an ArtifactTracking entity
    /// </summary>
    public ArtifactTracking(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId,
        string artifactPath,
        ArtifactType artifactType,
        string contentHash,
        string generatorVersion,
        string templateVersion,
        string entityName)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SpecId = specId;
        ArtifactPath = artifactPath;
        ArtifactType = artifactType;
        ContentHash = contentHash;
        GeneratorVersion = generatorVersion;
        TemplateVersion = templateVersion;
        EntityName = entityName;
    }

    /// <summary>
    /// Sets the source conversation link
    /// </summary>
    public void SetSourceConversation(GlobalUniqueId? conversationId)
    {
        SourceConversationId = conversationId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the source requirement link
    /// </summary>
    public void SetSourceRequirement(GlobalUniqueId? requirementId)
    {
        SourceRequirementId = requirementId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the owner
    /// </summary>
    public void SetOwner(GlobalUniqueId? userId)
    {
        OwnerUserId = userId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the glossary-term link
    /// </summary>
    public void SetGlossaryTerm(GlobalUniqueId? termId)
    {
        GlossaryTermId = termId;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the artifact file path
    /// </summary>
    public void UpdatePath(string artifactPath)
    {
        ArtifactPath = artifactPath;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the artifact type
    /// </summary>
    public void UpdateArtifactType(ArtifactType artifactType)
    {
        ArtifactType = artifactType;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the entity name
    /// </summary>
    public void UpdateEntityName(string entityName)
    {
        EntityName = entityName;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the hash after regeneration
    /// </summary>
    public void UpdateHash(string contentHash, string generatorVersion, string templateVersion)
    {
        ContentHash = contentHash;
        GeneratorVersion = generatorVersion;
        TemplateVersion = templateVersion;
        MarkAsModified();
    }

    /// <summary>
    /// Verifies the hash to detect tampering
    /// </summary>
    public bool VerifyHash(string currentHash)
    {
        return string.Equals(ContentHash, currentHash, StringComparison.OrdinalIgnoreCase);
    }
}
