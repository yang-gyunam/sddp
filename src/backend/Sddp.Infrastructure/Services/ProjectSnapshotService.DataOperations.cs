using System.Data;
using System.Text.Json;
using Dapper;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces.Snapshots;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services;

public partial class ProjectSnapshotService
{
    // ========================================================================
    // Extract (SELECT all project data)
    // ========================================================================

    private async Task<ProjectSnapshotData> ExtractProjectDataAsync(
        GlobalUniqueId tenantId, GlobalUniqueId projectId)
    {
        var data = new ProjectSnapshotData();
        var tid = (Guid)tenantId;
        var pid = (Guid)projectId;

        // Project record itself (name, description, status, etc.)
        var projectRows = await (QueryRowsAsync(
            "SELECT * FROM projects WHERE id = @ProjectId AND tenant_id = @TenantId",
            new { ProjectId = pid, TenantId = tid })).ConfigureAwait(false);
        if (projectRows.Count > 0) data.Tables["projects"] = projectRows;

        // Direct project_id tables
        foreach (var table in DirectProjectTables)
        {
            string sql;
            if (table == "conversations")
            {
                // Only project-scoped conversations
                sql = $"SELECT * FROM {table} WHERE project_id = @ProjectId AND is_active = TRUE";
            }
            else if (table == "embeddings")
            {
                sql = $"SELECT id, tenant_id, project_id, source_type, source_id, chunk_index, chunk_text, conversation_id, spec_version, message_type, created_at, updated_at, is_active FROM {table} WHERE project_id = @ProjectId AND is_active = TRUE";
            }
            else
            {
                sql = $"SELECT * FROM {table} WHERE project_id = @ProjectId";
            }

            var rows = await (QueryRowsAsync(sql, new { ProjectId = pid })).ConfigureAwait(false);
            if (rows.Count > 0) data.Tables[table] = rows;
        }

        // Get conversation IDs for child tables
        var conversationIds = GetIds(data, "conversations");
        if (conversationIds.Count > 0)
        {
            foreach (var table in ConversationChildTables)
            {
                var colName = table is "user_conversation_settings" or "user_read_statuses"
                    ? "conversation_id"
                    : "id";
                if (table is "channels" or "forums" or "direct_messages")
                    colName = "id";
                else
                    colName = "conversation_id";

                var sql = $"SELECT * FROM {table} WHERE {colName} = ANY(@Ids)";
                var rows = await (QueryRowsAsync(sql, new { Ids = conversationIds.ToArray() })).ConfigureAwait(false);
                if (rows.Count > 0) data.Tables[table] = rows;
            }

            // Messages: via conversation_id
            var msgSql = "SELECT * FROM messages WHERE conversation_id = ANY(@Ids)";
            var msgRows = await (QueryRowsAsync(msgSql, new { Ids = conversationIds.ToArray() })).ConfigureAwait(false);

            // Topics: via forum IDs
            var forumIds = GetIds(data, "forums");
            if (forumIds.Count > 0)
            {
                var topicSql = "SELECT * FROM topics WHERE forum_id = ANY(@Ids)";
                var topicRows = await (QueryRowsAsync(topicSql, new { Ids = forumIds.ToArray() })).ConfigureAwait(false);
                if (topicRows.Count > 0) data.Tables["topics"] = topicRows;

                // Messages: via topic_id
                var topicIds = topicRows
                    .Where(r => r.ContainsKey("id") && r["id"] is Guid)
                    .Select(r => (Guid)r["id"]!)
                    .ToList();
                if (topicIds.Count > 0)
                {
                    var topicMsgSql = "SELECT * FROM messages WHERE topic_id = ANY(@Ids)";
                    var topicMsgRows = await (QueryRowsAsync(topicMsgSql, new { Ids = topicIds.ToArray() })).ConfigureAwait(false);
                    msgRows.AddRange(topicMsgRows);
                }
            }

            if (msgRows.Count > 0) data.Tables["messages"] = msgRows;
        }

        // Spec child tables
        var specIds = GetIds(data, "specs");
        if (specIds.Count > 0)
        {
            foreach (var table in SpecChildTables)
            {
                var sql = $"SELECT * FROM {table} WHERE spec_id = ANY(@Ids)";
                var rows = await (QueryRowsAsync(sql, new { Ids = specIds.ToArray() })).ConfigureAwait(false);
                if (rows.Count > 0) data.Tables[table] = rows;
            }
        }

        // Entity metadata child tables
        var entityMetaIds = GetIds(data, "entity_metadata");
        if (entityMetaIds.Count > 0)
        {
            foreach (var table in EntityMetaChildTables)
            {
                var sql = $"SELECT * FROM {table} WHERE entity_metadata_id = ANY(@Ids)";
                var rows = await (QueryRowsAsync(sql, new { Ids = entityMetaIds.ToArray() })).ConfigureAwait(false);
                if (rows.Count > 0) data.Tables[table] = rows;
            }
        }

        // Task child tables
        var taskIds = GetIds(data, "tasks");
        if (taskIds.Count > 0)
        {
            foreach (var table in TaskChildTables)
            {
                var sql = $"SELECT * FROM {table} WHERE task_id = ANY(@Ids)";
                var rows = await (QueryRowsAsync(sql, new { Ids = taskIds.ToArray() })).ConfigureAwait(false);
                if (rows.Count > 0) data.Tables[table] = rows;
            }
        }

        // SLA notifications (via sign_offs)
        var signOffIds = GetIds(data, "sign_offs");
        if (signOffIds.Count > 0)
        {
            foreach (var table in SignOffChildTables)
            {
                var sql = $"SELECT * FROM {table} WHERE sign_off_id = ANY(@Ids)";
                var rows = await (QueryRowsAsync(sql, new { Ids = signOffIds.ToArray() })).ConfigureAwait(false);
                if (rows.Count > 0) data.Tables[table] = rows;
            }
        }

        return data;
    }

