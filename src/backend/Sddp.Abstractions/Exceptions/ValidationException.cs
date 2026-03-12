namespace Sddp.Abstractions.Exceptions;

/// <summary>
/// Exception for validation failures.
/// </summary>
public class ValidationException : SddpException
{
    /// <summary>
    /// Validation errors grouped by field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION_FAILED", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base("VALIDATION_FAILED", error)
    {
        Errors = new Dictionary<string, string[]> { { field, [error] } };
    }

    public ValidationException(string message)
        : base("VALIDATION_FAILED", message)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
