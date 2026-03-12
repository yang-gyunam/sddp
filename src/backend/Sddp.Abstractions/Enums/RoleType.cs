namespace Sddp.Abstractions.Enums;

/// <summary>
/// Built-in user role types.
/// </summary>
public enum RoleType
{
    /// <summary>Product owner who defines requirements and priorities.</summary>
    ProductOwner = 0,

    /// <summary>Domain expert who provides business logic and domain knowledge.</summary>
    DomainExpert = 1,

    /// <summary>Developer who implements code and technical design.</summary>
    Developer = 2,

    /// <summary>Reviewer who reviews specs and code.</summary>
    Reviewer = 3,

    /// <summary>QA tester who validates quality and test outcomes.</summary>
    QATester = 4,

    /// <summary>Administrator who manages the overall system.</summary>
    Admin = 5
}

/// <summary>
/// Convenience helpers for role capabilities.
/// </summary>
public static class RoleTypeExtensions
{
    /// <summary>
    /// Returns whether the role can create specs.
    /// </summary>
    public static bool CanCreateSpec(this RoleType role)
    {
        return role is RoleType.ProductOwner or RoleType.DomainExpert or RoleType.Developer;
    }

    /// <summary>
    /// Returns whether the role can approve specs.
    /// </summary>
    public static bool CanApproveSpec(this RoleType role)
    {
        return role is RoleType.ProductOwner or RoleType.Reviewer;
    }

    /// <summary>
    /// Returns whether the role can perform write operations.
    /// </summary>
    public static bool HasWritePermission(this RoleType role)
    {
        return true;
    }
}
