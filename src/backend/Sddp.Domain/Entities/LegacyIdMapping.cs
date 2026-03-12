using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// Legacy system ID mapping entity.
/// Links existing system IDs to SDDP IDs in brownfield environments.
/// </summary>
public class LegacyIdMapping : EntityBase
{
    /// <summary>
    /// Type of entity being mapped (User, Spec, Requirement, etc.).
    /// </summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>
    /// Internal SDDP GlobalUniqueId.
    /// </summary>
    public GlobalUniqueId EntityId { get; private set; }

    /// <summary>
    /// Legacy system identifier (for example, "JIRA", "Confluence", "Legacy-ERP").
    /// </summary>
    public string LegacySystem { get; private set; } = string.Empty;

    /// <summary>
    /// ID value from the legacy system.
    /// </summary>
    public string LegacyId { get; private set; } = string.Empty;

    /// <summary>
    /// Additional metadata, optionally stored as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    // Parameterless constructor for EF Core.
    private LegacyIdMapping() { }

    public LegacyIdMapping(string entityType, GlobalUniqueId entityId, string legacySystem, string legacyId, string? metadata = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        LegacySystem = legacySystem;
        LegacyId = legacyId;
        Metadata = metadata;
    }

    /// <summary>
    /// Updates metadata.
    /// </summary>
    public void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
        MarkAsModified();
    }

    /// <summary>
    /// Creates a mapping for import scenarios.
    /// </summary>
    public static LegacyIdMapping CreateForImport(string entityType, GlobalUniqueId entityId, string legacySystem, string legacyId)
    {
        return new LegacyIdMapping(entityType, entityId, legacySystem, legacyId, $"{{\"importedAt\": \"{Timestamp.Now}\"}}");
    }

    /// <summary>
    /// Creates a mapping for export scenarios.
    /// </summary>
    public static LegacyIdMapping CreateForExport(string entityType, GlobalUniqueId entityId, string legacySystem, string legacyId)
    {
        return new LegacyIdMapping(entityType, entityId, legacySystem, legacyId, $"{{\"exportedAt\": \"{Timestamp.Now}\"}}");
    }
}
