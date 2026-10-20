namespace Sddp.Abstractions.Interfaces;

/// <summary>Checks a remote repository without modifying the project's browsing cache.</summary>
public interface IRepositoryConnectionChecker
{
    Task CheckAsync(GitRemoteAccess access, string branch, string artifactRootPath,
        IReadOnlyList<string> existingPaths, CancellationToken cancellationToken);
    Task CheckAsync(string repoUrl, string branch, string artifactRootPath,
        IReadOnlyList<string> existingPaths, CancellationToken cancellationToken);
}
