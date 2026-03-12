namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// HTTP
/// Application HttpContext
/// </summary>
public interface IRequestContextProvider
{
    /// <summary> IP </summary>
    string? IpAddress { get; }

    /// <summary>User-Agent </summary>
    string? UserAgent { get; }

    /// <summary> relationship ID (Trace Identifier)</summary>
    string? CorrelationId { get; }
}
