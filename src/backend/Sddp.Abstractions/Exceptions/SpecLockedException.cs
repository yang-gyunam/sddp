using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when a locked spec is modified.
/// </summary>
public class SpecLockedException : SddpException
{
    /// <summary>
    /// Identifier of the locked spec.
    /// </summary>
    public GlobalUniqueId SpecId { get; }

    /// <summary>
    /// Timestamp when the spec was locked.
    /// </summary>
    public Timestamp LockedAt { get; }

    public SpecLockedException(GlobalUniqueId specId, Timestamp lockedAt)
        : base("SPEC_LOCKED", $"Spec '{specId}' is locked and cannot be modified. Locked at: {lockedAt}")
    {
        SpecId = specId;
        LockedAt = lockedAt;
    }

    public SpecLockedException(GlobalUniqueId specId)
        : base("SPEC_LOCKED", $"Spec '{specId}' is locked and cannot be modified.")
    {
        SpecId = specId;
        LockedAt = Timestamp.Empty;
    }
}
