namespace Sddp.Abstractions.Constants;

/// <summary>
/// Business-rule limit constants.
/// </summary>
public static class BusinessLimits
{
    /// <summary>
    /// Maximum number of active members per project, including the PO.
    /// </summary>
    public const int MaxProjectMembers = 10;

    /// <summary>
    /// Maximum number of active projects per tenant.
    /// </summary>
    public const int MaxActiveProjectsPerTenant = 5;
}
