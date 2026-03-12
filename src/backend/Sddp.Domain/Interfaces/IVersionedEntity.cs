using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Interfaces;

/// <summary>
/// Interface for entities that require version management.
/// </summary>
public interface IVersionedEntity
{
    /// <summary>
    /// Semantic version.
    /// </summary>
    SemanticVersion Version { get; }

    /// <summary>
    /// Start of the validity period (SCD Type 6).
    /// </summary>
    Timestamp ValidFrom { get; }

    /// <summary>
    /// End of the validity period (SCD Type 6). Null means the version is current.
    /// </summary>
    Timestamp? ValidTo { get; }

    /// <summary>
    /// Indicates whether this is the current valid version.
    /// </summary>
    bool IsCurrent { get; }
}
