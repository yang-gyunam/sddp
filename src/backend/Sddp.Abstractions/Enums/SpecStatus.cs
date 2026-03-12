namespace Sddp.Abstractions.Enums;

/// <summary>
/// Enumeration representing the status of a Spec
/// State transitions: Draft -> InReview -> Approved -> Locked
/// </summary>
public enum SpecStatus
{
    /// <summary>Draft - editable</summary>
    Draft = 0,

    /// <summary>In Review - awaiting approval</summary>
    InReview = 1,

    /// <summary>Approved - code generation available</summary>
    Approved = 2,

    /// <summary>Locked - not editable (code generation completed)</summary>
    Locked = 3
}

/// <summary>
/// SpecStatus extension methods
/// </summary>
public static class SpecStatusExtensions
{
    /// <summary>
    /// Checks whether a transition to the specified status is allowed
    /// </summary>
    public static bool CanTransitionTo(this SpecStatus current, SpecStatus target)
    {
        return (current, target) switch
        {
            (SpecStatus.Draft, SpecStatus.InReview) => true,
            (SpecStatus.InReview, SpecStatus.Approved) => true,
            (SpecStatus.InReview, SpecStatus.Draft) => true, // Rejection
            (SpecStatus.Approved, SpecStatus.Locked) => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks whether the spec is in an editable status
    /// </summary>
    public static bool IsEditable(this SpecStatus status)
    {
        return status == SpecStatus.Draft;
    }
}