    // ========================================================================
    // Delete (children-first)
    // ========================================================================

    private async Task DeleteProjectDataAsync(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, IDbTransaction transaction)
    {
        var pid = (Guid)projectId;

        // First, gather IDs for child deletion
        var conversationIds = await (QueryIdsAsync("SELECT id FROM conversations WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var forumIds = conversationIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM forums WHERE id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];
        var topicIds = forumIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM topics WHERE forum_id = ANY(@Ids)", new { Ids = forumIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];
        var specIds = await (QueryIdsAsync("SELECT id FROM specs WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var entityMetaIds = await (QueryIdsAsync("SELECT id FROM entity_metadata WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var taskIds = await (QueryIdsAsync("SELECT id FROM tasks WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var signOffIds = specIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM sign_offs WHERE spec_id = ANY(@Ids)", new { Ids = specIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];

        // Delete in children-first order

        // Level 0: deepest leaves
        if (signOffIds.Count > 0)
            await (ExecuteDelete("sla_notifications", "sign_off_id", signOffIds, transaction)).ConfigureAwait(false);
        if (taskIds.Count > 0)
        {
            await (ExecuteDelete("task_time_logs", "task_id", taskIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("task_acceptance_criteria", "task_id", taskIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("task_linked_items", "task_id", taskIds, transaction)).ConfigureAwait(false);
        }
        if (entityMetaIds.Count > 0)
        {
            await (ExecuteDelete("entity_relationship_metadata", "entity_metadata_id", entityMetaIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("field_metadata", "entity_metadata_id", entityMetaIds, transaction)).ConfigureAwait(false);
        }
        if (specIds.Count > 0)
        {
            await (ExecuteDelete("artifact_to_spec_mappings", "spec_id", specIds, transaction)).ConfigureAwait(false);
        }

        // Level 1: messages (depend on conversations and topics)
        if (conversationIds.Count > 0 || topicIds.Count > 0)
        {
            // Clear self-ref first
            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);

            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);
        }

        // Level 2: topics, conversation_members, user settings
        if (forumIds.Count > 0)
            await (ExecuteDelete("topics", "forum_id", forumIds, transaction)).ConfigureAwait(false);
        if (conversationIds.Count > 0)
        {
            await (ExecuteDelete("conversation_members", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_conversation_settings", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_read_statuses", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
        }

        // Level 3: sign_offs, alternatives
        if (specIds.Count > 0)
        {
            await (ExecuteDelete("sign_offs", "spec_id", specIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("alternatives", "spec_id", specIds, transaction)).ConfigureAwait(false);
        }

        // Level 4: TPT children (channels, forums, direct_messages)
        if (conversationIds.Count > 0)
        {
            await (ExecuteDelete("channels", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("forums", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("direct_messages", "id", conversationIds, transaction)).ConfigureAwait(false);
        }

        // Level 5: Clear self-references, then delete root tables
        await (_connection.ExecuteAsync("UPDATE specs SET supersedes_spec_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        await (_connection.ExecuteAsync("UPDATE requirements SET parent_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        await (_connection.ExecuteAsync("UPDATE glossary_terms SET replaced_by_term_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);

        // Level 6: Direct project_id tables (reverse order from insert)
        var deleteOrder = new[]
        {
            "embeddings", "sla_policies", "ai_reports", "worklogs", "working_days",
            "effort_allocations", "tasks", "artifact_trackings", "entity_metadata",
            "relationships", "glossary_terms", "requirements", "specs",
            "conversations", "project_members",
        };

        foreach (var table in deleteOrder)
        {
            await (_connection.ExecuteAsync(
                $"DELETE FROM {table} WHERE project_id = @Pid",
                new { Pid = pid }, transaction)).ConfigureAwait(false);
        }
    }

    // ========================================================================
    // Restore (parents-first, 2-pass for self-refs)
    // ========================================================================

    private async Task RestoreProjectDataAsync(
        ProjectSnapshotData data, IDbTransaction transaction)
    {
        // Restore the project record itself (UPDATE, not DELETE/INSERT — it's the FK root)
        if (data.Tables.TryGetValue("projects", out var projectRows) && projectRows.Count > 0)
        {
            var projectRow = projectRows[0];
            var projectId = NormalizeValue(projectRow["id"]);

            var setClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("@ProjectId", projectId);

            // Restorable columns (exclude id, tenant_id, created_at which shouldn't change)
            var restorableColumns = new[] { "name", "description", "status", "repo_url", "repo_branch", "artifact_root_path", "sync_interval_minutes", "last_synced_at" };
            var idx = 0;
            foreach (var col in restorableColumns)
            {
                if (!projectRow.ContainsKey(col)) continue;
                var paramName = $"@pProj{idx}";
                setClauses.Add($"{col} = {paramName}");
                parameters.Add(paramName, NormalizeValue(projectRow[col]));
                idx++;
            }

            if (setClauses.Count > 0)
            {
                var sql = $"UPDATE projects SET {string.Join(", ", setClauses)}, updated_at = NOW() WHERE id = @ProjectId";
                await (_connection.ExecuteAsync(sql, parameters, transaction)).ConfigureAwait(false);
            }
        }

        // INSERT order: parents first
        var insertOrder = new[]
        {
            // Root tables with direct project_id
            "project_members",
            "conversations",
            "specs",
            "requirements",
            "glossary_terms",
            "relationships",
            "entity_metadata",
            "field_metadata", "entity_relationship_metadata",
            "artifact_trackings",
            "tasks",
            "effort_allocations", "working_days", "worklogs",
            "ai_reports",
            "sla_policies",
            "embeddings",
            "channels", "forums", "direct_messages",      // TPT children; channels may reference specs
            // Child tables
            "conversation_members",
            "user_conversation_settings", "user_read_statuses",
            "sign_offs", "alternatives", "artifact_to_spec_mappings",
            "topics",
            "messages",
            "task_acceptance_criteria", "task_linked_items", "task_time_logs",
            "sla_notifications",
        };

        foreach (var table in insertOrder)
        {
            if (!data.Tables.TryGetValue(table, out var rows) || rows.Count == 0)
                continue;

            if (SelfRefColumns.TryGetValue(table, out var selfRefCol))
            {
                // 2-pass: first insert with self-ref = NULL, then update
                await (InsertRowsAsync(table, rows, selfRefCol, transaction)).ConfigureAwait(false);
                await (UpdateSelfRefsAsync(table, rows, selfRefCol, transaction)).ConfigureAwait(false);
            }
            else
            {
                await (InsertRowsAsync(table, rows, null, transaction)).ConfigureAwait(false);
            }
        }
    }

    // ========================================================================
    // Reset (public API — wraps DeleteProjectDataAsync with row count tracking)
    // ========================================================================

    public async Task<Dictionary<string, int>> ResetProjectDataAsync(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var transaction = _connection.BeginTransaction();

        try
        {
            // Count rows before deletion for reporting
            var pid = (Guid)projectId;
            var tableCounts = new Dictionary<string, int>();

            var tablesToCount = new[]
            {
                "specs", "requirements", "conversations", "tasks",
                "glossary_terms", "relationships", "artifact_trackings",
                "entity_metadata", "effort_allocations"
            };

            foreach (var table in tablesToCount)
            {
                var count = await (_connection.ExecuteScalarAsync<int>(
                    $"SELECT COUNT(*) FROM {table} WHERE project_id = @Pid",
                    new { Pid = pid }, transaction)).ConfigureAwait(false);
                if (count > 0)
                    tableCounts[table] = count;
            }

            // Execute cascade delete (preserves project_members)
            await (DeleteProjectDataForResetAsync(tenantId, projectId, transaction)).ConfigureAwait(false);

            transaction.Commit();
            return tableCounts;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// delete — project_members, project_snapshots
    /// DeleteProjectDataAsync Level 6 project_members
    /// </summary>
    private async Task DeleteProjectDataForResetAsync(
        GlobalUniqueId tenantId, GlobalUniqueId projectId, IDbTransaction transaction)
    {
        var pid = (Guid)projectId;

        // Gather IDs for child deletion ()
        var conversationIds = await (QueryIdsAsync("SELECT id FROM conversations WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var forumIds = conversationIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM forums WHERE id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];
        var topicIds = forumIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM topics WHERE forum_id = ANY(@Ids)", new { Ids = forumIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];
        var specIds = await (QueryIdsAsync("SELECT id FROM specs WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var entityMetaIds = await (QueryIdsAsync("SELECT id FROM entity_metadata WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var taskIds = await (QueryIdsAsync("SELECT id FROM tasks WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        var signOffIds = specIds.Count > 0
            ? await (QueryIdsAsync("SELECT id FROM sign_offs WHERE spec_id = ANY(@Ids)", new { Ids = specIds.ToArray() }, transaction))
.ConfigureAwait(false)            : [];

        // Level 0–5: children-first delete (DeleteProjectDataAsync)
        if (signOffIds.Count > 0)
            await (ExecuteDelete("sla_notifications", "sign_off_id", signOffIds, transaction)).ConfigureAwait(false);
        if (taskIds.Count > 0)
        {
            await (ExecuteDelete("task_time_logs", "task_id", taskIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("task_acceptance_criteria", "task_id", taskIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("task_linked_items", "task_id", taskIds, transaction)).ConfigureAwait(false);
        }
        if (entityMetaIds.Count > 0)
        {
            await (ExecuteDelete("entity_relationship_metadata", "entity_metadata_id", entityMetaIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("field_metadata", "entity_metadata_id", entityMetaIds, transaction)).ConfigureAwait(false);
        }
        if (specIds.Count > 0)
            await (ExecuteDelete("artifact_to_spec_mappings", "spec_id", specIds, transaction)).ConfigureAwait(false);

        if (conversationIds.Count > 0 || topicIds.Count > 0)
        {
            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);
        }

        if (forumIds.Count > 0)
            await (ExecuteDelete("topics", "forum_id", forumIds, transaction)).ConfigureAwait(false);
        if (conversationIds.Count > 0)
        {
            await (ExecuteDelete("conversation_members", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_conversation_settings", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_read_statuses", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
        }

        if (specIds.Count > 0)
        {
            await (ExecuteDelete("sign_offs", "spec_id", specIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("alternatives", "spec_id", specIds, transaction)).ConfigureAwait(false);
        }

        if (conversationIds.Count > 0)
        {
            await (ExecuteDelete("channels", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("forums", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("direct_messages", "id", conversationIds, transaction)).ConfigureAwait(false);
        }

        await (_connection.ExecuteAsync("UPDATE specs SET supersedes_spec_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        await (_connection.ExecuteAsync("UPDATE requirements SET parent_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);
        await (_connection.ExecuteAsync("UPDATE glossary_terms SET replaced_by_term_id = NULL WHERE project_id = @Pid", new { Pid = pid }, transaction)).ConfigureAwait(false);

        // Level 6: project_members ()
        var resetDeleteOrder = new[]
        {
            "embeddings", "sla_policies", "ai_reports", "worklogs", "working_days",
            "effort_allocations", "tasks", "artifact_trackings", "entity_metadata",
            "relationships", "glossary_terms", "requirements", "specs",
            "conversations",
        };

        foreach (var table in resetDeleteOrder)
        {
            await (_connection.ExecuteAsync(
                $"DELETE FROM {table} WHERE project_id = @Pid",
                new { Pid = pid }, transaction)).ConfigureAwait(false);
        }
    }

    // ========================================================================
    // Delete TenantWide conversations (project_id IS NULL)
    // ========================================================================

    public async Task<int> DeleteTenantWideConversationsAsync(
        GlobalUniqueId tenantId,
        CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        using var transaction = _connection.BeginTransaction();

        try
        {
            var tid = (Guid)tenantId;

            // TenantWide conversation ID get (project_id IS NULL)
            var conversationIds = await (QueryIdsAsync(
                "SELECT id FROM conversations WHERE tenant_id = @Tid AND project_id IS NULL",
                new { Tid = tid }, transaction)).ConfigureAwait(false);

            if (conversationIds.Count == 0)
            {
                transaction.Commit();
                return 0;
            }

            var forumIds = await (QueryIdsAsync(
                "SELECT id FROM forums WHERE id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            var topicIds = forumIds.Count > 0
                ? await (QueryIdsAsync("SELECT id FROM topics WHERE forum_id = ANY(@Ids)", new { Ids = forumIds.ToArray() }, transaction))
.ConfigureAwait(false)                : [];

            // Messages (reply self-ref → delete)
            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("UPDATE messages SET reply_to_id = NULL WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (conversationIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE conversation_id = ANY(@Ids)", new { Ids = conversationIds.ToArray() }, transaction)).ConfigureAwait(false);
            if (topicIds.Count > 0)
                await (_connection.ExecuteAsync("DELETE FROM messages WHERE topic_id = ANY(@Ids)", new { Ids = topicIds.ToArray() }, transaction)).ConfigureAwait(false);

            // Topics, members, settings
            if (forumIds.Count > 0)
                await (ExecuteDelete("topics", "forum_id", forumIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("conversation_members", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_conversation_settings", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("user_read_statuses", "conversation_id", conversationIds, transaction)).ConfigureAwait(false);

            // TPT children
            await (ExecuteDelete("channels", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("forums", "id", conversationIds, transaction)).ConfigureAwait(false);
            await (ExecuteDelete("direct_messages", "id", conversationIds, transaction)).ConfigureAwait(false);

            // Base conversations
            await (_connection.ExecuteAsync(
                "DELETE FROM conversations WHERE tenant_id = @Tid AND project_id IS NULL",
                new { Tid = tid }, transaction)).ConfigureAwait(false);

            transaction.Commit();
            return conversationIds.Count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
