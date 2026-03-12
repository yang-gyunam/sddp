namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Standard API response DTO.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response data.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error code.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed response.
    /// </summary>
    public static ApiResponse<T> Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a not-found response.
    /// </summary>
    public static ApiResponse<T> NotFound(string entityType, string entityId) => new()
    {
        Success = false,
        ErrorCode = "NOT_FOUND",
        ErrorMessage = $"{entityType} with ID '{entityId}' was not found."
    };

    /// <summary>
    /// Creates a validation-failed response.
    /// </summary>
    public static ApiResponse<T> ValidationFailed(string errorMessage) => new()
    {
        Success = false,
        ErrorCode = "VALIDATION_FAILED",
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates an unauthorized response.
    /// </summary>
    public static ApiResponse<T> Unauthorized(string errorMessage = "Unauthorized access") => new()
    {
        Success = false,
        ErrorCode = "UNAUTHORIZED",
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// API response without data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response with no data.
    /// </summary>
    public static ApiResponse Ok() => new()
    {
        Success = true
    };

    /// <summary>
    /// Creates a failed response.
    /// </summary>
    public new static ApiResponse Fail(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
