namespace Sddp.Abstractions.Base;

/// <summary>
/// Represents a recoverable domain validation error.
/// Used for business rule violations such as invalid status transitions.
/// </summary>
public sealed record DomainError(string Code, string Message)
{
    public static DomainError InvalidTransition(string operation, string currentStatus) =>
        new("INVALID_TRANSITION", $"Cannot {operation} in {currentStatus} status");

    public static DomainError InvalidStatus(string operation, string status) =>
        new("INVALID_STATUS", $"Cannot {operation} in {status} status");

    public static DomainError AlreadyPerformed(string operation) =>
        new("ALREADY_PERFORMED", $"{operation} has already been performed");

    public static DomainError Inconsistency(string message) =>
        new("INCONSISTENCY", message);

    public static DomainError ValidationFailed(string message) =>
        new("VALIDATION_FAILED", message);
}
