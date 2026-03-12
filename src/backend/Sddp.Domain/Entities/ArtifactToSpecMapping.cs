using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Enums;

namespace Sddp.Domain.Entities;

/// <summary>
/// ↔ Spec entity ()
/// Spec //AI
/// </summary>
public class ArtifactToSpecMapping : AuditableEntityBase
{
    public GlobalUniqueId TenantId { get; private set; }
    public GlobalUniqueId ProjectId { get; private set; }
    public GlobalUniqueId SpecId { get; private set; }

    /// <summary>
    /// (: "src/PaymentService.cs")
    /// </summary>
    public string ArtifactPath { get; private set; } = string.Empty;

    /// <summary>
    ///
    /// </summary>
    public MappingReason MappingReason { get; private set; }

    /// <summary>
    /// (, nullable)
    /// </summary>
    public string? SourceContent { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Spec (Navigation)
    /// </summary>
    public Spec Spec { get; private set; } = null!;

    // EF Core default create
    private ArtifactToSpecMapping() { }

    public ArtifactToSpecMapping(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId,
        string artifactPath,
        MappingReason mappingReason,
        string? sourceContent = null,
        string? notes = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SpecId = specId;
        ArtifactPath = artifactPath;
        MappingReason = mappingReason;
        SourceContent = sourceContent;
        Notes = notes;
    }

    /// <summary>
    /// ()
    /// </summary>
    public void UpdateSource(string? sourceContent, string? notes)
    {
        SourceContent = sourceContent;
        if (notes is not null)
            Notes = notes;
        MarkAsModified();
    }
}
