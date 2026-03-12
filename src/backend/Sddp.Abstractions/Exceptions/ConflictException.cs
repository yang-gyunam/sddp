namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception for request conflicts that map to HTTP 409.
/// </summary>
public class ConflictException : SddpException
{
    public ConflictException(string message)
        : base("CONFLICT", message) { }
}
