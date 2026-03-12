using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;

namespace Sddp.Domain.Entities;

/// <summary>
/// Role entity.
/// </summary>
public class Role : EntityBase
{
    /// <summary>
    /// Role type.
    /// </summary>
    public RoleType Type { get; private set; }

    /// <summary>
    /// Role name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Role description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this is a built-in system role.
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Permissions assigned to this role.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    /// <summary>
    /// Users assigned to this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    private Role() { }

    public Role(RoleType type, string name, string description, bool isSystemRole = false)
    {
        Type = type;
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        MarkAsModified();
    }

    /// <summary>
    /// Creates the default public roles.
    /// </summary>
    public static IEnumerable<Role> CreateDefaultRoles()
    {
        yield return new Role(RoleType.ProductOwner, "Product Owner", "Defines requirements and priorities", true);
        yield return new Role(RoleType.DomainExpert, "Domain Expert", "Provides business logic and domain knowledge", true);
        yield return new Role(RoleType.Developer, "Developer", "Implements code and technical design", true);
        yield return new Role(RoleType.Reviewer, "Reviewer", "Reviews specs and code", true);
        yield return new Role(RoleType.QATester, "QA Tester", "Validates quality and testing", true);
        yield return new Role(RoleType.Admin, "Admin", "Manages the overall system", true);
    }
}
