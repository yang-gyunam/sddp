using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Events;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Spec entity - an agreed document promoted from discussion
/// State transitions: Draft -> InReview -> Approved -> Locked
/// </summary>
public class Spec : VersionedEntityBase
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
    /// Spec code (for example: SPEC-001)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Spec title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Spec description (Markdown supported)
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Decision content (agreed outcome)
    /// </summary>
    public string? Decision { get; private set; }

    /// <summary>
    /// Context (background and rationale for the decision)
    /// </summary>
    public string? Context { get; private set; }

    /// <summary>
    /// Scope covered by the spec
    /// </summary>
    public string? Scope { get; private set; }

    /// <summary>
    /// Out-of-scope items excluded from the spec
    /// </summary>
    public string? OutOfScope { get; private set; }

    /// <summary>
    /// Definitions of terms and concepts (Markdown)
    /// </summary>
    public string? Definitions { get; private set; }

    /// <summary>
    /// Acceptance criteria (completion verification conditions, Markdown)
    /// </summary>
    public string? AcceptanceCriteria { get; private set; }

    /// <summary>
    /// Owner list
    /// </summary>
    public SpecOwners Owners { get; private set; } = SpecOwners.Empty;

    /// <summary>
    /// Review timing or trigger conditions (Markdown)
    /// </summary>
    public string? ReviewTrigger { get; private set; }

    /// <summary>
    /// Spec status
    /// </summary>
    public SpecStatus Status { get; private set; } = SpecStatus.Draft;

    /// <summary>
    /// Linked requirement ID (optional)
    /// </summary>
    public GlobalUniqueId? RequirementId { get; private set; }

    /// <summary>
    /// Linked requirement
    /// </summary>
    public Requirement? Requirement { get; private set; }

    /// <summary>
    /// Source conversation ID the spec was created from (optional)
    /// </summary>
    public GlobalUniqueId? BornFromConversationId { get; private set; }

    /// <summary>
    /// Previous spec ID this spec supersedes (when creating a new version)
    /// </summary>
    public GlobalUniqueId? SupersedesSpecId { get; private set; }

    /// <summary>
    /// Previous superseded spec
    /// </summary>
    public Spec? SupersedesSpec { get; private set; }

    /// <summary>
    /// Time when the spec was locked
    /// </summary>
    public Timestamp? LockedAt { get; private set; }

    /// <summary>
    /// Sign-off list (REQ-04.3: decision-maker sign-off)
    /// </summary>
    public ICollection<SignOff> SignOffs { get; private set; } = new List<SignOff>();

    /// <summary>
    /// Alternative list (REQ-04.3: alternative tracking)
    /// </summary>
    public ICollection<Alternative> Alternatives { get; private set; } = new List<Alternative>();

    // Default constructor for EF Core
    private Spec() { }

    public Spec(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        string code,
        string title,
        string? description = null,
        string? decision = null,
        string? context = null,
        string? scope = null,
        string? outOfScope = null,
        string? definitions = null,
        string? acceptanceCriteria = null,
        string? owners = null,
        string? reviewTrigger = null,
        GlobalUniqueId? requirementId = null,
        GlobalUniqueId? bornFromConversationId = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        Code = code;
        Title = title;
        Description = description;
        Decision = decision;
        Context = context;
        Scope = scope;
        OutOfScope = outOfScope;
        Definitions = definitions;
        AcceptanceCriteria = acceptanceCriteria;
        Owners = SpecOwners.FromCsv(owners);
        ReviewTrigger = reviewTrigger;
        RequirementId = requirementId;
        BornFromConversationId = bornFromConversationId;
        Status = SpecStatus.Draft;
    }

    /// <summary>
    /// Updates spec information (allowed only in Draft)
    /// </summary>
    public Result Update(
        string title,
        string? description,
        string? decision,
        string? context,
        string? scope,
        string? outOfScope,
        string? definitions,
        string? acceptanceCriteria,
        string? owners,
        string? reviewTrigger)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update spec", Status.ToString());
        }

        Title = title;
        Description = description;
        Decision = decision;
        Context = context;
        Scope = scope;
        OutOfScope = outOfScope;
        Definitions = definitions;
        AcceptanceCriteria = acceptanceCriteria;
        Owners = SpecOwners.FromCsv(owners);
        ReviewTrigger = reviewTrigger;
        IncrementPatchVersion();
        return Result.Success();
    }

    /// <summary>
    /// Checks whether the given user owns the spec
    /// </summary>
    public bool IsOwnedBy(GlobalUniqueId userId)
    {
        return Owners.Contains(userId);
    }

    /// <summary>
    /// Returns the primary owner (the first valid owner ID)
    /// </summary>
    public GlobalUniqueId? GetPrimaryOwnerId()
    {
        return Owners.GetPrimaryOwnerId();
    }

    /// <summary>
    /// Transitions the spec state
    /// </summary>
    public Result TransitionTo(SpecStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            return DomainError.InvalidTransition($"transition from {Status} to {newStatus}", Status.ToString());
        }

        Status = newStatus;

        if (newStatus == SpecStatus.Locked)
        {
            LockedAt = Timestamp.Now;
        }

        IncrementMinorVersion();
        return Result.Success();
    }

    /// <summary>
    /// Records a status change event for outbox publication
    /// </summary>
    public void RecordStatusChange(
        GlobalUniqueId actorId,
        SpecStatus fromStatus,
        SpecStatus toStatus,
        string? reason = null)
    {
        AddDomainEvent(new SpecStatusChangedEvent(
            SpecId: Id,
            TenantId: TenantId,
            ProjectId: ProjectId,
            FromStatus: fromStatus,
            ToStatus: toStatus,
            ActorId: actorId,
            Reason: reason));
    }

    /// <summary>
    /// Submits the spec for review (Draft -> InReview)
    /// </summary>
    public Result SubmitForReview()
    {
        var validationError = ValidateForReview();
        if (validationError is not null)
            return validationError;
        return TransitionTo(SpecStatus.InReview);
    }

    /// <summary>
    /// Approves the spec (InReview -> Approved)
    /// </summary>
    public Result Approve()
    {
        return TransitionTo(SpecStatus.Approved);
    }

    /// <summary>
    /// Rejects the spec (InReview -> Draft)
    /// </summary>
    public Result Reject()
    {
        return TransitionTo(SpecStatus.Draft);
    }

    /// <summary>
    /// Locks the spec (Approved -> Locked)
    /// </summary>
    public Result Lock()
    {
        return TransitionTo(SpecStatus.Locked);
    }

    /// <summary>
    /// Links a requirement
    /// </summary>
    public Result LinkRequirement(GlobalUniqueId requirementId)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("link requirement", Status.ToString());
        }

        RequirementId = requirementId;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Unlinks the requirement
    /// </summary>
    public Result UnlinkRequirement()
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("unlink requirement", Status.ToString());
        }

        RequirementId = null;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Links a conversation
    /// </summary>
    public void LinkConversation(GlobalUniqueId conversationId)
    {
        BornFromConversationId = conversationId;
        MarkAsModified();
    }

    /// <summary>
    /// Unlinks the conversation
    /// </summary>
    public void UnlinkConversation()
    {
        BornFromConversationId = null;
        MarkAsModified();
    }

    /// <summary>
    /// Marks that this spec supersedes a previous spec
    /// </summary>
    public void SetSupersedesSpec(GlobalUniqueId specId)
    {
        SupersedesSpecId = specId;
        MarkAsModified();
    }

    /// <summary>
    /// Validates required fields before review submission
    /// </summary>
    private DomainError? ValidateForReview()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(Decision))
            errors.Add("Decision is required");

        if (string.IsNullOrWhiteSpace(Context))
            errors.Add("Context is required");

        if (string.IsNullOrWhiteSpace(AcceptanceCriteria))
            errors.Add("AcceptanceCriteria is required");

        if (errors.Count > 0)
        {
            return DomainError.ValidationFailed(
                $"Cannot submit for review: {string.Join(", ", errors)}");
        }

        return null;
    }

    /// <summary>
    /// Creates a new version instance (VersionedEntityBase implementation)
    /// </summary>
    protected override VersionedEntityBase CreateNewVersionInstance()
    {
        return new Spec(
            TenantId,
            ProjectId,
            Code,
            Title,
            Description,
            Decision,
            Context,
            Scope,
            OutOfScope,
            Definitions,
            AcceptanceCriteria,
            Owners.ToCsv(),
            ReviewTrigger,
            RequirementId,
            BornFromConversationId)
        {
            SupersedesSpecId = Id
        };
    }
}
