using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Infrastructure.Services;

/// <summary>Isolated shallow Git inspection; no checkout, hooks, submodules or customer cache writes.</summary>
public sealed class GitRepositoryConnectionChecker : IRepositoryConnectionChecker
{
    public Task CheckAsync(string repoUrl, string branch, string artifactRootPath,
        IReadOnlyList<string> existingPaths, CancellationToken cancellationToken) =>
        CheckAsync(new GitRemoteAccess(repoUrl, false), branch, artifactRootPath, existingPaths, cancellationToken);

    public async Task CheckAsync(GitRemoteAccess access, string branch, string artifactRootPath,
        IReadOnlyList<string> existingPaths, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        var directory = Path.Combine(Path.GetTempPath(), "sddp-git-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await Run(["clone", "--bare", "--depth=1", "--single-branch", "--branch", branch, "--", access.Url, directory], timeout.Token, access);
            if (artifactRootPath.Length > 0)
            {
                var kind = await Run(["-C", directory, "cat-file", "-t", $"HEAD:{artifactRootPath}"], timeout.Token);
                if (kind.Trim() != "tree")
                    throw new SddpException("REPOSITORY_ROOT_INVALID", "Artifact root must identify a directory in the selected branch.");
            }
            if (existingPaths.Count > 0)
            {
                var output = await Run(["-C", directory, "ls-tree", "-r", "--name-only", "-z", "HEAD"], timeout.Token);
                var files = output.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
                var missing = existingPaths.Where(p => !files.Contains(p)).ToList();
                if (missing.Count > 0)
                    throw new SddpException("REPOSITORY_PATH_MISMATCH",
                        $"{missing.Count} existing file relationship path(s) are absent from this branch. Review them before confirmation: {string.Join(", ", missing.Take(5))}");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SddpException("REPOSITORY_CHECK_TIMEOUT", "Connection check timed out. Check the repository URL and server network access, then retry.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<string> Run(string[] arguments, CancellationToken ct, GitRemoteAccess? access = null)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        GitProcessCredentials.Configure(start, access);
        using var process = Process.Start(start) ?? throw new SddpException("REPOSITORY_UNAVAILABLE", "Git is unavailable on the server.");
        try
        {
            var output = ReadBounded(process.StandardOutput, ct);
            var errors = ReadBounded(process.StandardError, ct);
            await process.WaitForExitAsync(ct);
            var text = await output;
            await errors;
            if (process.ExitCode != 0)
                throw new SddpException("REPOSITORY_CHECK_FAILED", "Connection check failed. Verify the HTTPS URL, branch, artifact root and server Git credentials.");
            return text;
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken ct)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        var exceeded = false;
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            if (result.Length + count > 8 * 1024 * 1024) exceeded = true;
            if (!exceeded) result.Append(buffer, 0, count);
        }
        if (exceeded) throw new SddpException("REPOSITORY_CHECK_LIMIT", "Repository inspection exceeded the supported size.");
        return result.ToString();
    }
}
