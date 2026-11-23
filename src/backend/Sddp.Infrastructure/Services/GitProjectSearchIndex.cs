using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services;

/// <summary>The search cache only consumes blobs from the same authorized commit as the Files viewer.</summary>
public sealed class GitProjectSearchIndex(GitProjectFileRepository files, IDbConnection connection,
    IOptions<GitRepositoryOptions> options) : IProjectSearchFileIndex
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public async Task<string> PrepareAsync(GlobalUniqueId tenant, GlobalUniqueId project, CancellationToken ct)
    {
        var (context, commit) = await files.OpenAsync(tenant, project, null, ct);
        var maxBytes = Math.Clamp(options.Value.MaxArtifactContentBytes, 1, 8 * 1024 * 1024);
        var scope = new { Tenant = tenant.ToGuid(), Project = project.ToGuid(), Commit = commit, MaxBytes = maxBytes };
        const string head = "SELECT revision FROM project_search_file_heads WHERE tenant_id=@Tenant AND project_id=@Project AND max_bytes=@MaxBytes";
        var db = (DbConnection)connection;
        if (db.State != ConnectionState.Open) await db.OpenAsync(ct);
        if (await db.QuerySingleOrDefaultAsync<string>(new(head, scope, cancellationToken: ct)) == commit) return commit;
        await using var tx = await db.BeginTransactionAsync(ct);
        CommandDefinition Command(string sql, object? args = null) => new(sql, args ?? scope, tx, cancellationToken: ct);
        await db.ExecuteAsync(Command("SELECT pg_advisory_xact_lock(hashtextextended(@Project::text,27))"));
        var previousCommit = await db.QuerySingleOrDefaultAsync<string>(Command(head));
        if (previousCommit == commit) return commit;

        var entries = await GitProjectFileRepository.ListFilesAsync(context, commit, ct);
        var oldPaths = await db.QueryAsync<string>(Command(
            "SELECT path FROM project_search_files WHERE tenant_id=@Tenant AND project_id=@Project"));
        var dates = await ReadDates(context.RepoPath, commit, previousCommit, entries.Select(e => e.Name), ct);
        var changed = entries.Where(e => dates.ContainsKey(e.Name));
        using var batch = new Process
        {
            StartInfo = new("git")
            {
                WorkingDirectory = context.RepoPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        batch.StartInfo.ArgumentList.Add("cat-file");
        batch.StartInfo.ArgumentList.Add("--batch");
        batch.Start();
        try
        {
            foreach (var group in changed.Chunk(32))
            {
                var contents = new string[group.Length];
                for (var i = 0; i < group.Length; i++)
                {
                    var e = group[i];
                    contents[i] = e.Kind is "file" or "symlink" && e.Size <= maxBytes
                        ? await ReadBlob(batch, e, ct) : "";
                }
                await db.ExecuteAsync(Command("""
                    INSERT INTO project_search_files(tenant_id,project_id,path,blob,content,updated_at)
                    SELECT @Tenant,@Project,t.path,t.blob,t.content,t.modified
                    FROM unnest(@Paths::text[],@Blobs::text[],@Contents::text[],@Dates::timestamptz[]) AS t(path,blob,content,modified)
                    ON CONFLICT (tenant_id,project_id,path) DO UPDATE SET blob=excluded.blob,
                        content=excluded.content,updated_at=excluded.updated_at
                    """, new
                {
                    scope.Tenant,
                    scope.Project,
                    Paths = group.Select(e => e.Name).ToArray(),
                    Blobs = group.Select(e => e.ObjectId).ToArray(),
                    Contents = contents,
                    Dates = group.Select(e => dates[e.Name].UtcDateTime).ToArray()
                }));
            }
            batch.StandardInput.Close();
            await batch.WaitForExitAsync(ct);
            if (batch.ExitCode != 0) throw new IOException("Git could not read the committed files.");
        }
        finally { if (!batch.HasExited) batch.Kill(entireProcessTree: true); }
        var removed = oldPaths.Except(entries.Select(e => e.Name), StringComparer.Ordinal).ToArray();
        await db.ExecuteAsync(Command("DELETE FROM project_search_files WHERE tenant_id=@Tenant AND project_id=@Project AND path=ANY(@Removed)",
            new { scope.Tenant, scope.Project, Removed = removed }));
        await db.ExecuteAsync(Command("""
            INSERT INTO project_search_file_heads(tenant_id,project_id,revision,max_bytes) VALUES (@Tenant,@Project,@Commit,@MaxBytes)
            ON CONFLICT (tenant_id,project_id) DO UPDATE SET revision=excluded.revision,max_bytes=excluded.max_bytes
            """));
        await tx.CommitAsync(ct);
        return commit;
    }

    // Stop at the indexed first-parent commit. Following the current history also handles rewritten branches.
    // A revert or mode-only change still updates the file date even when its blob is unchanged.
    internal static async Task<Dictionary<string, DateTimeOffset>> ReadDates(string directory, string commit,
        string? previousCommit, IEnumerable<string> paths, CancellationToken ct)
    {
        var pending = paths.ToHashSet(StringComparer.Ordinal);
        var dates = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        if (pending.Count == 0) return dates;
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Utf8,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "log", "--first-parent", "--diff-merges=first-parent", "--format=%H %ct", "--raw", "-z", "--no-renames", commit })
            start.ArgumentList.Add(arg);
        using var log = Process.Start(start) ?? throw new IOException("Cannot read Git history.");
        try
        {
            var date = DateTimeOffset.UnixEpoch;
            var expectPath = false;
            await foreach (var token in GitProjectFileRepository.ReadNulRecords(log.StandardOutput, ct))
            {
                if (expectPath)
                {
                    if (pending.Remove(token)) dates[token] = date;
                    if (pending.Count == 0) break;
                    expectPath = false;
                }
                else if (token.TrimStart('\n').StartsWith(':')) expectPath = true;
                else if (!string.IsNullOrWhiteSpace(token))
                {
                    var header = token.Trim().Split(' ', 2);
                    if (header.Length != 2 || !long.TryParse(header[1], CultureInfo.InvariantCulture, out var epoch))
                        throw new IOException("Unexpected Git history record.");
                    if (header[0] == previousCommit) return dates;
                    date = DateTimeOffset.FromUnixTimeSeconds(epoch);
                }
            }
            if (pending.Count != 0) throw new IOException("Git history did not include every committed path.");
            return dates;
        }
        finally { if (!log.HasExited) log.Kill(entireProcessTree: true); }
    }

    private static async Task<string> ReadBlob(Process process, GitProjectFileRepository.TreeEntry entry, CancellationToken ct)
    {
        await process.StandardInput.WriteLineAsync(entry.ObjectId.AsMemory(), ct);
        await process.StandardInput.FlushAsync(ct);
        var output = process.StandardOutput.BaseStream;
        // git --batch promises this exact header for an existing blob named by its full object ID.
        var header = Encoding.ASCII.GetBytes(FormattableString.Invariant($"{entry.ObjectId} blob {entry.Size}\n"));
        var actual = new byte[header.Length];
        await output.ReadExactlyAsync(actual, ct);
        if (!actual.AsSpan().SequenceEqual(header)) throw new IOException("Unexpected Git blob header.");
        var bytes = new byte[checked((int)entry.Size!.Value + 1)];
        await output.ReadExactlyAsync(bytes, ct);
        if (bytes[^1] != '\n') throw new IOException("Incomplete Git blob.");
        if (bytes.Contains((byte)0)) return "";
        try { return Utf8.GetString(bytes, 0, bytes.Length - 1); }
        catch (DecoderFallbackException) { return ""; }
    }
}
