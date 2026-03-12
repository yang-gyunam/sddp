using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Relationship entity for tracking links between entities
/// REQ-04.4: 8 relationship types
/// REQ-10.3: standalone relationship entity
/// </summary>
public class Relationship : EntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// Source entity type
    /// </summary>
    public EntityType FromEntityType { get; private set; }

    /// <summary>
    /// Source entity ID
    /// </summary>
    public GlobalUniqueId FromEntityId { get; private set; }

    /// <summary>
    /// Target entity type
    /// </summary>
    public EntityType ToEntityType { get; private set; }

    /// <summary>
    /// Target entity ID
    /// </summary>
    public GlobalUniqueId ToEntityId { get; private set; }

    /// <summary>
    /// Relationship type (8 supported types)
    /// </summary>
    public RelationType Type { get; private set; }

    /// <summary>
    /// Valid-from timestamp (SCD Type 6)
    /// </summary>
    public Timestamp ValidFrom { get; private set; } = Timestamp.Now;

    /// <summary>
    /// Valid-to timestamp (null means currently active)
    /// </summary>
    public Timestamp? ValidTo { get; private set; }

    /// <summary>
    /// Whether the relationship is currently active
    /// </summary>
    public bool IsCurrent => ValidTo == null;

    /// <summary>
    /// Reason for creating the relationship
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Spec ID that created the relationship (for traceability)
    /// </summary>
    public GlobalUniqueId? SourceSpecId { get; private set; }

    /// <summary>
    /// Decision message ID that created the relationship (for traceability)
    /// </summary>
    public GlobalUniqueId? SourceDecisionId { get; private set; }

    /// <summary>
    /// Creator ID
    /// </summary>
    public GlobalUniqueId CreatedBy { get; private set; }

    /// <summary>
    /// Source spec navigation property (when SourceSpecId is set)
    /// </summary>
    public Spec? SourceSpec { get; private set; }

    // Default constructor for EF Core
    private Relationship() { }

    /// <summary>
    /// Creates a new relationship
    /// </summary>
    public Relationship(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        EntityType fromEntityType,
        GlobalUniqueId fromEntityId,
        EntityType toEntityType,
        GlobalUniqueId toEntityId,
        RelationType type,
        GlobalUniqueId createdBy,
        string? reason = null,
        GlobalUniqueId? sourceSpecId = null,
        GlobalUniqueId? sourceDecisionId = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        FromEntityType = fromEntityType;
        FromEntityId = fromEntityId;
        ToEntityType = toEntityType;
        ToEntityId = toEntityId;
        Type = type;
        CreatedBy = createdBy;
        Reason = reason;
        SourceSpecId = sourceSpecId;
        SourceDecisionId = sourceDecisionId;
        ValidFrom = Timestamp.Now;
    }

    /// <summary>
    /// Invalidates the relationship by setting ValidTo
    /// </summary>
    public Result Invalidate()
    {
        if (ValidTo != null)
        {
            return DomainError.AlreadyPerformed("Invalidation");
        }

        ValidTo = Timestamp.Now;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Returns whether the relationship is active and currently valid
    /// </summary>
    public bool IsValid()
    {
        return IsActive && IsCurrent;
    }
}
