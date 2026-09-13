using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// Artifact (Git)
/// </summary>
public interface IArtifactRepositoryService
{
    /// <summary>
    /// project get
    /// </summary>
    Task<ArtifactRepositoryContext> GetProjectContextAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// project status
    /// </summary>
    Task EnsureProjectSyncedAsync(
        ArtifactRepositoryContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Artifact get ()
    /// </summary>
    Task<ArtifactFileInfo> GetArtifactFileInfoAsync(
        ArtifactRepositoryContext context,
        string artifactPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the artifact's actual contents for review. Returns null when the file is
    /// absent or the path escapes the artifact root.
    /// </summary>
    Task<ArtifactFileContent?> GetArtifactContentAsync(
        ArtifactRepositoryContext context,
        string artifactPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Artifact
/// </summary>
public record ArtifactRepositoryContext(
    GlobalUniqueId TenantId,
    GlobalUniqueId ProjectId,
    string RepoPath,
    string RepoUrl,
    string RepoBranch,
    string ArtifactRootPath,
    int SyncIntervalMinutes,
    DateTimeOffset? LastSyncedAt);

/// <summary>
/// Artifact
/// </summary>
public record ArtifactFileInfo(
    bool Exists,
    string? Hash,
    string FullPath);

public record ArtifactFileContent(
    string? Content,
    string? Hash,
    long SizeBytes,
    bool IsBinary,
    bool TooLarge = false,
    long MaxBytes = 0);
