namespace Sddp.Infrastructure.Services;

public class GitRepositoryOptions
{
    public string? ServerUrl { get; init; }
    public string RepoRoot { get; init; } = "data/git-repos";
    public long MaxArtifactContentBytes { get; init; } = 5L * 1024 * 1024;
}
