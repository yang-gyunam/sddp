namespace Sddp.Infrastructure.Services;

public class GitRepositoryOptions
{
    public string RepoRoot { get; init; } = "data/git-repos";
    public string Username { get; init; } = "oauth2";
    public string? Token { get; init; }

    public long MaxArtifactContentBytes { get; init; } = 5L * 1024 * 1024;
}
