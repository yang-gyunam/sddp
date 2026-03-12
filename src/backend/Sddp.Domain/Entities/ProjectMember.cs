using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Domain.Entities;

/// <summary>
/// project entity
/// </summary>
public class ProjectMember : EntityBase
{
    /// <summary>
    /// project ID
    /// </summary>
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// user ID
    /// </summary>
    public GlobalUniqueId UserId { get; private set; }

    /// <summary>
    /// user role ID (UserRole)
    /// </summary>
    public GlobalUniqueId UserRoleId { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public Timestamp JoinedAt { get; private set; }

    /// <summary>
    /// project
    /// </summary>
    public Project Project { get; private set; } = null!;

    /// <summary>
    /// user
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// user role
    /// </summary>
    public UserRole UserRole { get; private set; } = null!;

    // EF Core default create
    private ProjectMember() { }

    public ProjectMember(GlobalUniqueId projectId, GlobalUniqueId userId, GlobalUniqueId userRoleId)
    {
        ProjectId = projectId;
        UserId = userId;
        UserRoleId = userRoleId;
        JoinedAt = Timestamp.Now;
    }

    public void ChangeRole(GlobalUniqueId userRoleId)
    {
        UserRoleId = userRoleId;
        MarkAsModified();
    }
}
