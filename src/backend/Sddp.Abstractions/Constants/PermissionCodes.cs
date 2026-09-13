namespace Sddp.Abstractions.Constants;

/// <summary>
/// Stable permission codes used by authorization policies and seed data.
/// </summary>
public static class PermissionCodes
{
    public const string SpecCreate = "spec:create";
    public const string SpecRead = "spec:read";
    public const string SpecUpdate = "spec:update";
    public const string SpecDelete = "spec:delete";
    public const string SpecApprove = "spec:approve";
    public const string SpecLock = "spec:lock";

    public const string ConversationCreate = "conversation:create";
    public const string ConversationRead = "conversation:read";
    public const string ConversationPost = "conversation:post";
    public const string ConversationClose = "conversation:close";

    public const string RequirementCreate = "requirement:create";
    public const string RequirementRead = "requirement:read";
    public const string RequirementUpdate = "requirement:update";
    public const string RequirementDelete = "requirement:delete";

    public const string GenerationExecute = "generation:execute";
    public const string GenerationRollback = "generation:rollback";

    public const string GlossaryCreate = "glossary:create";
    public const string GlossaryRead = "glossary:read";
    public const string GlossaryUpdate = "glossary:update";
    public const string GlossaryDeprecate = "glossary:deprecate";

    public const string UserManage = "user:manage";
    public const string RoleAssign = "role:assign";
    public const string AuditRead = "audit:read";

    public const string ProjectCreate = "project:create";
    public const string ProjectRead = "project:read";
    public const string ProjectUpdate = "project:update";
    public const string ProjectDelete = "project:delete";

    public const string TaskCreate = "task:create";
    public const string TaskRead = "task:read";
    public const string TaskUpdate = "task:update";
    public const string TaskDelete = "task:delete";

    public const string KnowledgeRead = "knowledge:read";
    public const string KnowledgeCreate = "knowledge:create";
    public const string KnowledgeUpdate = "knowledge:update";
    public const string KnowledgeArchive = "knowledge:archive";
    public const string KnowledgePublish = "knowledge:publish";
    public const string KnowledgeAdmin = "knowledge:admin";

    public static IReadOnlyList<string> All { get; } =
    [
        SpecCreate, SpecRead, SpecUpdate, SpecDelete, SpecApprove, SpecLock,
        ConversationCreate, ConversationRead, ConversationPost, ConversationClose,
        RequirementCreate, RequirementRead, RequirementUpdate, RequirementDelete,
        GenerationExecute, GenerationRollback,
        GlossaryCreate, GlossaryRead, GlossaryUpdate, GlossaryDeprecate,
        UserManage, RoleAssign, AuditRead,
        ProjectCreate, ProjectRead, ProjectUpdate, ProjectDelete,
        TaskCreate, TaskRead, TaskUpdate, TaskDelete,
        KnowledgeRead, KnowledgeCreate, KnowledgeUpdate,
        KnowledgeArchive, KnowledgePublish, KnowledgeAdmin
    ];
}
