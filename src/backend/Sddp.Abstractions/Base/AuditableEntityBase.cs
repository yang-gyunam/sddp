using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Base;

/// <summary>
/// Base class for entities that track creation and update ownership.
/// </summary>
public abstract class AuditableEntityBase : EntityBase
{
    /// <summary>
    /// User who created the entity.
    /// </summary>
    public GlobalUniqueId CreatedBy { get; protected set; }

    /// <summary>
    /// User who last updated the entity.
    /// </summary>
    public GlobalUniqueId UpdatedBy { get; protected set; }

    /// <summary>
    /// Sets the initial audit owner for a newly created entity.
    /// </summary>
    public void SetCreatedBy(GlobalUniqueId userId)
    {
        CreatedBy = userId;
        UpdatedBy = userId;
    }

    /// <summary>
    /// Updates the audit owner after a modification.
    /// </summary>
    public void SetUpdatedBy(GlobalUniqueId userId)
    {
        UpdatedBy = userId;
        MarkAsModified();
    }
}
