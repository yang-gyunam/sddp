using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Conversation abstract base entity (TPT base)
/// DB table: conversations
/// Derived types: Channel, Forum, DirectMessage
/// </summary>
public abstract class Conversation : EntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; protected set; }

    /// <summary>
    /// Project ID (nullable: null means tenant-wide)
    /// </summary>
    public GlobalUniqueId? ProjectId { get; protected set; }

    /// <summary>
    /// Conversation space name
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    /// Conversation space description
    /// </summary>
    public string? Description { get; protected set; }

    /// <summary>
    /// Canonical conversation type (Channel, Forum, DirectMessage)
    /// </summary>
    public ConversationType ConversationType { get; protected set; }

    /// <summary>
    /// Canonical visibility (Public, Private)
    /// </summary>
    public ConversationVisibility Visibility { get; protected set; }

    /// <summary>
    /// Canonical scope (TenantWide, ProjectScoped)
    /// </summary>
    public ConversationScope Scope { get; protected set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; protected set; }

    /// <summary>
    /// Whether the conversation is archived
    /// </summary>
    public bool IsArchived { get; protected set; }

    /// <summary>
    /// Whether this is a default conversation space
    /// </summary>
    public bool IsDefault { get; protected set; }

    /// <summary>
    /// Messages
    /// </summary>
    public ICollection<Message> Messages { get; protected set; } = [];

    /// <summary>
    /// Members
    /// </summary>
    public ICollection<ConversationMember> Members { get; protected set; } = [];

    // Default constructor for EF Core
    protected Conversation() { }

    protected Conversation(
        GlobalUniqueId tenantId,
        GlobalUniqueId? projectId,
        string name,
        ConversationType conversationType,
        ConversationVisibility visibility = ConversationVisibility.Public,
        int sortOrder = 0,
        ConversationScope? scope = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        Name = name;
        ConversationType = conversationType;
        Visibility = visibility;
        Scope = scope ?? (projectId.HasValue ? ConversationScope.ProjectScoped : ConversationScope.TenantWide);
        SortOrder = sortOrder;
        IsArchived = false;
        IsDefault = false;
        EnsureScopeConsistency();
    }

    /// <summary>
    /// Returns whether this is a tenant-wide conversation
    /// </summary>
    public bool IsTenantWide => Scope == ConversationScope.TenantWide;

    /// <summary>
    /// Updates the name
    /// </summary>
    public Result UpdateName(string name)
    {
        if (IsArchived)
            return DomainError.InvalidStatus("update name", "Archived");

        Name = name;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Updates the description
    /// </summary>
    public Result UpdateDescription(string? description)
    {
        if (IsArchived)
            return DomainError.InvalidStatus("update description", "Archived");

        Description = description;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Updates the sort order
    /// </summary>
    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        MarkAsModified();
    }

    /// <summary>
    /// Updates the visibility
    /// </summary>
    public void SetVisibility(ConversationVisibility visibility)
    {
        Visibility = visibility;
        MarkAsModified();
    }

    /// <summary>
    /// Archives the conversation
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        MarkAsModified();
    }

    /// <summary>
    /// Unarchives the conversation
    /// </summary>
    public void Unarchive()
    {
        IsArchived = false;
        MarkAsModified();
    }

    /// <summary>
    /// Returns whether messages can be added
    /// </summary>
    public bool CanAddMessage() => !IsArchived && IsActive;

    private void EnsureScopeConsistency()
    {
        if (Scope == ConversationScope.TenantWide && ProjectId.HasValue)
        {
            throw new InvalidOperationException("TenantWide scope cannot have a ProjectId.");
        }

        if (Scope == ConversationScope.ProjectScoped && !ProjectId.HasValue)
        {
            throw new InvalidOperationException("ProjectScoped scope requires a ProjectId.");
        }
    }
}
