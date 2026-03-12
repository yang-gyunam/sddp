using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// user-role entity ()
/// </summary>
public class UserRole : EntityBase
{
    /// <summary>
    /// user ID
    /// </summary>
    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// role ID
    /// </summary>
    public GlobalUniqueId RoleId { get; private set; }

    /// <summary>
    /// role ID
    /// </summary>
    public GlobalUniqueId AssignedBy { get; private set; }

    /// <summary>
    /// role
    /// </summary>
    public Timestamp AssignedAt { get; private set; }

    /// <summary>
    /// tenant ID (role)
    /// </summary>
    public GlobalUniqueId? TenantId { get; private set; }

    /// <summary>
    /// project ID (role)
    /// </summary>
    public GlobalUniqueId? ProjectId { get; private set; }

    /// <summary>
    /// user
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// role
    /// </summary>
    public Role? Role { get; private set; }

    // EF Core default create
    private UserRole() { }

    public UserRole(GlobalUniqueId userId, GlobalUniqueId roleId, GlobalUniqueId assignedBy,
        GlobalUniqueId? tenantId = null, GlobalUniqueId? projectId = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedAt = Timestamp.Now;
        TenantId = tenantId;
        ProjectId = projectId;
    }
}
