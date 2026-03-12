namespace Sddp.Abstractions.Enums;

/// <summary>
/// Requirement hierarchy level
/// </summary>
public enum RequirementLevel
{
    /// <summary>Level A - high-level/business requirements (targeting ProductOwner)</summary>
    A = 0,

    /// <summary>Level B - mid-level/user behavior requirements (targeting DomainExpert)</summary>
    B = 1,

    /// <summary>Level C - low-level/detailed spec requirements (targeting Developer)</summary>
    C = 2
}

/// <summary>
/// RequirementLevel extension methods
/// </summary>
public static class RequirementLevelExtensions
{
    /// <summary>
    /// Returns the primary audience role for this level
    /// </summary>
    public static RoleType GetPrimaryAudience(this RequirementLevel level)
    {
        return level switch
        {
            RequirementLevel.A => RoleType.ProductOwner,
            RequirementLevel.B => RoleType.DomainExpert,
            RequirementLevel.C => RoleType.Developer,
            _ => RoleType.Developer
        };
    }

    /// <summary>
    /// Returns the level description
    /// </summary>
    public static string GetDescription(this RequirementLevel level)
    {
        return level switch
        {
            RequirementLevel.A => "Business Requirements",
            RequirementLevel.B => "User Behavior Requirements",
            RequirementLevel.C => "Detailed Spec Requirements",
            _ => "Unknown"
        };
    }
}
