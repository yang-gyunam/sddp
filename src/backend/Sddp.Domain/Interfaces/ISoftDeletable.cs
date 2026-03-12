using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Deletion timestamp.
    /// </summary>
    Timestamp? DeletedAt { get; }

    /// <summary>
    /// Deleter ID.
    /// </summary>
    GlobalUniqueId? DeletedBy { get; }
}
