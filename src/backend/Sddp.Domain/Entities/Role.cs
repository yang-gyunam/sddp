using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;

namespace Sddp.Domain.Entities;

// Mirrors src/backend/Sddp.Domain/Entities/Role.cs without the AI agent role.
// RoleConfiguration.cs is exported as-is, so every mapped property must exist here.

/// <summary>
/// Role entity
/// </summary>
public class Role : EntityBase
{
    /// <summary>
    /// Stable role code used by SQL seed data and external contracts.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Role type
    /// </summary>
    public RoleType Type { get; private set; }

    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this is a built-in system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Authorization scope stored as SYSTEM or PROJECT.
    /// </summary>
    public string Scope { get; private set; } = "PROJECT";

    /// <summary>
    /// Whether this role is assigned by default.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Stable display order for built-in roles.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Permissions assigned to this role
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    /// <summary>
    /// Users assigned to this role
    /// </summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    // Default constructor for EF Core
    private Role() { }

    public Role(
        RoleType type,
        string name,
        string description,
        bool isSystemRole = false,
        string? code = null,
        string? scope = null,
        bool isDefault = false,
        int sortOrder = 0)
    {
        Type = type;
        Code = code ?? GetDefaultCode(type);
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
        Scope = scope ?? GetDefaultScope(type);
        IsDefault = isDefault;
        SortOrder = sortOrder;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        MarkAsModified();
    }

    /// <summary>
    /// Creates the 6 default roles
    /// </summary>
    public static IEnumerable<Role> CreateDefaultRoles()
    {
        yield return new Role(RoleType.ProductOwner, "Product Owner", "Defines requirements and priorities",
            true, "PRODUCT_OWNER", "PROJECT", sortOrder: 2);
        yield return new Role(RoleType.DomainExpert, "Domain Expert", "Provides business logic and domain knowledge",
            true, "DOMAIN_EXPERT", "PROJECT", sortOrder: 3);
        yield return new Role(RoleType.Developer, "Developer", "Implements code and technical design",
            true, "DEVELOPER", "PROJECT", isDefault: true, sortOrder: 4);
        yield return new Role(RoleType.Reviewer, "Reviewer", "Reviews specs and code",
            true, "REVIEWER", "PROJECT", sortOrder: 5);
        yield return new Role(RoleType.QATester, "QA Tester", "Validates quality and testing",
            true, "QA_TESTER", "PROJECT", sortOrder: 6);
        yield return new Role(RoleType.Admin, "Admin", "Manages the overall system",
            true, "ADMIN", "SYSTEM", sortOrder: 1);
    }

    private static string GetDefaultCode(RoleType type) => type switch
    {
        RoleType.ProductOwner => "PRODUCT_OWNER",
        RoleType.DomainExpert => "DOMAIN_EXPERT",
        RoleType.Developer => "DEVELOPER",
        RoleType.Reviewer => "REVIEWER",
        RoleType.QATester => "QA_TESTER",
        RoleType.Admin => "ADMIN",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown role type.")
    };

    private static string GetDefaultScope(RoleType type) =>
        type is RoleType.Admin ? "SYSTEM" : "PROJECT";
}
