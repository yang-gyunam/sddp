namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception for forbidden access that maps to HTTP 403.
/// </summary>
public class ForbiddenAccessException : SddpException
{
    public ForbiddenAccessException(string message)
        : base("FORBIDDEN", message)
    {
    }

    public ForbiddenAccessException(string resource, string action)
        : base("FORBIDDEN", $"Access to '{resource}' for action '{action}' is forbidden")
    {
    }
}
