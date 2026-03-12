namespace Sddp.Infrastructure.Services;

public partial class ProjectSnapshotService
{
    // Tables that have a direct project_id column (order: parents first for INSERT)
    private static readonly string[] DirectProjectTables =
    [
        // Level 0 — root
        "project_members",
        "conversations",
        "specs",
        "requirements",
        "glossary_terms",
        "relationships",
        "entity_metadata",
        "artifact_trackings",
        "tasks",
        "effort_allocations",
        "working_days",
        "worklogs",
        "ai_reports",
        "sla_policies",
        "embeddings",
    ];

    // Tables that reference conversations (no direct project_id)
    private static readonly string[] ConversationChildTables =
    [
        "channels",
        "forums",
        "direct_messages",
        "conversation_members",
        "user_conversation_settings",
        "user_read_statuses",
    ];

    // Tables that reference specs
    private static readonly string[] SpecChildTables =
    [
        "sign_offs",
        "alternatives",
        "artifact_to_spec_mappings",
    ];

    // Tables that reference entity_metadata
    private static readonly string[] EntityMetaChildTables =
    [
        "field_metadata",
        "entity_relationship_metadata",
    ];

    // Tables that reference conversations → forums → topics
    private static readonly string[] ForumChildTables =
    [
        "topics",
    ];

    // Tables that reference tasks
    private static readonly string[] TaskChildTables =
    [
        "task_acceptance_criteria",
        "task_linked_items",
        "task_time_logs",
    ];

    // sign_offs children
    private static readonly string[] SignOffChildTables =
    [
        "sla_notifications",
    ];

    // Self-referencing columns (table -> self-ref column)
    private static readonly Dictionary<string, string> SelfRefColumns = new()
    {
        ["specs"] = "supersedes_spec_id",
        ["requirements"] = "parent_id",
        ["glossary_terms"] = "replaced_by_term_id",
        ["messages"] = "reply_to_id",
    };
}
