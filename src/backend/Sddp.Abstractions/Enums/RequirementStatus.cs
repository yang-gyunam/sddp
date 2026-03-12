namespace Sddp.Abstractions.Enums;

/// <summary>
/// Requirement status
/// </summary>
public enum RequirementStatus
{
    /// <summary>Drafting</summary>
    Draft = 0,

    /// <summary>Under review</summary>
    InReview = 1,

    /// <summary>Approved</summary>
    Approved = 2,

    /// <summary>Deprecated</summary>
    Deprecated = 3
}

/// <summary>
/// RequirementStatus extension methods
/// </summary>
public static class RequirementStatusExtensions
{
    /// <summary>
    /// Returns whether a transition to the target state is allowed
    /// </summary>
    public static bool CanTransitionTo(this RequirementStatus current, RequirementStatus target)
    {
        return (current, target) switch
        {
            // Draft -> InReview or Deprecated
            (RequirementStatus.Draft, RequirementStatus.InReview) => true,
            (RequirementStatus.Draft, RequirementStatus.Deprecated) => true,

            // InReview -> Approved or Draft (rejected) or Deprecated
            (RequirementStatus.InReview, RequirementStatus.Approved) => true,
            (RequirementStatus.InReview, RequirementStatus.Draft) => true,
            (RequirementStatus.InReview, RequirementStatus.Deprecated) => true,

            // Approved -> Deprecated
            (RequirementStatus.Approved, RequirementStatus.Deprecated) => true,

            // All other transitions are invalid
            _ => false
        };
    }

    /// <summary>
    /// Returns the status description
    /// </summary>
    public static string GetDescription(this RequirementStatus status)
    {
        return status switch
        {
            RequirementStatus.Draft => "Draft",
            RequirementStatus.InReview => "In Review",
            RequirementStatus.Approved => "Approved",
            RequirementStatus.Deprecated => "Deprecated",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Returns whether the state is editable
    /// </summary>
    public static bool IsEditable(this RequirementStatus status)
    {
        return status == RequirementStatus.Draft;
    }
}
