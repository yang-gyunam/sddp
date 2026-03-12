using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Base;

/// <summary>
/// Base class for versioned entities that follow an SCD Type 6-style lifecycle.
/// </summary>
public abstract class VersionedEntityBase : AuditableEntityBase
{
    /// <summary>
    /// Semantic version of the current entity state.
    /// </summary>
    public SemanticVersion Version { get; protected set; } = SemanticVersion.Initial;

    /// <summary>
    /// Timestamp when this version became effective.
    /// </summary>
    public Timestamp ValidFrom { get; protected set; } = Timestamp.Now;

    /// <summary>
    /// Timestamp when this version expired. `null` means the version is current.
    /// </summary>
    public Timestamp? ValidTo { get; protected set; }

    /// <summary>
    /// Indicates whether this version is the active one.
    /// </summary>
    public bool IsCurrent => ValidTo is null || ValidTo.Value.IsEmpty;

    /// <summary>
    /// Major (Breaking Change)
    /// </summary>
    protected void IncrementMajorVersion()
    {
        Version = Version.IncrementMajor();
        MarkAsModified();
    }

    /// <summary>
    /// Minor (Feature)
    /// </summary>
    protected void IncrementMinorVersion()
    {
        Version = Version.IncrementMinor();
        MarkAsModified();
    }

    /// <summary>
    /// Patch (Bug Fix)
    /// </summary>
    protected void IncrementPatchVersion()
    {
        Version = Version.IncrementPatch();
        MarkAsModified();
    }

    /// <summary>
    /// Expires the current version before a successor is created.
    /// </summary>
    public void Expire()
    {
        ValidTo = Timestamp.Now;
        MarkAsModified();
    }

    /// <summary>
    /// Creates the next version instance derived from the current one.
    /// </summary>
    protected abstract VersionedEntityBase CreateNewVersionInstance();
}
