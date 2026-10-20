using System.ComponentModel.DataAnnotations;
using Sddp.Abstractions.ValueObjects;
namespace Sddp.Abstractions.DTOs;

/// <summary>Connection metadata. Credentials are never returned.</summary>
public sealed record GitConnectionDto(string Id, string ProjectId, string Name, string Provider,
    string ServerUrl, string Username, bool HasToken, int Version, DateTimeOffset? CheckedAt);
/// <summary>Connection list and immutable distribution policy.</summary>
public sealed record GitConnectionListDto(bool AllowsPrivateRepositories, IReadOnlyList<GitConnectionDto> Items);
/// <summary>Creates a connection restricted to one project.</summary>
public sealed record CreateGitConnectionDto
{
    [Required] public string ProjectId { get; init; } = "";
    [Required, MaxLength(100)] public string Name { get; init; } = "";
    [Required] public string Provider { get; init; } = "GitHub";
    [Required, MaxLength(2048)] public string ServerUrl { get; init; } = "";
    [MaxLength(200)] public string Username { get; init; } = "";
    [MaxLength(4096)] public string? Token { get; init; }
    public override string ToString() => "CreateGitConnectionDto { credentials redacted }";
}
/// <summary>Updates metadata or replaces a token; null preserves the current token.</summary>
public sealed record UpdateGitConnectionDto
{
    [Required, MaxLength(100)] public string Name { get; init; } = "";
    [MaxLength(200)] public string Username { get; init; } = "";
    [MaxLength(4096)] public string? Token { get; init; }
    public bool RemoveToken { get; init; }
    [Range(0,int.MaxValue)] public int ExpectedVersion { get; init; }
    public override string ToString() => "UpdateGitConnectionDto { credentials redacted }";
}
