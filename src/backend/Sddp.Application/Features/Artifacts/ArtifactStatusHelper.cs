using Sddp.Abstractions.Interfaces;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Artifacts;

internal static class ArtifactStatusHelper
{
    internal static async Task<string> DetermineStatusAsync(
        IArtifactRepositoryService artifactRepositoryService,
        ArtifactRepositoryContext context,
        ArtifactTracking artifact,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artifact.ArtifactPath))
            return "Missing";

        try
        {
            var fileInfo = await (artifactRepositoryService.GetArtifactFileInfoAsync(
                context,
                artifact.ArtifactPath,
                cancellationToken)).ConfigureAwait(false);

            if (!fileInfo.Exists)
                return "Missing";

            if (string.IsNullOrWhiteSpace(artifact.ContentHash) || string.IsNullOrWhiteSpace(fileInfo.Hash))
                return "Missing";

            return string.Equals(fileInfo.Hash, artifact.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? "Valid"
                : "Modified";
        }
        catch
        {
            return "Missing";
        }
    }

    internal static string ComputeHash(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
