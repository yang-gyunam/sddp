using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Glossary term entity for managing shared project terminology
/// State transitions: Draft -> Active <-> Deprecated
/// </summary>
public class GlossaryTerm : VersionedEntityBase
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
    /// Term name (unique within a project)
    /// </summary>
    public string Term { get; private set; } = string.Empty;

    /// <summary>
    /// Term definition (Markdown supported)
    /// </summary>
    public string Definition { get; private set; } = string.Empty;

    /// <summary>
    /// Category (Technical/Business/Abbreviation/Domain)
    /// </summary>
    public TermCategory Category { get; private set; } = TermCategory.Business;

    /// <summary>
    /// Usage examples stored as a JSON array
    /// </summary>
    public List<string> UsageExamples { get; private set; } = new();

    /// <summary>
    /// Related term IDs stored as a JSON array
    /// </summary>
    public List<GlobalUniqueId> RelatedTermIds { get; private set; } = new();

    /// <summary>
    /// Source reference (document, URL, etc.)
    /// </summary>
    public string? Source { get; private set; }

    /// <summary>
    /// Definer ID (the user who defined the term)
    /// </summary>
    public GlobalUniqueId DefinedBy { get; private set; }

    /// <summary>
    /// Owner user ID (the user responsible for the term)
    /// </summary>
    public GlobalUniqueId? OwnerUserId { get; private set; }

    /// <summary>
    /// Approver ID (the user who approved the term when it became Active)
    /// </summary>
    public GlobalUniqueId? ApprovedBy { get; private set; }

    /// <summary>
    /// Approval timestamp
    /// </summary>
    public Timestamp? ApprovedAt { get; private set; }

    /// <summary>
    /// Term status
    /// </summary>
    public GlossaryTermStatus Status { get; private set; } = GlossaryTermStatus.Draft;

    /// <summary>
    /// Source spec ID where the term was defined
    /// </summary>
    public GlobalUniqueId? SourceSpecId { get; private set; }

    /// <summary>
    /// Source conversation ID where the term was discussed
    /// </summary>
    public GlobalUniqueId? SourceConversationId { get; private set; }

    /// <summary>
    /// Source requirement ID where the term was defined
    /// </summary>
    public GlobalUniqueId? SourceRequirementId { get; private set; }

    /// <summary>
    /// Replacement term ID used when the term is deprecated
    /// </summary>
    public GlobalUniqueId? ReplacedByTermId { get; private set; }

    /// <summary>
    /// Replacement term
    /// </summary>
    public GlossaryTerm? ReplacedByTerm { get; private set; }

    /// <summary>
    /// Synonym list (comma-separated)
    /// </summary>
    public string? Synonyms { get; private set; }

    /// <summary>
    /// Abbreviation when the full name is stored in Term
    /// </summary>
    public string? Abbreviation { get; private set; }

    // Default constructor for EF Core
    private GlossaryTerm() { }

    public GlossaryTerm(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        string term,
        string definition,
        TermCategory category,
        GlobalUniqueId definedBy,
        string? source = null,
        string? synonyms = null,
        string? abbreviation = null,
        GlobalUniqueId? sourceSpecId = null,
        GlobalUniqueId? sourceConversationId = null,
        GlobalUniqueId? sourceRequirementId = null,
        GlobalUniqueId? ownerUserId = null)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        Term = term;
        Definition = definition;
        Category = category;
        DefinedBy = definedBy;
        Source = source;
        Synonyms = synonyms;
        Abbreviation = abbreviation;
        SourceSpecId = sourceSpecId;
        SourceConversationId = sourceConversationId;
        SourceRequirementId = sourceRequirementId;
        OwnerUserId = ownerUserId;
        Status = GlossaryTermStatus.Draft;
    }

    /// <summary>
    /// Updates term details (allowed only in Draft)
    /// </summary>
    public Result Update(
        string definition,
        TermCategory category,
        string? source,
        string? synonyms,
        string? abbreviation)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update term", Status.ToString());
        }

        Definition = definition;
        Category = category;
        Source = source;
        Synonyms = synonyms;
        Abbreviation = abbreviation;
        IncrementPatchVersion();
        return Result.Success();
    }

    /// <summary>
    /// Renames the term (allowed only in Draft; creating a new version is recommended)
    /// </summary>
    public Result UpdateTerm(string term)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update term name", Status.ToString());
        }

        Term = term;
        IncrementMinorVersion();
        return Result.Success();
    }

    /// <summary>
    /// Sets usage examples
    /// </summary>
    public Result SetUsageExamples(List<string> examples)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update examples", Status.ToString());
        }

        UsageExamples = examples;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Adds a usage example
    /// </summary>
    public Result AddUsageExample(string example)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("add example", Status.ToString());
        }

        UsageExamples.Add(example);
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Sets related terms
    /// </summary>
    public Result SetRelatedTerms(List<GlobalUniqueId> termIds)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("update related terms", Status.ToString());
        }

        RelatedTermIds = termIds;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Adds a related term
    /// </summary>
    public Result AddRelatedTerm(GlobalUniqueId termId)
    {
        if (!Status.IsEditable())
        {
            return DomainError.InvalidStatus("add related term", Status.ToString());
        }

        if (!RelatedTermIds.Contains(termId))
        {
            RelatedTermIds.Add(termId);
            MarkAsModified();
        }
        return Result.Success();
    }

    /// <summary>
    /// Sets the source spec
    /// </summary>
    public void SetSourceSpec(GlobalUniqueId? specId)
    {
        SourceSpecId = specId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the source conversation
    /// </summary>
    public void SetSourceConversation(GlobalUniqueId? conversationId)
    {
        SourceConversationId = conversationId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the source requirement
    /// </summary>
    public void SetSourceRequirement(GlobalUniqueId? requirementId)
    {
        SourceRequirementId = requirementId;
        MarkAsModified();
    }

    /// <summary>
    /// Sets the owner
    /// </summary>
    public void SetOwner(GlobalUniqueId? userId)
    {
        OwnerUserId = userId;
        MarkAsModified();
    }

    /// <summary>
    /// Transitions the term state
    /// </summary>
    public Result TransitionTo(GlossaryTermStatus newStatus)
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
    /// Approves the term (Draft -> Active)
    /// </summary>
    public Result Approve(GlobalUniqueId approverId)
    {
        var result = TransitionTo(GlossaryTermStatus.Active);
        if (result.IsFailure) return result;
        ApprovedBy = approverId;
        ApprovedAt = Timestamp.Now;
        return Result.Success();
    }

    /// <summary>
    /// Deprecates the term
    /// </summary>
    public Result Deprecate(GlobalUniqueId? replacementTermId = null)
    {
        var result = TransitionTo(GlossaryTermStatus.Deprecated);
        if (result.IsFailure) return result;
        ReplacedByTermId = replacementTermId;
        return Result.Success();
    }

    /// <summary>
    /// Reactivates a deprecated term (Deprecated -> Active)
    /// </summary>
    public Result Reactivate(GlobalUniqueId reactivatedBy)
    {
        var result = TransitionTo(GlossaryTermStatus.Active);
        if (result.IsFailure) return result;
        ApprovedBy = reactivatedBy;
        ApprovedAt = Timestamp.Now;
        ReplacedByTermId = null;
        return Result.Success();
    }

    /// <summary>
    /// Returns the term to Draft for re-review (Active -> Draft)
    /// </summary>
    public Result RevertToDraft()
    {
        var result = TransitionTo(GlossaryTermStatus.Draft);
        if (result.IsFailure) return result;
        ApprovedBy = null;
        ApprovedAt = null;
        return Result.Success();
    }

    /// <summary>
    /// Creates a new version instance (VersionedEntityBase implementation)
    /// </summary>
    protected override VersionedEntityBase CreateNewVersionInstance()
    {
        return new GlossaryTerm(
            TenantId,
            ProjectId,
            Term,
            Definition,
            Category,
            DefinedBy,
            Source,
            Synonyms,
            Abbreviation,
            SourceSpecId,
            SourceConversationId,
            SourceRequirementId,
            OwnerUserId)
        {
            UsageExamples = new List<string>(UsageExamples),
            RelatedTermIds = new List<GlobalUniqueId>(RelatedTermIds)
        };
    }

    /// <summary>
    /// SCD Type 6: copies current data to create the next version instance.
    /// Preserves version, status, and approval metadata. Calling Update() increments the version.
    /// </summary>
    public GlossaryTerm CreateNextVersion()
    {
        var next = (GlossaryTerm)CreateNewVersionInstance();
        next.Version = Version;
        next.Status = Status;
        next.ApprovedBy = ApprovedBy;
        next.ApprovedAt = ApprovedAt;
        return next;
    }
}
