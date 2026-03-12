using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception for unauthorized access that maps to HTTP 401.
/// </summary>
public class UnauthorizedException : SddpException
{
    /// <summary>
    /// Role required to perform the operation, when applicable.
    /// </summary>
    public RoleType? RequiredRole { get; }

    /// <summary>
    /// Current caller role, when applicable.
    /// </summary>
    public RoleType? CurrentRole { get; }

    public UnauthorizedException(string message)
        : base("UNAUTHORIZED", message)
    {
    }

    public UnauthorizedException(RoleType requiredRole, RoleType currentRole)
        : base("UNAUTHORIZED", $"Required role: {requiredRole}, but current role is: {currentRole}")
    {
        RequiredRole = requiredRole;
        CurrentRole = currentRole;
    }

    public UnauthorizedException(string action, RoleType currentRole)
        : base("UNAUTHORIZED", $"Role '{currentRole}' is not authorized to perform action: {action}")
    {
        CurrentRole = currentRole;
    }
}
