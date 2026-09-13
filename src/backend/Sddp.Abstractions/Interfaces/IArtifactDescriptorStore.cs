using Sddp.Abstractions.Models;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Interfaces;

public interface IArtifactDescriptorStore
{
    /// <summary>
    /// Writes the descriptor, replacing any existing one for the same artifact path.
    /// </summary>
    Task WriteAsync(
        GlobalUniqueId projectId,
        ArtifactDescriptor descriptor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a descriptor. No-op when the file is absent.
    /// </summary>
    Task DeleteAsync(
        GlobalUniqueId projectId,
        string artifactPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Absolute path the descriptor for <paramref name="artifactPath"/> would occupy,
    /// mirroring the artifact layout so a reader can derive it from the file path alone.
    /// Returns null when the path escapes the project's descriptor directory.
    /// </summary>
    string? ResolvePath(GlobalUniqueId projectId, string artifactPath);
}

/// <summary>
/// Converts descriptors to and from YAML.
/// </summary>
public interface IArtifactDescriptorSerializer
{
    string Serialize(ArtifactDescriptor descriptor);

    ArtifactDescriptor Deserialize(string yaml);
}
