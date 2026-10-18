using System.Data;
using System.Data.Common;
using System.Text.Json;
using Dapper;
using Sddp.Abstractions.Interfaces.Snapshots;

namespace Sddp.Infrastructure.Services;

public partial class ProjectSnapshotService
{
    private async Task ValidateSnapshotColumnsAsync(ProjectSnapshotData data, IDbTransaction transaction, CancellationToken ct)
    {
        foreach (var (table, rows) in data.Tables)
        {
            if (rows.Count == 0) continue;
            var columns = (await _connection.QueryAsync<string>(new CommandDefinition(
                "SELECT column_name FROM information_schema.columns WHERE table_schema=current_schema() AND table_name=@table",
                new { table }, transaction, cancellationToken: ct)).ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row.Keys.Any(column => !columns.Contains(column)))
                    throw new InvalidOperationException($"Unknown snapshot column in {table}.");
                foreach (var column in new[] { "tenant_id", "project_id" })
                    if (columns.Contains(column) && !row.ContainsKey(column))
                        throw new InvalidOperationException($"Missing snapshot scope: {table}.{column}.");
            }
        }
    }

    private async Task LockProjectAsync(Guid tenantId, Guid projectId, IDbTransaction transaction, CancellationToken ct)
    {
        try
        {
            // Module parents precede the project, matching member publication. Never wait on an inverted lock.
            foreach (var extension in _extensions)
                await extension.LockAsync(_connection, transaction, tenantId, projectId, ct).ConfigureAwait(false);
            var id = await _connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM projects WHERE tenant_id=@tenantId AND id=@projectId FOR UPDATE NOWAIT",
                new { tenantId, projectId }, transaction, cancellationToken: ct)).ConfigureAwait(false);
            if (id is null) throw new InvalidOperationException("Project not found in the requested tenant.");
            // Parent inserts committed between the first scan and project lock must also be locked.
            foreach (var extension in _extensions)
            {
                await extension.LockAsync(_connection, transaction, tenantId, projectId, ct).ConfigureAwait(false);
                await extension.EnsureIdleAsync(_connection, transaction, tenantId, projectId, ct).ConfigureAwait(false);
            }
        }
        catch (DbException error) when (error.SqlState == "55P03")
        {
            throw new InvalidOperationException("Project data is busy; retry the snapshot operation after current work finishes.", error);
        }
    }

    private static Guid SnapshotId(Dictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value)) throw new InvalidOperationException($"Missing snapshot field: {column}.");
        if (value is Guid id) return id;
        if (value is JsonElement { ValueKind: JsonValueKind.String } element && element.TryGetGuid(out id)) return id;
        if (value is string text && Guid.TryParse(text, out id)) return id;
        throw new InvalidOperationException($"Invalid snapshot identifier: {column}.");
    }

    private static bool HasSnapshotValue(Dictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) && value is not null &&
        value is not JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined };

    private static void ValidateSnapshotScope(ProjectSnapshotData data, Guid tenantId, Guid projectId)
    {
        if (data.FormatVersion is not (1 or 2) || data.Tables is null || data.Extensions is null)
            throw new InvalidOperationException("Unsupported snapshot format.");
        if (data.FormatVersion == 1 && data.Tables.GetValueOrDefault("embeddings", []).Any(row => !HasSnapshotValue(row, "embedding")))
            throw new InvalidOperationException("Legacy snapshot omitted required embedding vectors; use a complete backup or a new snapshot.");
        var allowed = DirectProjectTables.Concat(ConversationChildTables).Concat(SpecChildTables)
            .Concat(EntityMetaChildTables).Concat(TaskChildTables).Concat(SignOffChildTables)
            .Concat(new[] { "projects", "topics", "messages" }).ToHashSet(StringComparer.Ordinal);
        foreach (var (table, rows) in data.Tables)
        {
            if (!allowed.Contains(table) || rows is null) throw new InvalidOperationException($"Unknown snapshot table: {table}.");
            foreach (var row in rows)
            {
                if (row is null) throw new InvalidOperationException("Null snapshot row.");
                if (row.ContainsKey("tenant_id") && SnapshotId(row, "tenant_id") != tenantId)
                    throw new InvalidOperationException($"Snapshot tenant mismatch in {table}.");
                if ((DirectProjectTables.Contains(table) || row.ContainsKey("project_id")) && SnapshotId(row, "project_id") != projectId)
                    throw new InvalidOperationException($"Snapshot project mismatch in {table}.");
            }
        }
        if (!data.Tables.TryGetValue("projects", out var projects) || projects.Count != 1 ||
            SnapshotId(projects[0], "id") != projectId || SnapshotId(projects[0], "tenant_id") != tenantId)
            throw new InvalidOperationException("Snapshot must contain exactly the requested project.");

        HashSet<Guid> Ids(string table) => data.Tables.GetValueOrDefault(table, []).Select(row => SnapshotId(row, "id")).ToHashSet();
        void Children(IEnumerable<string> tables, string column, HashSet<Guid> parents)
        {
            foreach (var table in tables)
                foreach (var row in data.Tables.GetValueOrDefault(table, []))
                    if (!parents.Contains(SnapshotId(row, column)))
                        throw new InvalidOperationException($"Snapshot parent mismatch in {table}.{column}.");
        }
        var conversations = Ids("conversations");
        Children(new[] { "channels", "forums", "direct_messages" }, "id", conversations);
        Children(new[] { "conversation_members" }, "conversation_id", conversations);
        Children(new[] { "topics" }, "forum_id", Ids("forums"));
        var topics = Ids("topics");
        Children(new[] { "user_conversation_settings", "user_read_statuses" }, "conversation_id", conversations.Union(topics).ToHashSet());
        Children(SpecChildTables, "spec_id", Ids("specs"));
        Children(EntityMetaChildTables, "entity_metadata_id", Ids("entity_metadata"));
        Children(TaskChildTables, "task_id", Ids("tasks"));
        Children(SignOffChildTables, "sign_off_id", Ids("sign_offs"));
        foreach (var row in data.Tables.GetValueOrDefault("messages", []))
        {
            var conversation = HasSnapshotValue(row, "conversation_id");
            var topic = HasSnapshotValue(row, "topic_id");
            if (conversation == topic || (conversation && !conversations.Contains(SnapshotId(row, "conversation_id"))) ||
                (topic && !topics.Contains(SnapshotId(row, "topic_id"))))
                throw new InvalidOperationException("Snapshot message context mismatch.");
        }
        foreach (var (table, column) in SelfRefColumns)
        {
            var ids = Ids(table);
            foreach (var row in data.Tables.GetValueOrDefault(table, []))
                if (HasSnapshotValue(row, column) && !ids.Contains(SnapshotId(row, column)))
                    throw new InvalidOperationException($"Snapshot self reference mismatch in {table}.");
        }
    }
}
