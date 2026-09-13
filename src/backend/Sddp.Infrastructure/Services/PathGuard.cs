namespace Sddp.Infrastructure.Services;

internal static class PathGuard
{
    internal static bool IsContainedIn(string candidateFullPath, string rootFullPath)
    {
        if (string.IsNullOrEmpty(candidateFullPath) || string.IsNullOrEmpty(rootFullPath))
            return false;

        var normalizedRoot = rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return candidateFullPath.StartsWith(normalizedRoot, StringComparison.Ordinal);
    }
}
