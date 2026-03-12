using Sddp.Abstractions.Base;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Result = Sddp.Abstractions.Base.Result;

namespace Sddp.Domain.Entities;

/// <summary>
/// Sign-off entity for recording spec approval decisions
/// REQ-04.3: decision-maker sign-off
/// REQ-06.2: separation of creation and approval
/// </summary>
public class SignOff : EntityBase
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
    /// Stakeholder (decision-maker) ID (FK)
    /// </summary>
    public GlobalUniqueId StakeholderId { get; private set; }

    /// <summary>
    /// Role at the time of approval
    /// </summary>
    public RoleType Role { get; private set; }

    /// <summary>
    /// Sign-off decision
    /// </summary>
    public SignOffDecision Decision { get; private set; } = SignOffDecision.Pending;

    /// <summary>
    /// Conditions for conditional approval
    /// </summary>
    public string? Conditions { get; private set; }

    /// <summary>
    /// Comments or reason
    /// </summary>
    public string? Comments { get; private set; }

    /// <summary>
    /// SLA timer start time (request time; refreshed on Reset)
    /// </summary>
    public Timestamp RequestedAt { get; private set; } = Timestamp.Now;

    /// <summary>
    /// Sign-off completion time
    /// </summary>
    public Timestamp? SignedAt { get; private set; }

    /// <summary>
    /// Spec navigation property
    /// </summary>
    public Spec Spec { get; private set; } = null!;

    /// <summary>
    /// Stakeholder (user) navigation property
    /// </summary>
    public User Stakeholder { get; private set; } = null!;

    // Default constructor for EF Core
    private SignOff() { }

    public SignOff(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId specId,
        GlobalUniqueId stakeholderId,
        RoleType role)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        SpecId = specId;
        StakeholderId = stakeholderId;
        Role = role;
        Decision = SignOffDecision.Pending;
        RequestedAt = Timestamp.Now;
    }

    /// <summary>
    /// Approves the sign-off
    /// </summary>
    public Result Approve(string? comments = null)
    {
        if (Decision.IsDecided())
        {
            return DomainError.AlreadyPerformed("Sign-off decision");
        }

        Decision = SignOffDecision.Approved;
        Comments = comments;
        SignedAt = Timestamp.Now;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Rejects the sign-off
    /// </summary>
    public Result Reject(string reason)
    {
        if (Decision.IsDecided())
        {
            return DomainError.AlreadyPerformed("Sign-off decision");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required", nameof(reason));
        }

        Decision = SignOffDecision.Rejected;
        Comments = reason;
        SignedAt = Timestamp.Now;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Approves the sign-off with conditions
    /// </summary>
    public Result ApproveWithConditions(string conditions, string? comments = null)
    {
        if (Decision.IsDecided())
        {
            return DomainError.AlreadyPerformed("Sign-off decision");
        }

        if (string.IsNullOrWhiteSpace(conditions))
        {
            throw new ArgumentException("Conditions are required for conditional approval", nameof(conditions));
        }

        Decision = SignOffDecision.Conditional;
        Conditions = conditions;
        Comments = comments;
        SignedAt = Timestamp.Now;
        MarkAsModified();
        return Result.Success();
    }

    /// <summary>
    /// Resets the sign-off decision for re-review
    /// </summary>
    public void Reset()
    {
        Decision = SignOffDecision.Pending;
        Conditions = null;
        Comments = null;
        SignedAt = null;
        RequestedAt = Timestamp.Now;
        MarkAsModified();
    }
}
