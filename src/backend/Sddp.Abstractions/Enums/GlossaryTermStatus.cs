namespace Sddp.Abstractions.Enums;

/// <summary>
/// Enumeration representing the status of a glossary term
/// State transitions: Draft -> Active <-> Deprecated
/// </summary>
public enum GlossaryTermStatus
{
    /// <summary>Draft - editable, awaiting review</summary>
    Draft = 0,

    /// <summary>Active - approved and in use</summary>
    Active = 1,

    /// <summary>Deprecated - no longer in use (replacement term may exist)</summary>
    Deprecated = 2
}

/// <summary>
/// GlossaryTermStatus extension methods
/// </summary>
public static class GlossaryTermStatusExtensions
{
    /// <summary>
    /// Checks whether a transition to the specified status is allowed
    /// </summary>
    public static bool CanTransitionTo(this GlossaryTermStatus current, GlossaryTermStatus target)
    {
        return (current, target) switch
        {
            (GlossaryTermStatus.Draft, GlossaryTermStatus.Active) => true,     // Approval
            (GlossaryTermStatus.Draft, GlossaryTermStatus.Deprecated) => true, // Deprecate draft
            (GlossaryTermStatus.Active, GlossaryTermStatus.Deprecated) => true, // Deprecate
            (GlossaryTermStatus.Active, GlossaryTermStatus.Draft) => true,     // Revert to draft for re-review
            (GlossaryTermStatus.Deprecated, GlossaryTermStatus.Active) => true, // Reactivate
            _ => false
        };
    }

    /// <summary>
    /// Checks whether the term is in an editable status
    /// </summary>
    public static bool IsEditable(this GlossaryTermStatus status)
    {
        return status == GlossaryTermStatus.Draft;
    }

    /// <summary>
    /// Checks whether the term is in a usable status (can be referenced from Specs)
    /// </summary>
    public static bool IsUsable(this GlossaryTermStatus status)
    {
        return status == GlossaryTermStatus.Active;
    }
}
