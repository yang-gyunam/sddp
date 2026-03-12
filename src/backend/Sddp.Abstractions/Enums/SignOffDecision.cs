namespace Sddp.Abstractions.Enums;

/// <summary>
/// Sign-off decision types
/// REQ-04.3: Decision stakeholder sign-off
/// </summary>
public enum SignOffDecision
{
    /// <summary>Pending - decision not yet made</summary>
    Pending = 0,

    /// <summary>Approved - spec approved</summary>
    Approved = 1,

    /// <summary>Rejected - spec rejected</summary>
    Rejected = 2,

    /// <summary>Conditional - approved upon condition fulfillment</summary>
    Conditional = 3
}

/// <summary>
/// SignOffDecision extension methods
/// </summary>
public static class SignOffDecisionExtensions
{
    /// <summary>
    /// Checks whether the sign-off has been decided (any status other than Pending)
    /// </summary>
    public static bool IsDecided(this SignOffDecision decision)
    {
        return decision != SignOffDecision.Pending;
    }

    /// <summary>
    /// Checks whether the decision is an approval type (Approved or Conditional)
    /// </summary>
    public static bool IsApprovalType(this SignOffDecision decision)
    {
        return decision is SignOffDecision.Approved or SignOffDecision.Conditional;
    }
}
