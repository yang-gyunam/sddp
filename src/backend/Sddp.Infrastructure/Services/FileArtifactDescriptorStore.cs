using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.Models;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services;

/// <summary>
/// Writes descriptors under an SDDP-owned root, mirroring the artifact layout:
/// <c>{Root}/{projectId}/src/Domain/Order.cs.yaml</c>.
///
/// Persist and back up this directory. An automatic bulk regeneration pass is not implemented.
/// </summary>
public sealed class FileArtifactDescriptorStore : IArtifactDescriptorStore
{
    private readonly IArtifactDescriptorSerializer _serializer;
    private readonly ILogger<FileArtifactDescriptorStore> _logger;
    private readonly string _root;

    public FileArtifactDescriptorStore(
        IArtifactDescriptorSerializer serializer,
        IOptions<ArtifactDescriptorOptions> options,
        ILogger<FileArtifactDescriptorStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _serializer = serializer;
        _logger = logger;
        _root = Path.GetFullPath(options.Value.Root, Directory.GetCurrentDirectory());
    }

    public string? ResolvePath(GlobalUniqueId projectId, string artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
            return null;

        var projectRoot = Path.Combine(_root, projectId.ToString());
        var projectRootFullPath = Path.GetFullPath(projectRoot);

        var sanitized = artifactPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(projectRootFullPath, sanitized + ".yaml"));

        return PathGuard.IsContainedIn(fullPath, projectRootFullPath) ? fullPath : null;
    }

    public async Task WriteAsync(
        GlobalUniqueId projectId,
        ArtifactDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var fullPath = ResolvePath(projectId, descriptor.Artifact.Path);
        if (fullPath is null)
        {
            _logger.LogWarning(
                "Refusing to write descriptor for artifact path outside project root: {ArtifactPath}",
                descriptor.Artifact.Path);
            return;
        }

        var yaml = _serializer.Serialize(descriptor);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await (File.WriteAllTextAsync(fullPath, yaml, cancellationToken)).ConfigureAwait(false);
    }

    public Task DeleteAsync(
        GlobalUniqueId projectId,
        string artifactPath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(projectId, artifactPath);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
