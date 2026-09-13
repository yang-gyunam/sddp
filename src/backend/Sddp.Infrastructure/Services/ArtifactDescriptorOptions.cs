namespace Sddp.Infrastructure.Services;

public class ArtifactDescriptorOptions
{
    /// <summary>
    /// Root of SDDP-owned descriptor storage. Deliberately separate from
    /// <see cref="GitRepositoryOptions.RepoRoot"/> so descriptors never land in a
    /// customer's checkout.
    /// </summary>
    public string Root { get; set; } = "data/artifact-descriptors";
}
