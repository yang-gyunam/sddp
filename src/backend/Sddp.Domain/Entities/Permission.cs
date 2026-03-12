using Sddp.Abstractions.Base;

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
    /// Roles that include this permission
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    // Default constructor for EF Core
    private Permission() { }

    public Permission(string code, string name, string description, string resource, string action)
    {
        Code = code;
        Name = name;
        Description = description;
        Resource = resource;
        Action = action;
    }

    /// <summary>
    /// Creates the default permission set
    /// </summary>
    public static IEnumerable<Permission> CreateDefaultPermissions()
    {
        // Spec permissions
        yield return new Permission("spec:create", "Create Spec", "Create a spec", "spec", "create");
        yield return new Permission("spec:read", "Read Spec", "Read a spec", "spec", "read");
        yield return new Permission("spec:update", "Update Spec", "Update a spec", "spec", "update");
        yield return new Permission("spec:delete", "Delete Spec", "Delete a spec", "spec", "delete");
        yield return new Permission("spec:approve", "Approve Spec", "Approve a spec", "spec", "approve");
        yield return new Permission("spec:lock", "Lock Spec", "Lock a spec", "spec", "lock");

        // Conversation permissions
        yield return new Permission("conversation:create", "Create Conversation", "Create a conversation", "conversation", "create");
        yield return new Permission("conversation:read", "Read Conversation", "Read a conversation", "conversation", "read");
        yield return new Permission("conversation:post", "Post to Conversation", "Post a conversation message", "conversation", "post");
        yield return new Permission("conversation:close", "Close Conversation", "Close a conversation", "conversation", "close");

        // Requirement permissions
        yield return new Permission("requirement:create", "Create Requirement", "Create a requirement", "requirement", "create");
        yield return new Permission("requirement:read", "Read Requirement", "Read a requirement", "requirement", "read");
        yield return new Permission("requirement:update", "Update Requirement", "Update a requirement", "requirement", "update");
        yield return new Permission("requirement:delete", "Delete Requirement", "Delete a requirement", "requirement", "delete");

        // Generation permissions
        yield return new Permission("generation:execute", "Execute Generation", "Run code generation", "generation", "execute");
        yield return new Permission("generation:rollback", "Rollback Generation", "Roll back generated code", "generation", "rollback");

        // Glossary permissions
        yield return new Permission("glossary:create", "Create Glossary", "Create a glossary term", "glossary", "create");
        yield return new Permission("glossary:read", "Read Glossary", "Read glossary terms", "glossary", "read");
        yield return new Permission("glossary:update", "Update Glossary", "Update a glossary term", "glossary", "update");
        yield return new Permission("glossary:deprecate", "Deprecate Glossary", "Deprecate a glossary term", "glossary", "deprecate");

        // User permissions
        yield return new Permission("user:manage", "Manage Users", "Manage users", "user", "manage");
        yield return new Permission("role:assign", "Assign Roles", "Assign roles", "role", "assign");

        // Audit permissions
        yield return new Permission("audit:read", "Read Audit Logs", "Read audit logs", "audit", "read");

        // Project permissions
        yield return new Permission("project:create", "Create Project", "Create a project", "project", "create");
        yield return new Permission("project:read", "Read Project", "Read a project", "project", "read");
        yield return new Permission("project:update", "Update Project", "Update a project", "project", "update");
        yield return new Permission("project:delete", "Delete Project", "Delete a project", "project", "delete");

        // Task permissions
        yield return new Permission("task:create", "Create Task", "Create a task", "task", "create");
        yield return new Permission("task:read", "Read Task", "Read a task", "task", "read");
        yield return new Permission("task:update", "Update Task", "Update a task", "task", "update");
        yield return new Permission("task:delete", "Delete Task", "Delete a task", "task", "delete");
    }
}
