using Sddp.Abstractions.Base;
using Sddp.Abstractions.Constants;

namespace Sddp.Domain.Entities;

/// <summary>
/// Permission entity
/// </summary>
public class Permission : EntityBase
{
    /// <summary>
    /// Permission code (for example: spec:create)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Permission name (display label)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Permission description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Resource (Spec, Conversation, Requirement, etc.)
    /// </summary>
    public string Resource { get; private set; } = string.Empty;

    /// <summary>
    /// Action (Create, Read, Update, Delete, Approve, Lock)
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this permission is part of the built-in authorization contract.
    /// </summary>
    public bool IsSystemPermission { get; private set; }

    /// <summary>
    /// Roles that include this permission
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    // Default constructor for EF Core
    private Permission() { }

    public Permission(
        string code,
        string name,
        string description,
        string resource,
        string action,
        bool isSystemPermission = false)
    {
        Code = code;
        Name = name;
        Description = description;
        Resource = resource;
        Action = action;
        IsSystemPermission = isSystemPermission;
    }

    public void MarkAsSystemPermission()
    {
        if (!IsSystemPermission)
        {
            IsSystemPermission = true;
            MarkAsModified();
        }
    }

    /// <summary>
    /// Creates the default permission set
    /// </summary>
    public static IEnumerable<Permission> CreateDefaultPermissions()
    {
        static Permission SystemPermission(
            string code,
            string name,
            string description,
            string resource,
            string action) => new(code, name, description, resource, action, isSystemPermission: true);

        // Spec permissions
        yield return SystemPermission(PermissionCodes.SpecCreate, "Create Spec", "Create a spec", "spec", "create");
        yield return SystemPermission(PermissionCodes.SpecRead, "Read Spec", "Read a spec", "spec", "read");
        yield return SystemPermission(PermissionCodes.SpecUpdate, "Update Spec", "Update a spec", "spec", "update");
        yield return SystemPermission(PermissionCodes.SpecDelete, "Delete Spec", "Delete a spec", "spec", "delete");
        yield return SystemPermission(PermissionCodes.SpecApprove, "Approve Spec", "Approve a spec", "spec", "approve");
        yield return SystemPermission(PermissionCodes.SpecLock, "Lock Spec", "Lock a spec", "spec", "lock");

        // Conversation permissions
        yield return SystemPermission(PermissionCodes.ConversationCreate, "Create Conversation", "Create a conversation", "conversation", "create");
        yield return SystemPermission(PermissionCodes.ConversationRead, "Read Conversation", "Read a conversation", "conversation", "read");
        yield return SystemPermission(PermissionCodes.ConversationPost, "Post to Conversation", "Post a conversation message", "conversation", "post");
        yield return SystemPermission(PermissionCodes.ConversationClose, "Close Conversation", "Close a conversation", "conversation", "close");

        // Requirement permissions
        yield return SystemPermission(PermissionCodes.RequirementCreate, "Create Requirement", "Create a requirement", "requirement", "create");
        yield return SystemPermission(PermissionCodes.RequirementRead, "Read Requirement", "Read a requirement", "requirement", "read");
        yield return SystemPermission(PermissionCodes.RequirementUpdate, "Update Requirement", "Update a requirement", "requirement", "update");
        yield return SystemPermission(PermissionCodes.RequirementDelete, "Delete Requirement", "Delete a requirement", "requirement", "delete");

        // Generation permissions
        yield return SystemPermission(PermissionCodes.GenerationExecute, "Execute Generation", "Run code generation", "generation", "execute");
        yield return SystemPermission(PermissionCodes.GenerationRollback, "Rollback Generation", "Roll back generated code", "generation", "rollback");

        // Glossary permissions
        yield return SystemPermission(PermissionCodes.GlossaryCreate, "Create Glossary", "Create a glossary term", "glossary", "create");
        yield return SystemPermission(PermissionCodes.GlossaryRead, "Read Glossary", "Read glossary terms", "glossary", "read");
        yield return SystemPermission(PermissionCodes.GlossaryUpdate, "Update Glossary", "Update a glossary term", "glossary", "update");
        yield return SystemPermission(PermissionCodes.GlossaryDeprecate, "Deprecate Glossary", "Deprecate a glossary term", "glossary", "deprecate");

        // User permissions
        yield return SystemPermission(PermissionCodes.UserManage, "Manage Users", "Manage users", "user", "manage");
        yield return SystemPermission(PermissionCodes.RoleAssign, "Assign Roles", "Assign roles", "role", "assign");

        // Audit permissions
        yield return SystemPermission(PermissionCodes.AuditRead, "Read Audit Logs", "Read audit logs", "audit", "read");

        // Project permissions
        yield return SystemPermission(PermissionCodes.ProjectCreate, "Create Project", "Create a project", "project", "create");
        yield return SystemPermission(PermissionCodes.ProjectRead, "Read Project", "Read a project", "project", "read");
        yield return SystemPermission(PermissionCodes.ProjectUpdate, "Update Project", "Update a project", "project", "update");
        yield return SystemPermission(PermissionCodes.ProjectDelete, "Delete Project", "Delete a project", "project", "delete");

        // Task permissions
        yield return SystemPermission(PermissionCodes.TaskCreate, "Create Task", "Create a task", "task", "create");
        yield return SystemPermission(PermissionCodes.TaskRead, "Read Task", "Read a task", "task", "read");
        yield return SystemPermission(PermissionCodes.TaskUpdate, "Update Task", "Update a task", "task", "update");
        yield return SystemPermission(PermissionCodes.TaskDelete, "Delete Task", "Delete a task", "task", "delete");

        // Knowledge permissions
        yield return SystemPermission(PermissionCodes.KnowledgeRead, "Read Knowledge", "Read knowledge pages", "knowledge", "read");
        yield return SystemPermission(PermissionCodes.KnowledgeCreate, "Create Knowledge", "Create knowledge pages", "knowledge", "create");
        yield return SystemPermission(PermissionCodes.KnowledgeUpdate, "Update Knowledge", "Update knowledge pages", "knowledge", "update");
        yield return SystemPermission(PermissionCodes.KnowledgeArchive, "Archive Knowledge", "Archive knowledge pages", "knowledge", "archive");
        yield return SystemPermission(PermissionCodes.KnowledgePublish, "Publish Knowledge", "Publish knowledge pages", "knowledge", "publish");
        yield return SystemPermission(PermissionCodes.KnowledgeAdmin, "Administer Knowledge", "Administer project knowledge", "knowledge", "admin");
    }
}
