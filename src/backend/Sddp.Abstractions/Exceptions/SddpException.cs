namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Base application exception that carries an explicit error code.
/// </summary>
public class SddpException : Exception
{
    /// <summary>
    /// Machine-readable error code associated with the exception.
    /// </summary>
    public string ErrorCode { get; }

    public SddpException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public SddpException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
