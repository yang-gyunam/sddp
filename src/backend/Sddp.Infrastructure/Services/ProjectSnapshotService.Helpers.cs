using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces.Snapshots;

namespace Sddp.Infrastructure.Services;

public partial class ProjectSnapshotService
{
    private async Task<List<Dictionary<string, object?>>> QueryRowsAsync(
        string sql, object param, IDbTransaction transaction)
    {
        var rows = await _connection.QueryAsync<string>(
            $"SELECT row_to_json(snapshot_row)::text FROM ({sql}) snapshot_row", param, transaction).ConfigureAwait(false);
        return rows.Select(row => JsonSerializer.Deserialize<Dictionary<string, object?>>(row)!).ToList();
    }

    private async Task<List<Guid>> QueryIdsAsync(
        string sql, object param, IDbTransaction transaction)
    {
        var ids = await (_connection.QueryAsync<Guid>(sql, param, transaction)).ConfigureAwait(false);
        return ids.ToList();
    }

    private async Task ExecuteDelete(
        string table, string column, List<Guid> ids, IDbTransaction transaction)
    {
        if (ids.Count == 0) return;
        await (_connection.ExecuteAsync(
            $"DELETE FROM {table} WHERE {column} = ANY(@Ids)",
            new { Ids = ids.ToArray() }, transaction)).ConfigureAwait(false);
    }

    private List<Guid> GetIds(ProjectSnapshotData data, string table)
    {
        if (!data.Tables.TryGetValue(table, out var rows))
            return [];

        return rows
            .Where(r => r.ContainsKey("id") && r["id"] is not null)
            .Select(r =>
            {
                var val = r["id"];
                return val switch
                {
                    Guid g => g,
                    string s => Guid.Parse(s),
                    JsonElement je => Guid.Parse(je.GetString()!),
                    _ => Guid.Empty,
                };
            })
            .Where(g => g != Guid.Empty)
            .ToList();
    }

    private async Task InsertRowsAsync(
        string table, List<Dictionary<string, object?>> rows,
        string? nullifySelfRefCol, IDbTransaction transaction, bool legacyJson)
    {
        if (rows.Count == 0) return;
        var metadata = (await _connection.QueryAsync<(string Name, string Type)>(
            "SELECT column_name AS Name, udt_name AS Type FROM information_schema.columns WHERE table_schema=current_schema() AND table_name=@table",
            new { table }, transaction).ConfigureAwait(false)).ToDictionary(column => column.Name, column => column.Type);
        foreach (var original in rows)
        {
            var row = new Dictionary<string, object?>(original);
            foreach (var scopeColumn in new[] { "tenant_id", "project_id" })
                if (metadata.ContainsKey(scopeColumn) && !row.ContainsKey(scopeColumn))
                    throw new InvalidOperationException($"Missing snapshot scope: {table}.{scopeColumn}.");
            foreach (var column in row.Keys.ToArray())
            {
                if (!metadata.TryGetValue(column, out var type))
                    throw new InvalidOperationException($"Unknown snapshot column: {table}.{column}.");
                if (legacyJson && type is "json" or "jsonb")
                {
                    var text = row[column] is JsonElement { ValueKind: JsonValueKind.String } value
                        ? value.GetString() : row[column] as string;
                    if (text is not null) row[column] = JsonSerializer.Deserialize<JsonElement>(text);
                }
            }
            if (nullifySelfRefCol is not null && row.ContainsKey(nullifySelfRefCol)) row[nullifySelfRefCol] = null;
            var columns = string.Join(", ", row.Keys.Select(column => "\"" + column.Replace("\"", "\"\"") + "\""));
            await _connection.ExecuteAsync($"INSERT INTO {table} ({columns}) SELECT {columns} FROM jsonb_populate_recordset(NULL::{table}, @Json::jsonb)",
                new { Json = JsonSerializer.Serialize(new[] { row }) }, transaction).ConfigureAwait(false);
        }
    }

    private async Task UpdateSelfRefsAsync(
        string table, List<Dictionary<string, object?>> rows,
        string selfRefCol, IDbTransaction transaction)
    {
        await _connection.ExecuteAsync($"UPDATE {table} target SET {selfRefCol}=saved.{selfRefCol} FROM jsonb_populate_recordset(NULL::{table}, @Json::jsonb) saved WHERE target.id=saved.id",
            new { Json = JsonSerializer.Serialize(rows) }, transaction).ConfigureAwait(false);
    }

    internal static object? NormalizeValue(object? val)
    {
        if (val is null) return null;

        if (val is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => ParseString(je.GetString()),
                JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => je.GetRawText(),
                JsonValueKind.Object => je.GetRawText(),
                _ => je.GetRawText(),
            };
        }

        // String values from deserialization may also be UUIDs or timestamps
        if (val is string s)
            return ParseString(s);

        return val;
    }

    internal static object? ParseString(string? s)
    {
        if (s is null) return null;

        // UUID detection (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
        if (s.Length == 36 && Guid.TryParse(s, out var guid))
            return guid;

        // ISO 8601 timestamp detection
        if (s.Length >= 19 && DateTimeOffset.TryParse(s, out var dto))
            return dto;

        return s;
    }

    private async Task<ProjectSnapshotDto> GetSnapshotByIdAsync(Guid snapshotId)
    {
        const string sql = @"
            SELECT
                s.id, s.project_id, s.name, s.description,
                s.snapshot_type, s.status, s.table_counts,
                s.data_size_bytes, s.created_by,
                COALESCE(p.display_name, u.username, 'Unknown') AS created_by_name,
                s.created_at
            FROM project_snapshots s
            LEFT JOIN users u ON u.id = s.created_by
            LEFT JOIN persons p ON p.id = u.person_id
            WHERE s.id = @Id";

        var row = await (_connection.QuerySingleAsync(sql, new { Id = snapshotId })).ConfigureAwait(false);
        return MapToDto((object)row);
    }

    internal static ProjectSnapshotDto MapToDto(dynamic row)
    {
        var tableCountsStr = row.table_counts?.ToString() ?? "{}";
        var tableCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(tableCountsStr) ?? new Dictionary<string, int>();

        return new ProjectSnapshotDto(
            Id: row.id.ToString(),
            ProjectId: row.project_id.ToString(),
            Name: row.name,
            Description: row.description,
            SnapshotType: row.snapshot_type,
            Status: row.status,
            TableCounts: tableCounts,
            DataSizeBytes: row.data_size_bytes,
            CreatedBy: new UserRefDto(row.created_by.ToString(), row.created_by_name, null),
            CreatedAt: ((DateTimeOffset)row.created_at).ToString("O"));
    }
}
