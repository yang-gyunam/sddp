using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that require audit tracking.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Creation timestamp.
    /// </summary>
    Timestamp CreatedAt { get; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    Timestamp UpdatedAt { get; }

    /// <summary>
    /// Creator ID.
    /// </summary>
    GlobalUniqueId CreatedBy { get; }

    /// <summary>
    /// Last modifier ID.
    /// </summary>
    GlobalUniqueId UpdatedBy { get; }
}
