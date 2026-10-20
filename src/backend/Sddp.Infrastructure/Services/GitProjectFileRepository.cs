using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services;

/// <summary>Reads Git objects, never working-tree files or symlink targets.</summary>
public sealed class GitProjectFileRepository(IArtifactRepositoryService repositories, IOptions<GitRepositoryOptions> options)
    : IProjectFileRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SyncLocks = new();
    private static readonly Regex Sha = new("\\A(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})\\z", RegexOptions.CultureInvariant);
    private const int PageSize = 30;
    private const int OutputLimit = 8 * 1024 * 1024;

    public async Task<ProjectFileSearchDto> SearchAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string query, string? revision, int skip, CancellationToken ct)
    {
        query = query.Trim();
        if (query.Length is < 1 or > 256 || query.Contains('\0') || skip < 0)
            throw Invalid("Use a search query of 1–256 characters and a non-negative offset.");
        var (context, commit) = await OpenAsync(tenantId, projectId, revision, ct).ConfigureAwait(false);
        // Read every committed path, independently of folders loaded by the client. Query text is never a Git argument.
        var output = await GitAsync(context, ["ls-tree", "-r", "-l", "-z", "--full-tree", commit], ct).ConfigureAwait(false);
        var matches = ParseTree(output).Where(e => e.Kind != "directory" && e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
        const int searchPageSize = 100;
        var entries = matches.Skip(skip).Take(searchPageSize)
            .Select(e => new ProjectFileEntryDto(e.Name[(e.Name.LastIndexOf('/') + 1)..], e.Name, e.Kind, e.Size)).ToList();
        return new(context.RepoBranch, commit, entries, matches.Count,
            skip < matches.Count - entries.Count ? skip + entries.Count : null);
    }

    public async Task<ProjectFileTreeDto> GetTreeAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, CancellationToken ct)
    {
        ValidatePath(path, allowRoot: true);
        var (context, commit) = await OpenAsync(tenantId, projectId, revision, ct).ConfigureAwait(false);
        var tree = path.Length == 0 ? commit : $"{commit}:{path}";
        var result = await GitAsync(context, ["ls-tree", "-l", "-z", tree], ct).ConfigureAwait(false);
        var entries = ParseTree(result).Select(e => new ProjectFileEntryDto(e.Name,
            path.Length == 0 ? e.Name : $"{path}/{e.Name}", e.Kind, e.Size)).OrderBy(e => e.Kind != "directory")
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return new(context.RepoBranch, commit, path, entries);
    }

    public async Task<ProjectFileContentDto> GetContentAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, CancellationToken ct)
    {
        ValidatePath(path);
        var (context, commit) = await OpenAsync(tenantId, projectId, revision, ct).ConfigureAwait(false);
        var entry = await FindEntryAsync(context, commit, path, ct).ConfigureAwait(false);
        var maxBytes = Math.Clamp(options.Value.MaxArtifactContentBytes, 1, OutputLimit);
        var tooLarge = entry.Size > maxBytes;
        string? content = null;
        var binary = false;
        if (entry.Kind != "submodule" && !tooLarge)
        {
            var bytes = await GitBytesAsync(context, ["cat-file", "blob", entry.ObjectId], ct).ConfigureAwait(false);
            binary = bytes.Contains((byte)0);
            if (!binary)
            {
                try { content = new UTF8Encoding(false, true).GetString(bytes); }
                catch (DecoderFallbackException) { binary = true; }
            }
        }
        var history = await ReadHistoryAsync(context, commit, path, 0, 1, ct).ConfigureAwait(false);
        return new(context.RepoBranch, commit, path, entry.Kind, entry.Size ?? 0, content, binary, tooLarge, maxBytes, history.FirstOrDefault());
    }

    public async Task<ProjectFileHistoryDto> GetHistoryAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string path, string? revision, int skip, CancellationToken ct)
    {
        ValidatePath(path);
        if (skip is < 0 or > 2000) throw Invalid("Invalid history offset (0–2000).");
        var (context, commit) = await OpenAsync(tenantId, projectId, revision, ct).ConfigureAwait(false);
        await FindEntryAsync(context, commit, path, ct).ConfigureAwait(false);
        var history = await ReadHistoryAsync(context, commit, path, skip, PageSize + 1, ct).ConfigureAwait(false);
        return new(commit, history.Take(PageSize).ToList(), history.Count > PageSize && skip + PageSize <= 2000 ? skip + PageSize : null);
    }

    private async Task<(ArtifactRepositoryContext Context, string Commit)> OpenAsync(GlobalUniqueId tenantId, GlobalUniqueId projectId, string? revision, CancellationToken ct)
    {
        if (revision is not null && !Sha.IsMatch(revision)) throw Invalid("Revision must be a full commit SHA.");
        var gate = SyncLocks.GetOrAdd($"{tenantId}/{projectId}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        ArtifactRepositoryContext context;
        try
        {
            context = await repositories.GetProjectContextAsync(tenantId, projectId, ct).ConfigureAwait(false);
            await repositories.EnsureProjectSyncedAsync(context, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            throw new SddpException("REPOSITORY_UNAVAILABLE", "Repository unavailable. Check the project's Git settings and synchronization.");
        }
        finally { gate.Release(); }
        var tip = (await GitAsync(context, ["rev-parse", "--verify", $"refs/heads/{context.RepoBranch}^{{commit}}"], ct).ConfigureAwait(false)).Trim();
        if (revision is null) return (context, tip);
        var commit = (await GitAsync(context, ["rev-parse", "--verify", $"{revision}^{{commit}}"], ct).ConfigureAwait(false)).Trim();
        await GitAsync(context, ["merge-base", "--is-ancestor", commit, tip], ct).ConfigureAwait(false);
        return (context, commit);
    }

    private static async Task<TreeEntry> FindEntryAsync(ArtifactRepositoryContext context, string commit, string path, CancellationToken ct)
    {
        var result = await GitAsync(context, ["ls-tree", "-l", "-z", "--full-tree", commit, "--", path], ct).ConfigureAwait(false);
        var entry = ParseTree(result).FirstOrDefault(e => e.Name == path && e.Kind != "directory");
        return entry ?? throw new NotFoundException("File", path);
    }

    private static async Task<List<ProjectFileCommitDto>> ReadHistoryAsync(ArtifactRepositoryContext context, string commit, string path, int skip, int count, CancellationToken ct)
    {
        // Read from the tip to preserve rename paths even when paging older commits.
        var output = await GitAsync(context, ["log", "--follow", $"--max-count={skip + count}",
            "--format=%H%x00%an%x00%aI%x00%s", "--name-status", "-z", commit, "--", path], ct).ConfigureAwait(false);
        var tokens = output.Split('\0');
        var items = new List<ProjectFileCommitDto>();
        var historicalPath = path;
        for (var i = 0; i + 3 < tokens.Length;)
        {
            var sha = tokens[i++].TrimStart('\n');
            if (!Sha.IsMatch(sha)) break;
            var author = tokens[i++];
            var date = DateTimeOffset.Parse(tokens[i++], CultureInfo.InvariantCulture);
            var message = tokens[i++];
            items.Add(new(sha, author, date, message, historicalPath));
            while (i < tokens.Length)
            {
                var status = tokens[i].TrimStart('\n');
                if (Sha.IsMatch(status)) break;
                i++;
                if (status.Length == 0) continue;
                if (i >= tokens.Length) break;
                var oldPath = tokens[i++];
                if ((status[0] == 'R' || status[0] == 'C') && i < tokens.Length)
                {
                    var newPath = tokens[i++];
                    if (newPath == historicalPath) historicalPath = oldPath;
                }
            }
        }
        return items.Skip(skip).ToList();
    }

    private sealed record TreeEntry(string Name, string Kind, string ObjectId, long? Size);
    private static IEnumerable<TreeEntry> ParseTree(string text)
    {
        foreach (var record in text.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = record.IndexOf('\t');
            var fields = record[..tab].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var kind = fields[1] == "tree" ? "directory" : fields[1] == "commit" ? "submodule" : fields[0] == "120000" ? "symlink" : "file";
            yield return new(record[(tab + 1)..], kind, fields[2], long.TryParse(fields[3], out var size) ? size : null);
        }
    }

    public static void ValidatePath(string path, bool allowRoot = false)
    {
        if ((path.Length == 0 && !allowRoot) || path.Length > 4096 || path.Contains('\0') || path.Contains('\\')
            || path.StartsWith('/') || (path.Length > 0 && path.Split('/').Any(p => p is "" or "." or "..")))
            throw Invalid("Use a repository-relative file path.");
    }

    private static SddpException Invalid(string message) => new("INVALID_FILE_REQUEST", message);
    private static async Task<string> GitAsync(ArtifactRepositoryContext context, string[] args, CancellationToken ct)
        => Encoding.UTF8.GetString(await GitBytesAsync(context, args, ct).ConfigureAwait(false));

    private static async Task<byte[]> GitBytesAsync(ArtifactRepositoryContext context, string[] args, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var start = new ProcessStartInfo("git") { WorkingDirectory = context.RepoPath, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_LITERAL_PATHSPECS"] = "1";
        start.ArgumentList.Add("--no-pager");
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        process.Start();
        try
        {
            var stderr = process.StandardError.BaseStream.CopyToAsync(Stream.Null, timeout.Token);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await process.StandardOutput.BaseStream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
            {
                if (output.Length + read > OutputLimit) throw Invalid("Repository response is too large. Select a smaller directory or file.");
                output.Write(buffer, 0, read);
            }
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            await stderr.ConfigureAwait(false);
            if (process.ExitCode != 0) throw Invalid("The requested file or revision is unavailable in the configured branch.");
            return output.ToArray();
        }
        finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
    }
}
