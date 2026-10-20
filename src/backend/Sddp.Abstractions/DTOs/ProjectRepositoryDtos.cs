using System.ComponentModel.DataAnnotations;

namespace Sddp.Abstractions.DTOs;

/// <summary>Saved project repository settings and durable confirmation state.</summary>
public sealed record ProjectRepositoryDto(string RepoUrl, string RepoBranch, string ArtifactRootPath,
    int SyncIntervalMinutes, int Version, DateTimeOffset? CheckedAt, DateTimeOffset? ConfirmedAt,
    int RelationshipCount, bool CanConfirm, bool IsReadOnly, string? GitConnectionId = null, string? Visibility = null);

/// <summary>Repository draft with the version the editor originally loaded.</summary>
public sealed record UpdateProjectRepositoryDto
{
    public string? GitConnectionId { get; init; }
    [Required, MaxLength(2048)] public string RepoUrl { get; init; } = "";
    [Required, MaxLength(255)] public string RepoBranch { get; init; } = "main";
    [MaxLength(1024)] public string ArtifactRootPath { get; init; } = "";
    [Range(1, 10080)] public int SyncIntervalMinutes { get; init; } = 60;
    [Range(0, int.MaxValue)] public int ExpectedVersion { get; init; }
}

/// <summary>Identifies the exact saved repository configuration being checked or confirmed.</summary>
public sealed record RepositoryVersionDto([Range(0, int.MaxValue)] int ExpectedVersion);
