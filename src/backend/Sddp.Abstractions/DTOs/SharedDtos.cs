namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Normalized user reference embedded in API responses.
/// </summary>
public sealed record UserRefDto(string Id, string? Name, string? AvatarUrl);
