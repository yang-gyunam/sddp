using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Requirement entity
/// Supports Level A -> B -> C hierarchy and state transitions
/// </summary>
public class Requirement : VersionedEntityBase
{
    /// <summary>
    /// Tenant ID (for multi-tenancy)
    /// </summary>
    public GlobalUniqueId TenantId { get; private set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public GlobalUniqueId ProjectId { get; private set; }

    /// <summary>
    /// Requirement code (for example: REQ-001, REQ-001-A, REQ-001-A-1)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Requirement title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Requirement description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Requirement hierarchy level (A, B, C)
    /// </summary>
    public RequirementLevel Level { get; private set; }

    /// <summary>
    /// Requirement priority
    /// </summary>
    public RequirementPriority Priority { get; private set; } = RequirementPriority.Medium;

    /// <summary>
    /// Requirement status
    /// </summary>
    public RequirementStatus Status { get; private set; } = RequirementStatus.Draft;

    /// <summary>
    /// Parent requirement ID (required for Level B and C)
    /// </summary>
    public GlobalUniqueId? ParentId { get; private set; }

    /// <summary>
    /// Parent requirement
    /// </summary>
    public Requirement? Parent { get; private set; }

    /// <summary>
    /// Child requirements
    /// </summary>
    public ICollection<Requirement> Children { get; private set; } = [];

    /// <summary>
    /// Owner user ID (nullable; may be unassigned)
    /// </summary>
    public GlobalUniqueId? OwnerUserId { get; private set; }

    /// <summary>
    /// Linked conversation ID (optional)
    /// </summary>
    public GlobalUniqueId? ConversationId { get; private set; }

    // Default constructor for EF Core
    private Requirement() { }

    public Requirement(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        string code,
        string title,
        string description,
        RequirementLevel level,
        RequirementPriority priority = RequirementPriority.Medium,
        GlobalUniqueId? parentId = null,
        GlobalUniqueId? ownerUserId = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        Code = code;
        Title = title;
        Description = description;
        Level = level;
        Priority = priority;
        ParentId = parentId;
        OwnerUserId = ownerUserId;
        Status = RequirementStatus.Draft;

        ValidateLevelHierarchy();
    }

    /// <summary>
    /// Updates requirement information (allowed only in Draft)
    /// </summary>
    public Result Update(string title, string description, RequirementPriority priority)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update requirement", Status.ToString());
        }

        Title = title;
        Description = description;
        Priority = priority;
        IncrementPatchVersion();
        return Result.Success();
    }

    /// <summary>
    /// Transitions the requirement state
    /// </summary>
    public Result TransitionTo(RequirementStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            return DomainError.InvalidTransition($"transition from {Status} to {newStatus}", Status.ToString());
        }

        Status = newStatus;
        IncrementMinorVersion();
        return Result.Success();
    }

    /// <summary>
    /// Links a conversation
    /// </summary>
    public void LinkConversation(GlobalUniqueId conversationId)
    {
        ConversationId = conversationId;
        MarkAsModified();
    }

    /// <summary>
    /// Unlinks the conversation
    /// </summary>
    public void UnlinkConversation()
    {
        ConversationId = null;
        MarkAsModified();
    }

    /// <summary>
    /// Changes the parent requirement (allowed only in Draft)
    /// </summary>
    public Result SetParent(GlobalUniqueId? parentId)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("change parent", Status.ToString());
        }

        ParentId = parentId;

        var hierarchyError = ValidateLevelHierarchyFor(parentId);
        if (hierarchyError is not null)
        {
            return hierarchyError;
        }

        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Sets or changes the owner
    /// </summary>
    public void SetOwner(GlobalUniqueId? ownerUserId)
    {
        OwnerUserId = ownerUserId;
        MarkAsModified();
    }

    /// <summary>
    /// Submits the requirement for review (Draft -> InReview)
    /// </summary>
    public Result SubmitForReview()
    {
        return TransitionTo(RequirementStatus.InReview);
    }

    /// <summary>
    /// Approves the requirement (InReview -> Approved)
    /// </summary>
    public Result Approve()
    {
        return TransitionTo(RequirementStatus.Approved);
    }

    /// <summary>
    /// Rejects the requirement (InReview -> Draft)
    /// </summary>
    public Result Reject()
    {
        return TransitionTo(RequirementStatus.Draft);
    }

    /// <summary>
    /// Deprecates the requirement
    /// </summary>
    public Result Deprecate()
    {
        return TransitionTo(RequirementStatus.Deprecated);
    }

    /// <summary>
    /// Validates the level hierarchy (constructor invariant - keeps exception behavior)
    /// </summary>
    private void ValidateLevelHierarchy()
    {
        switch (Level)
        {
            case RequirementLevel.A when ParentId.HasValue:
                throw new InvalidOperationException("Level A requirement cannot have a parent");

            case RequirementLevel.B when !ParentId.HasValue:
                throw new InvalidOperationException("Level B requirement must have a parent");

            case RequirementLevel.C when !ParentId.HasValue:
                throw new InvalidOperationException("Level C requirement must have a parent");
        }
    }

    /// <summary>
    /// Validates the level hierarchy for SetParent (returns a Result-compatible error)
    /// </summary>
    private DomainError? ValidateLevelHierarchyFor(GlobalUniqueId? parentId)
    {
        return Level switch
        {
            RequirementLevel.A when parentId.HasValue =>
                DomainError.Inconsistency("Level A requirement cannot have a parent"),
            RequirementLevel.B when !parentId.HasValue =>
                DomainError.Inconsistency("Level B requirement must have a parent"),
            RequirementLevel.C when !parentId.HasValue =>
                DomainError.Inconsistency("Level C requirement must have a parent"),
            _ => null
        };
    }

    /// <summary>
    /// Creates a historical snapshot of the current state (SCD Type 6).
    /// The snapshot receives a new ID and stores a past version by setting ValidTo.
    /// The original entity ID remains unchanged so references stay intact.
    /// </summary>
    public Requirement CreateHistoricalSnapshot()
    {
        return new Requirement
        {
            // EntityBase
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,

            // AuditableEntityBase
            CreatedBy = CreatedBy,
            UpdatedBy = UpdatedBy,

            // VersionedEntityBase
            Version = Version,
            ValidFrom = ValidFrom,
            ValidTo = Timestamp.Now,

            // Requirement domain fields
            TenantId = TenantId,
            ProjectId = ProjectId,
            Code = Code,
            Title = Title,
            Description = Description,
            Level = Level,
            Priority = Priority,
            Status = Status,
            ParentId = ParentId,
            ConversationId = ConversationId,
            OwnerUserId = OwnerUserId,
        };
    }

    /// <summary>
    /// Resets ValidFrom to the current time (called after an SCD Type 6 update).
    /// </summary>
    public void ResetValidFrom()
    {
        ValidFrom = Timestamp.Now;
    }

    /// <summary>
    /// Creates a new version instance (VersionedEntityBase implementation)
    /// </summary>
    protected override VersionedEntityBase CreateNewVersionInstance()
    {
        return new Requirement(TenantId, ProjectId, Code, Title, Description, Level, Priority, ParentId, OwnerUserId)
        {
            ConversationId = ConversationId,
            Status = Status
        };
    }
}
