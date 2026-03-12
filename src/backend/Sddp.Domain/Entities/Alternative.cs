using Sddp.Abstractions.Base;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Alternative entity for options considered during spec decision-making
/// REQ-04.3: alternative tracking
/// </summary>
public class Alternative : AuditableEntityBase
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
    /// Spec ID (FK)
    /// </summary>
    public GlobalUniqueId SpecId { get; private set; }

    /// <summary>
    /// Alternative description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Rejection reason (null means still under review)
    /// </summary>
    public string? RejectedReason { get; private set; }

    /// <summary>
    /// Proposer ID
    /// </summary>
    public GlobalUniqueId ProposedBy { get; private set; }

    /// <summary>
    /// Proposed timestamp
    /// </summary>
    public Timestamp ProposedAt { get; private set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Whether the alternative has been rejected
    /// </summary>
    public bool IsRejected => !string.IsNullOrEmpty(RejectedReason);

    /// <summary>
    /// Spec navigation property
    /// </summary>
    public Spec Spec { get; private set; } = null!;

    /// <summary>
    /// Proposer (user) navigation property
    /// </summary>
    public User Proposer { get; private set; } = null!;

    // Default constructor for EF Core
    private Alternative() { }

    public Alternative(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId,
        string description,
        GlobalUniqueId proposedBy,
        int order = 0)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SpecId = specId;
        Description = description;
        ProposedBy = proposedBy;
        ProposedAt = Timestamp.Now;
        Order = order;
    }

    /// <summary>
    /// Updates the alternative description
    /// </summary>
    public Result UpdateDescription(string description)
    {
        if (IsRejected)
        {
            return DomainError.InvalidStatus("update description", "Rejected");
        }

        Description = description;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Rejects the alternative
    /// </summary>
    public Result Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required", nameof(reason));
        }

        if (IsRejected)
        {
            return DomainError.AlreadyPerformed("Rejection");
        }

        RejectedReason = reason;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Updates the sort order
    /// </summary>
    public void SetOrder(int order)
    {
        Order = order;
        MarkAsModified();
    }
}
